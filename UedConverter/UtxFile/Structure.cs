using System.IO;
using System.Text;

namespace UedConverter.UtxFile;

public class UString(string value) : ISizeable
{
    public string Value { get; set; } = value;
    public uint Flags { get; set; }
    public RawData? RawData { get; set; }

    public long GetSize() => Sizeable.GetSize(Value) + sizeof(uint) + Sizeable.GetSize(RawData);

    public override string ToString() => $"UString(\"{Value}\")";
}

public class UProperty(string name) : ISizeable
{
    public string Name { get; set; } = name;
    public int Type { get; set; }
    public object? Value { get; set; }
    public UColor? Color { get; set; }
    public RawData? RawData { get; set; }

    public override string ToString()
    {
        return $"UProperty( {Name} T={Type} V={Value} C={Color} )";
    }

    public long GetSize() => sizeof(int) + sizeof(int) + sizeof(int) + Sizeable.GetSize(Color);

    public string GetTypeText() => UPropertyType.GetText(Type);
    public string GetValueText()
    {
        return Type switch
        {
            _ => $"{Value}"
        };
    }
}

public static class UPropertyType
{
    public const byte Reference = 5;
    public const byte NameRef = 21;
    public const byte Color = 42;
    public const byte IntIndex = 162;
    public const byte Int = 34;

    public static string GetText(int type)
    {
        return type switch
        {
            Reference => "Reference",
            NameRef => "NameRef",
            Color => "Color",
            IntIndex => "IntIndex",
            Int => "Int",
            _ => $"UnknownType 0x{type:X} ({type})",
        };
    }
}

public class Signature
{
    private const long PATTERN = 0x9e2a83c1;
    public Signature(byte[]? signature)
    {
        ArgumentNullException.ThrowIfNull(signature);
        if (signature.Length != 4) throw new ArgumentException($"Signature length ({signature.Length}) is not equal 4");
        //convertToHex
        var signatireNumber = BitConverter.ToUInt32(signature, 0);
        if (signatireNumber != PATTERN) throw new ArgumentException($"Incorrect signature [ {signature[0]:x} {signature[1]:x} {signature[2]:x} {signature[3]:x} ] should be: [ c1 83 2a 9e ]");
    }
}

public class Header : ISizeable
{
    public int Version { get; set; }
    public int UnknowField1 { get; set; }
    public int NamesStart { get; set; }
    public int NamesCount { get; set; }
    public int ContentsTableCount { get; set; }//grouping and names for images and packages
    public int ContentsTableStart { get; set; }
    public int ClassesCount { get; set; }//some classes after images
    public int ClassesStart { get; set; }

    public RawData? RawData { get; set; }

    public long GetSize()
    {
        return 8 * sizeof(int) + Sizeable.GetSize(RawData);
    }
}

public class Image : ISizeable
{
    public string? Name { get; set; }
    public string? Group { get; set; }
    public int Palette { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public int Size { get => Width * Height; }
    public RawData? ImageHeaderRaw { get; set; }
    public ImageChunk? ImageData { get; set; }
    public bool IsCorrect { get; set; } = false;
    public List<UProperty> Properties { get; } = [];
    public List<ImageChunk> MipMaps { get; } = [];

    public void AddProperty(UProperty property)
    {
        Properties.Add(property);
        switch (property.Name)
        {
            case "USize": Width = Convert.ToInt32(property.Value); break;
            case "VSize": Height = Convert.ToInt32(property.Value); break;
            case "Palette": Palette = Convert.ToInt32(property.Value); break;
        }
    }

    public long GetSize() => 6 * sizeof(int) + Sizeable.GetSize(ImageHeaderRaw) + Sizeable.GetSize(ImageData) + sizeof(bool) + Sizeable.GetSize(Properties) + Sizeable.GetSize(MipMaps);
}

public class UsedClass : ISizeable
{
    public string? Package;
    public string? Type;
    public int? id;
    public string? Name;

