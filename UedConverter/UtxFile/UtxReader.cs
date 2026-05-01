using System.Collections;
using System.IO;
using System.Text;

namespace UedConverter.UtxFile;

public sealed class UtxReader : IDisposable
{
    private readonly string _path;
    private readonly Structure _structure;
    private readonly FileStream _fs;
    private readonly BinaryReader _br;
    private readonly List<string> _problems = [];

    private UtxReader(string path)
    {
        _path = path;
        _structure = new Structure(Path.GetFileNameWithoutExtension(_path));
        _fs = new FileStream(_path, FileMode.Open, FileAccess.Read);
        _br = new BinaryReader(_fs);
    }

    public void Dispose()
    {
        _br.Dispose();
        _fs.Dispose();
    }

    public static ReadResult ReadFile(string path)
    {
        using var reader = new UtxReader(path);
        try
        {
            reader.Read();
        }
        catch (Exception e)
        {
            return new ReadResult(reader._structure, reader._problems, e);
        }
        return new ReadResult(reader._structure, reader._problems);
    }

    public record ReadResult(Structure Structure, List<string> Problems, Exception? Exception = null);

    private void Read()
    {
        _structure.Signature = new Signature(_br.ReadBytes(4));
        _structure.Header = new Header()
        {
            Version = _br.ReadInt32(),
            UnknowField1 = _br.ReadInt32(),
            NamesCount = _br.ReadInt32(),
            NamesStart = _br.ReadInt32(),
            ContentsTableCount = _br.ReadInt32(),
            ContentsTableStart = _br.ReadInt32(),
            ClassesCount = _br.ReadInt32(),
            ClassesStart = _br.ReadInt32(),
        };

        _fs.Seek(_structure.Header.NamesStart, SeekOrigin.Begin);

        ReadNames();
        ReadUsedClasses();
        ReadContentsTable();

        ResolveObjectReferences();

        LoadContents();
    }

    private void ReadNames()
    {
        _fs.Seek(_structure.Header.NamesStart, SeekOrigin.Begin);
        for (int i = 0; i < _structure.Header.NamesCount; i++)
        {
            var start = _fs.Position;
            if (_structure.Header.Version > 63)
            {
                //Read string with set length
                var length = _br.ReadByte() - 1;
                if (length > 0)
                {
                    var value = _br.ReadBytes(length);
                    _br.ReadByte();//terminator
                    var flags = _br.ReadUInt32();
                    _structure.AddName(Encoding.UTF8.GetString(value, 0, length), flags, GetRaw(start));
                }
            }
            else
            {
                //Read string character by character
                var sb = new StringBuilder();
                char c;
                do
                {
                    c = _br.ReadChar();
                    sb.Append(c);
                }
                while (c != (char)0);
                var flags = _br.ReadUInt32();
                if (sb.Length > 0) _structure.AddName(sb.ToString().TrimEnd('\0'), flags, GetRaw(start));
            }
        }
    }

    private void ReadPalette(ContentDef? definition = null)
    {
        if (definition?.Offset != null) _fs.Seek((int)definition.Offset, SeekOrigin.Begin);

        var type = _fs.ReadByte();
        var w = _fs.ReadByte();
        var h = _fs.ReadByte();
        UColor[] colors = new UColor[w * h];
        for (int i = 0; i < w * h; i++)
        {
            colors[i] = UColor.FromBytes(_br.ReadBytes(4));
        }
        var palette = new Palette()
        {
            Type = (byte)type,
            W = (byte)w,
            H = (byte)h,
            Colors = colors,
            Name = definition?.Name
        };
        _structure.Palettes.Add(palette);
        definition?.Obj = palette;
    }

