using System.Diagnostics;
using System.IO;
using System.Text;

namespace UedConverter.UtxFile;

public sealed class UtxReader : IDisposable
{
    private readonly string _path;
    private readonly Structure _structure;
    private readonly FileStream _fs;
    private readonly BinaryReader _br;

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
            return new ReadResult(reader._structure, e);
        }
        return new ReadResult(reader._structure);
    }

    public record ReadResult(Structure Structure, Exception? Exception = null);

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

    private void ReadPalettes()
    {
        while (true)
        {
            var type = _fs.ReadByte();
            var w = _fs.ReadByte();
            var h = _fs.ReadByte();
            if (type != 0 || w != 64 || h != 4)
            {
                _fs.Seek(-3, SeekOrigin.Current);
                return;
            }
            UColor[] colors = new UColor[w * h];
            for (int i = 0; i < w * h; i++)
            {
                colors[i] = UColor.FromBytes(_br.ReadBytes(4));
            }
            _structure.Palettes.Add(new Palette()
            {
                Type = (byte)type,
                W = (byte)w,
                H = (byte)h,
                Colors = colors,
            });
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
            Name = definition.Name
        };
        _structure.Palettes.Add(palette);
        definition.Obj = palette;
    }

    private void ReadImages()
    {
        while (true)
        {
            try
            {
                if (!ReadImage()) break;
            }
            catch (Exception)
            {
                break;
            }
        }
    }

    private bool ReadImage(int? offset = null, string? name = null, string? group = null)
    {
        if (offset != null) _fs.Seek(offset.Value, SeekOrigin.Begin);
        Image image = new()
        {
            Name = name,
            Group = group,
        };
        var start = _fs.Position;

        //skip if zeroes present
        var skipped = 0;
        while (PeekByte() == 0)
        {
            _fs.ReadByte();
            skipped++;
        }
        if (skipped > 0) Debug.WriteLine($"Skipped {skipped} zeroes at {_fs.Position - skipped:X}");

        //read image data
        while (true)
        {
            var next = PeekByte();
            if (next == 1 || next == 0)
            {
                var back = _fs.Position;
                _br.ReadByte();
                var second = _br.ReadByte();
                if (second == 0xa2)
                {
                    _br.ReadBytes(6); break;
                }
                else
                {
                    _fs.Position = back;
                }
            }
            if (next < _structure.Names.Count)
            {
                if (_structure.Names[next].Value == "Core") return false;
                image.AddProperty(ReadProperty());
            }
            else
            {
                break;
            }
        }
        var imageCount = _br.ReadByte();
        var size = image.Size;
        image.ImageHeaderRaw = GetRaw(start);
        if (size > 512 * 512 || imageCount < 1) return false;
        image.ImageData = ReadImageChunk(image.Width, image.Height);
        _structure.Images.Add(image);
        if (image.Palette < 1 || image.Palette > _structure.Palettes.Count) return false;
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

    private UProperty ReadProperty()
    {
        var start = _fs.Position;
        var name = ReadName();
        var type = _br.ReadByte();
        var property = new UProperty(name) { Type = type };
        switch (type)
        {
            case UPropertyType.Reference:
                {
                    property.Value = ReadCompactInt();
                    break;
                }
            case UPropertyType.Int: { property.Value = _br.ReadInt32(); break; }
            case UPropertyType.IntIndex:
                {
                    property.Value = UColor.FromBytes(_br.ReadBytes(5));
                    break;
                }
            case UPropertyType.Color:
                {
                    property.Value = ReadCompactIndexName();
                    property.Color = UColor.FromBytes(_br.ReadBytes(4));
                    break;
                }
            default: { property.Value = _br.ReadByte(); break; }
        }
        property.RawData = GetRaw(start);
        return property;
    }

    private ImageChunk ReadImageChunk(int width, int height)
    {
        var start = _fs.Position;
        var image = new ImageChunk();
        var calculatedSize = width * height;
        var size = calculatedSize;
        if (_structure.Header.Version >= 64)
            _br.ReadInt32();//unknown variable
        size = ReadCompactInt();

        image.RawData = GetRaw(start);
        image.Pixels = _br.ReadBytes(width * height);
        image.Width = _br.ReadInt32();
        image.Height = _br.ReadInt32();
        image.wPower = _br.ReadByte();
        image.hPower = _br.ReadByte();
        image.IsCorrect = calculatedSize == size && image.Width == width && image.Height == height;
        return image;
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
                    id = _br.ReadInt32(),
                    Name = ReadCompactIndexName()
                });
        }
    }

    private string ReadName()
    {
        var idx = _br.ReadByte();
        if (idx >= _structure.Names.Count || idx < 0) return $"Index out of bounds({idx}/{_structure.Names.Count})";
        return _structure.Names[idx].Value;
    }

    private string ReadCompactIndexName()
    {
        var idx = ReadCompactInt();
        if (idx >= _structure.Names.Count || idx < 0) return $"Index out of bounds({idx}/{_structure.Names.Count})";
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
            var type = ReadCompactInt();
            var type2 = ReadCompactInt();
            var id = _br.ReadInt32();
            var name = ReadCompactIndexName();
            var idk = _br.ReadInt32();
            var size = ReadCompactInt();
            int? offset = null;
            if (size > 0)
            {
                offset = ReadCompactInt();
            }
            _structure.ContentTable.Add(new ContentDef() { id = type, id2 = type2, Name = name, id3 = id, id4 = idk, Size = size, Offset = offset, RawData = GetRaw(nStart) });
        }
    }

    private void LoadContents()
    {
        var textureId = FindClassId("Texture");
        var paletteId = FindClassId("Palette");
        if (textureId == null || paletteId == null) return;
        foreach (var entry in _structure.ContentTable)
        {
            var group = FindContentDefName(entry.id3);
            if (entry.id == paletteId)
            {
                ReadPalette(entry);
                _structure.DebugInfo.PaletteCounter++;
            }
            else if (entry.id == textureId)
            {
                ReadImage(entry.Offset, entry.Name, group);
                _structure.DebugInfo.TextureCounter++;
            }
        }
    }

    private int? FindClassId(string className)
    {
        var result = _structure.UsedClasses.FindIndex(x => x.Package == "Core" && x.Type == "Class" && x.Name == className);
        return result > 0 ? (result + 1) * -1 : null;
    }

    private string? FindContentDefName(int? id)
    {
        if (id == null) return null;
        if (id < 0) return "Error: Cannot evaluate content id below 0";
        if (id > _structure.ContentTable.Count) return "Error: Cannot evaluate content id above limit";
        if (id == 0) return null;
        var result = _structure.ContentTable[(int)id - 1];
        return result.Name;
    }

    private byte PeekByte()
    {
        var @byte = _br.ReadByte();
        _fs.Position -= 1;
        return @byte;
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