    public long GetSize() => 4 * sizeof(int);
    public override string ToString() => $"{Type} {Package}.{Name} ({id})";
}

public class ImageChunk : ISizeable
{
    public int Width;
    public int Height;
    public int wPower;
    public int hPower;
    public byte[]? Pixels;
    public RawData? RawData;
    public bool IsCorrect;
    public long GetSize() => 4 * sizeof(int) + Sizeable.GetSize(Pixels) * sizeof(byte) + Sizeable.GetSize(RawData) + sizeof(bool);
}

public class Palette : ISizeable
{
    public string? Name { get; set; }
    public byte Type { get; set; }
    public byte W { get; set; }
    public byte H { get; set; }
    public UColor[]? Colors { get; set; }
    public long GetSize() => 3 * sizeof(byte) + Sizeable.GetSize(Colors) * sizeof(int) + sizeof(int);
}

public struct UColor(byte r, byte g, byte b, byte a = 255) : ISizeable
{
    public byte r = r, g = g, b = b, a = a;

    public static UColor FromBytes(byte[] value)
    {
        return new UColor(value[0], value[1], value[2], value[3]);
    }

    public static UColor FromInt(int value)
    {
        var r = (value & 0xFF000000) >> 6;
        var g = (value & 0x00FF0000) >> 4;
        var b = (value & 0x0000FF00) >> 2;
        var a = value & 0x000000FF;
        return new UColor((byte)r, (byte)g, (byte)b, (byte)a);
    }

    public readonly long GetSize() => 4 * sizeof(byte);

    public override readonly string ToString()
    {
        return $"#{r:X2}{g:X2}{b:X2}{a:X2}";
    }
}

public class ContentDef : ISizeable
{
    public int? id;
    public int? id2;
    public string? Name;
    public int? id3;
    public int? id4;
    public RawData? RawData;
    public int? Size;
    public int? Offset;

    public object? Obj;
    public long GetSize() => 6 * sizeof(int) + Sizeable.GetSize(RawData);
    public override string ToString() => $"{Name} {id} {id2} {id3} {id4} Size:{Size} Offset:{Offset}";
}

public interface ISizeable
{
    long GetSize();
}

public static class Sizeable
{
    public static long GetSize(ISizeable? v) => v?.GetSize() ?? 0;
    public static long GetSize(string? str) => (str != null) ? (sizeof(char) * str.Length) : 0;
    public static long GetSize<T>(List<T>? lst) where T : ISizeable => (lst != null) ? lst.Sum(x => x.GetSize()) : 0;
    public static long GetSize<T>(T[]? lst) where T : ISizeable => (lst != null) ? lst.Sum(x => x.GetSize()) : 0;
    public static long GetSize(byte[]? lst) => (lst != null) ? (lst.Length * sizeof(byte)) : 0;
}

public class Structure(string filename) : ISizeable
{
    public string FileName { get; set; } = filename;
    public Signature? Signature { get; set; }
    public Header Header { get; set; } = new();
    public List<UString> Names { get; } = [];
    public List<Palette> Palettes { get; } = [];
    public List<Image> Images { get; } = [];
    public List<UsedClass> UsedClasses { get; } = [];
    public List<ContentDef> ContentTable { get; } = [];
    public DebugInfo DebugInfo { get; } = new();

    public void AddName(string value, uint flags, RawData rawData)
    {
        Names.Add(new UString(value) { Flags = flags, RawData = rawData });
    }

    public Palette? GetPalette(int index)
    {
        var idx = index - 1;
        if (idx >= 0)
        {
            if (idx < ContentTable.Count && ContentTable[idx].Obj is Palette palette)
            {
                return palette;
            }
            else if (idx < Palettes.Count)
            {
                return Palettes[index - 1];
            }
        }
        return null;
    }

