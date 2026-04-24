using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace UedConverter.UtxFile
{
    public class UtxReader
    {
        private string _path;
        private Structure _structure;
        private FileStream _fs;
        private BinaryReader _br;

        private UtxReader(string path)
        {
            _path = path;
            _structure = new Structure();
        }

        private void Read()
        {
            _structure.FileName = Path.GetFileNameWithoutExtension(_path);
            using (_fs = new FileStream(_path, FileMode.Open, FileAccess.Read))
            {
                using (_br = new BinaryReader(_fs))
                {
                    _structure.Signature = new Signature(_br.ReadBytes(4));
                    _structure.Header = new Header()
                    {
                        Version = _br.ReadInt32(),
                        UnknowField1 = _br.ReadInt32(),
                        NamesCount = _br.ReadInt32(),
                        NamesStart = _br.ReadInt32(),
                        FooterCount = _br.ReadInt32(),
                        FooterStart = _br.ReadInt32(),
                        ClassesCount = _br.ReadInt32(),
                        ClassesStart = _br.ReadInt32(),
                    };

                    _fs.Seek(_structure.Header.NamesStart, SeekOrigin.Begin);

                    ReadNames();
                    ReadUsedClasses();
                    ReadContentsTable();

                    LoadContents();
                }
            }
        }

        private void ReadNames()
        {
            _fs.Seek(_structure.Header.NamesStart, SeekOrigin.Begin);
            for (int i = 0; i < _structure.Header.NamesCount; i++)
            {
                var start = _fs.Position;
                if (_structure.Header.Version > 67)
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

        private void ReadPalette(int? offset = null, string name = null)
        {
            if (offset != null) _fs.Seek(offset.Value, SeekOrigin.Begin);

            var type = _fs.ReadByte();
            var w = _fs.ReadByte();
            var h = _fs.ReadByte();
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
                Name = name
            });
        }

        private void ReadImages()
        {
            while (true)
            {
                try
                {
                    if (!ReadImage()) break;
                }
                catch (Exception e)
                {
                    break;
                }
            }
        }

        private bool ReadImage(int? offset = null, string name = null, string group = null)
        {
            if (offset != null) _fs.Seek(offset.Value, SeekOrigin.Begin);
            Image image = new Image()
            {
                Name = name,
                Group = group,
            };
            bool correctImage = false;
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
                if (PeekByte() == 0) _fs.ReadByte();
                var next = PeekByte();
                if (next == 1)
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
            var name = ReadName();
            var type = _br.ReadByte();
            var property = new UProperty() { Name = name, Type = type };
            switch (type)
            {
                case 34: { property.Value = _br.ReadInt32(); break; }
                case 162:
                case 42:
                    {
                        property.Value = _br.ReadByte();
                        property.Color = UColor.FromBytes(_br.ReadBytes(4));
                        break;
                    }
                default: { property.Value = _br.ReadByte(); break; }
            }
            return property;
        }

        private ImageChunk ReadImageChunk(int width, int height)
        {
            var start = _fs.Position;
            var image = new ImageChunk();
            var size = width * height;
            _br.ReadBytes(5);
            if (size > 32)
            {
                //extra1
                _br.ReadByte();
            }
            if (size >= 128 * 64)
            {
                //extra2
                _br.ReadByte();
            }
            image.RawData = GetRaw(start);
            image.Pixels = _br.ReadBytes(width * height);
            image.Width = _br.ReadInt32();
            image.Height = _br.ReadInt32();
            image.wPower = _br.ReadByte();
            image.hPower = _br.ReadByte();
            image.IsCorrect = image.Width == width && image.Height == height;
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
            var idx = ReadCompactIndex();
            if (idx >= _structure.Names.Count || idx < 0) return $"Index out of bounds({idx}/{_structure.Names.Count})";
            return _structure.Names[idx].Value;
        }

        private int ReadCompactIndex()
        {
            return ComplexIndex.Read(_br);
        }

        private void ReadContentsTable()
        {
            _fs.Seek(_structure.Header.FooterStart, SeekOrigin.Begin);

            int counter = 0;
            for (int i = 0; i < _structure.Header.FooterCount; i++)
            {
                counter++;
                var nStart = _fs.Position;
                var type = ReadCompactIndex();
                var type2 = ReadCompactIndex();
                var id = _br.ReadInt32();
                var name = ReadCompactIndexName();
                var idk = _br.ReadInt32();
                var size = ReadCompactIndex();
                int? offset = null;
                if (size > 0)
                {
                    offset = ReadCompactIndex();
                }
                _structure.ContentTable.Add(new ContentDef() { id = type, id2 = type2, Name = name, id3 = id, id4 = idk, Size = size, Offset = offset, RawData = GetRaw(nStart) });
            }
        }

        private void LoadContents()
        {
            try
            {
                var textureId = FindClassId("Texture");
                var paletteId = FindClassId("Palette");
                foreach (var entry in _structure.ContentTable)
                {
                    var group = FindContentDefName(entry.id3);
                    if (entry.id == paletteId)
                    {
                        ReadPalette(entry.Offset, entry.Name);
                    }
                    else if (entry.id == textureId)
                    {
                        ReadImage(entry.Offset, entry.Name, group);
                    }
                }
            }
            catch (NullReferenceException ex)
            {
                return;
            }
        }

        private int FindClassId(string className)
        {
            var result = _structure.UsedClasses.FindIndex(x => x.Package == "Core" && x.Type == "Class" && x.Name == className);
            return result > 0 ? (result + 1) * -1 : throw new NullReferenceException($"Required class ({className}) cannot be found");
        }

        private string FindContentDefName(int id)
        {
            if (id < 0) return "Error: Cannot evaluate content id below 0";
            if (id > _structure.ContentTable.Count) return "Error: Cannot evaluate content id above limit";
            if (id == 0) return null;
            var result = _structure.ContentTable[id - 1];
            return result.Name;
        }

        private byte PeekByte()
        {
            _br.ReadByte();
            _fs.Position -= 1;
            return _br.ReadByte();
        }

        private bool EOF() => _fs.Position >= _fs.Length;

        public static Structure ReadFile(string path)
        {
            var reader = new UtxReader(path);
            reader.Read();
            return reader._structure;
        }

        private static string ToRaw(byte[] bytes)
        {
            StringBuilder sb = new StringBuilder();
            byte byteCount = 0;
            foreach (byte b in bytes)
            {
                sb.Append(b.ToString("X").PadLeft(2, '0'));
                sb.Append(' ');
                byteCount++;
                if (byteCount == 8)
                {
                    sb.Append(' ');
                }
                if (byteCount > 15)
                {
                    byteCount = 0;
                    sb.AppendLine();
                }
            }
            return sb.ToString();
        }

        private string GetRaw(long start)
        {
            var end = _fs.Position;
            int length = (int)(end - start);
            _fs.Seek(start, SeekOrigin.Begin);
            var bytes = _br.ReadBytes(length);
            return ToRaw(bytes);
        }

        public static Structure GetExample()
        {
            var structure = new Structure()
            {
                Signature = new Signature(new byte[] { 0xc1, 0x83, 0x2a, 0x9e }),
                Header = new Header() { Version = 67, NamesCount = 6, NamesStart = 95, RawData = "45 00 00 00 01 00 00 00 1A 00 00 00 40 00 00 00 08 00 00 00 46 C5 01 00 05 00 00 00" },
            };

            structure.Names.Add(new UString() { Value = "None", RawData = "05 4E 6F 6E 65 00 10 04 07 04" });
            structure.Names.Add(new UString() { Value = "Palette", RawData = "08 50 61 6C 65 74 74 65 00 10 00 07 00" });
            structure.Names.Add(new UString() { Value = "bAutoVPan", RawData = "0A 62 41 75 74 6F 56 50 61 6E 00 10 00 07 00" });
            structure.Names.Add(new UString() { Value = "Palette1", RawData = "09 50 61 6C 65 74 74 65 31 00 10 00 07 00" });
            structure.Names.Add(new UString() { Value = "Group1", RawData = "07 48 6D 61 67 65 31 00 10 00 07 00" });
            structure.Names.Add(new UString() { Value = "Image1", RawData = "07 48 6D 61 67 65 31 00 10 00 07 00" });

            return structure;
        }
    }
}