    private bool ReadImage(ContentDef definition, string? group)
    {
        if (definition.Offset == null) return false;
        _fs.Seek((int)definition.Offset, SeekOrigin.Begin);
        Image image = new(definition.Name, group);
        var start = _fs.Position;

        //read image data
        while (true)
        {
            var property = ReadProperty();
            if (property == null) break;
            image.AddProperty(property);
        }
        var imageCount = _br.ReadByte();
        image.ImageHeaderRaw = GetRaw(start);
        if (image.Size > 1024 * 1024 || imageCount < 1) return false;

        image.ImageData = ReadImageChunk(image.Width, image.Height);
        _structure.Images.Add(image);
        image.IsCorrect = image.ImageData.IsCorrect;

        var nextMipmapWidth = image.ImageData.Width;
        var nextMipmapHeight = image.ImageData.Height;
        //Mipmaps
        for (int i = 0; i < imageCount - 1; i++)
        {
            nextMipmapWidth = Math.Max(1, nextMipmapWidth / 2);
            nextMipmapHeight = Math.Max(1, nextMipmapHeight / 2);
            var imageChunk = ReadImageChunk(nextMipmapWidth, nextMipmapHeight);
            image.MipMaps.Add(imageChunk);
            nextMipmapWidth = imageChunk.Width;
            nextMipmapHeight = imageChunk.Height;
        }
        return true;
    }

    private UProperty? ReadProperty()
    {
        var start = _fs.Position;
        var name = ReadCompactIndexName();
        var property = new UProperty(name);
        if (name == "None") return null;

        property.Type = _br.ReadByte();
        switch (property.Type)
        {
            case UPropertyType.Reference:
            case UPropertyType.NameRef:
                {
                    var objRef = new ObjectReference(ReadCompactInt());
                    property.Value = objRef;
                    objRef.ResolveReference(_structure);
                    break;
                }
            case UPropertyType.Float:
                {
                    property.Value = _br.ReadSingle();
                    break;
                }
            case UPropertyType.Int:
                {
                    property.Value = _br.ReadInt32();
                    break;
                }
            case UPropertyType.IntIndex:
                {
                    property.Name += $"[{_br.ReadByte()}]";
                    property.Value = _br.ReadInt32();
                    break;
                }
            case UPropertyType.Color:
                {
                    property.Value = ReadCompactIndexName();
                    property.Color = UColor.FromBytes(_br.ReadBytes(4));
                    break;
                }
            case UPropertyType.Boolean:
            case UPropertyType.ByteEnumerated:
                {
                    property.Value = _br.ReadByte();
                    break;
                }
            case UPropertyType.Struct:
                {
                    var structType = ReadCompactIndexName();
                    var structParam = ReadCompactIndexName();
                    property.Value = UnrealStructs.Read(_br, structType);
                    break;
                }
            case UPropertyType.StructIndex:
                {
                    var structType = ReadCompactIndexName();
                    var structParam = ReadCompactIndexName();
                    var index = _br.ReadByte();
                    if (index == 0x80)
                    {
                        var secondIndex = _br.ReadByte(); //dont ask me why
                        index = secondIndex;
                    }
                    property.Name += $"[{index}]";
                    property.Value = UnrealStructs.Read(_br, structType);
                    break;
                }
            default:
                {
                    _problems.Add($"Unknown type {property.Type} for UProperty({property.Name}) at 0x{start:X}");
                    property.Value = _br.ReadByte();
                    break;
                }
        }
        property.RawData = GetRaw(start);
        return property;
    }

    private ImageChunk ReadImageChunk(int width, int height)
    {
        var start = _fs.Position;
        var image = new ImageChunk();
        var calculatedSize = width * height;
        if (_structure.Header.Version >= 63)
            _br.ReadInt32();//unknown variable
        var size = ReadCompactInt();

        image.RawData = GetRaw(start);
        image.Pixels = _br.ReadBytes(width * height);
        image.Width = _br.ReadInt32();
        image.Height = _br.ReadInt32();
        image.wPower = _br.ReadByte();
        image.hPower = _br.ReadByte();
        image.IsCorrect = calculatedSize == size && image.Width == width && image.Height == height;
        return image;
    }

    private void ReadProceduralImage(ContentDef definition, string? group)
    {
        if (definition.Offset == null) return;
        _fs.Seek((int)definition.Offset, SeekOrigin.Begin);
        ProceduralImage image = new(definition.Name, group);
        var start = _fs.Position;

        //read image data
        while (true)
        {
            var property = ReadProperty();
            if (property == null) break;
            image.AddProperty(property);
        }
        image.ImageHeaderRaw = GetRaw(start);
        _structure.ProceduralImages.Add(image);
    }