    public long GetSize()
    {
        return Sizeable.GetSize(FileName)
            + Sizeable.GetSize(Header)
            + Sizeable.GetSize(Names)
            + Sizeable.GetSize(Palettes)
            + Sizeable.GetSize(Images)
            + Sizeable.GetSize(UsedClasses)
            + Sizeable.GetSize(ContentTable);
    }
}

public class DebugInfo
{
    public int TextureCounter = 0;
    public int PaletteCounter = 0;
}

class CompactIndex
{
    // https://en.wikipedia.org/wiki/Variable-length_quantity (Wiki shows wrong order of bit with sign) -> https://web.archive.org/web/20100820185656/http://unreal.epicgames.com/Packages.htm
    private const int firstOctetValueMask = 0b0011_1111;
    private const int nextValueMask = 0b1000_0000;
    private const int valueMask = 0b0111_1111;
    public static int Read(BinaryReader reader)
    {
        byte o1 = reader.ReadByte();
        var negative = Has(o1, 0x80);
        int value = o1 & firstOctetValueMask;
        if (Has(o1, 0x40))
        {
            byte o2 = reader.ReadByte();
            value += (o2 & valueMask) << 6;
            if (HasNext(o2))
            {
                byte o3 = reader.ReadByte();
                value += (o3 & valueMask) << (6 + 7);

                if (HasNext(o3))
                {
                    byte o4 = reader.ReadByte();
                    value += (o4 & valueMask) << (6 + 7 * 2);

                    if (HasNext(o4))
                    {
                        byte o5 = reader.ReadByte();
                        value += (o5 & valueMask) << (6 + 7 * 3);
                    }
                }
            }
        }
        return negative ? value * -1 : value;
    }
    private static bool HasNext(byte b) => (b & nextValueMask) != 0;
    private static bool Has(byte b, byte c) => (b & c) != 0;

    public static int ParseString(string str)//used in immediate window to easily convert numbers
    {
        List<byte> bytes = [];
        byte? upper = null;
        for (int i = 0; i < str.Length; i++)
        {
            byte c = (byte)char.ToUpper(str[i]);
            byte? v = null;
            if (c >= 'A' && c <= 'Z')
            {
                v = (byte)(c - ((byte)'A') + 10);
            }
            else if (c >= '0' && c <= '9')
            {
                v = (byte)(c - ((byte)'0'));
            }
            if (v != null)
            {
                if (upper == null)
                {
                    upper = v;
                }
                else
                {
                    bytes.Add((byte)((upper << 4) | v));
                    upper = null;
                }
            }
        }
        using var ms = new MemoryStream([.. bytes]);
        using var br = new BinaryReader(ms);
        return Read(br);
    }
}

public class RawData(byte[] data, long location) : ISizeable
{
    public readonly byte[] Data = data;
    public readonly long Location = location;

    public override string ToString() => $"RawData at 0x{Location:X} , len({Data.Length})";

    public long GetSize() => Data.Length + sizeof(long);

    public string GetText()
    {
        StringBuilder sb = new();
        int startAt = (int)(Location % 16);
        if (startAt != 0)
        {
            WriteAddress(sb, Location - startAt);
            for (int i = 0; i < startAt; i++)
            {
                sb.Append(" __");
                if (i == 7) sb.Append(' ');
            }
        }
        for (int i = 0; i < Data.Length; i++)
        {
            var lineLocation = (startAt + i) % 16;
            if (lineLocation == 0)
            {
                WriteAddress(sb, Location + i);
            }
            sb.Append(' ');
            sb.Append(Data[i].ToString("X2"));
            if (lineLocation == 7) sb.Append(' ');
            else if (lineLocation == 15) sb.AppendLine();
        }
        return sb.ToString();
    }

    private static void WriteAddress(StringBuilder sb, long address)
    {
        sb.Append("0x");
        sb.Append(address.ToString("X8"));
        sb.Append(": ");
    }
}