    private void ReadUsedClasses()
    {
        _fs.Seek(_structure.Header.ClassesStart, SeekOrigin.Begin);
        for (int i = 0; i < _structure.Header.ClassesCount; i++)
        {
            _structure.UsedClasses.Add(
                new UsedClass()
                {
                    Package = ReadCompactIndexName(),
                    Type = ReadCompactIndexName(),
                    Parent = new ObjectReference(_br.ReadInt32()),
                    Name = ReadCompactIndexName()
                });
        }
    }

    private string ReadCompactIndexName()
    {
        var start = _fs.Position;
        var idx = ReadCompactInt();
        if (idx >= _structure.Names.Count || idx < 0)
        {
            _problems.Add($"Index out of bound ({idx}) for compact index at 0x{start:X}");
            return $"Index out of bounds({idx}/{_structure.Names.Count})";
        }
        return _structure.Names[idx].Value;
    }

    private int ReadCompactInt()
    {
        return CompactIndex.Read(_br);
    }

    private void ReadContentsTable()
    {
        _fs.Seek(_structure.Header.ContentsTableStart, SeekOrigin.Begin);

        int counter = 0;
        for (int i = 0; i < _structure.Header.ContentsTableCount; i++)
        {
            counter++;
            var nStart = _fs.Position;
            var type = new ObjectReference(ReadCompactInt());
            var type2 = new ObjectReference(ReadCompactInt());
            var group = new ObjectReference(_br.ReadInt32());
            var name = ReadCompactIndexName();
            var idk = _br.ReadInt32();
            var size = ReadCompactInt();
            int? offset = null;
            if (size > 0)
            {
                offset = ReadCompactInt();
            }
            _structure.ContentTable.Add(new ContentDef() { id = type, id2 = type2, Name = name, Group = group, id4 = idk, Size = size, Offset = offset, RawData = GetRaw(nStart) });
        }
    }

    private void ResolveObjectReferences()
    {
        foreach (var usedClass in _structure.UsedClasses)
        {
            usedClass.Parent?.ResolveReference(_structure);
        }
        foreach (var contentDef in _structure.ContentTable)
        {
            contentDef.id?.ResolveReference(_structure);
            contentDef.id2?.ResolveReference(_structure);
            contentDef.Group?.ResolveReference(_structure);
        }
    }

    private void LoadContents()
    {
        var textureId = FindClassId("Engine", "Texture");
        var paletteId = FindClassId("Engine", "Palette");
        var groupId = FindClassId("Core", "Package");
        foreach (var entry in _structure.ContentTable)
        {
            var group = entry.Group?.Name;
            if (entry.id == null)
            {
                _problems.Add($"Unknown content id {entry.id} with name {entry.Name}");
                continue;
            }
            int id = entry.id.Value;
            if (id == paletteId)
            {
                ReadPalette(entry);
                _structure.DebugInfo.PaletteCounter++;
            }
            else if (id == textureId)
            {
                if (!ReadImage(entry, group))
                {
                    _problems.Add($"could not load texture {entry.Name} at 0x{entry.Offset:X}");
                }
                _structure.DebugInfo.TextureCounter++;
            }
            else if (id == groupId)
            {
                continue;
            }
            else if (entry.id?.Name?.EndsWith("Texture") ?? false)
            {
                ReadProceduralImage(entry, group);
                _structure.DebugInfo.ProceduralTextureCounter++;
            }
            else if (IsClass(entry.id, "Engine", "Font"))
            {
                _structure.DebugInfo.FontCounter++;
            }
            else
            {

                _problems.Add($"Unknown ContentDefinition {entry.Name} with reference {entry.id?.ReferenceText}");
            }
        }
    }

    private bool IsClass(ObjectReference? reference, string parent, string className)
    {
        if (reference != null && reference.Obj is UsedClass c)
        {
            return c.Name == className && c.Parent != null && c.Parent.Name == parent;
        }
        return false;
    }

    private int? FindClassId(string parent, string className)
    {
        var result = _structure.UsedClasses.FindIndex(x => x.Name == className && x.Parent != null && x.Parent.Name == parent);
        var foundIndex = (result + 1) * -1;
        return (result < 0) ? null : foundIndex;
    }

    private RawData GetRaw(long start)
    {
        var end = _fs.Position;
        int length = (int)(end - start);
        _fs.Seek(start, SeekOrigin.Begin);
        var bytes = _br.ReadBytes(length);
        return new RawData(bytes, start);
    }
}
