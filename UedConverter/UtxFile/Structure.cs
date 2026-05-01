using System.IO;
using System.Text;

namespace UedConverter.UtxFile;

public class UString(string value)
{
    public string Value { get; set; } = value;
    public uint Flags { get; set; }
    public RawData? RawData { get; set; }

    public override string ToString() => $"UString(\"{Value}\")";
}

public class UProperty(string name)
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

    public string GetTypeText() => UPropertyType.GetText(Type);
}

public class ObjectReference(int value)
{
    public int Value { get; } = value;

    public string? ReferenceText { get; set; } = null;
    public string? Name { get; set; } = null;
    public object? Obj { get; set; } = null;

    public override string ToString() => $"ObjId: ({Value}) -> {ReferenceText}";

    public void ResolveReference(Structure structure)
    {
        if (Value == 0) ReferenceText = "null";
        if (Value < 0)
        {
            var classDef = structure.UsedClasses[Value * -1 - 1];
            ReferenceText = classDef.ToString();
            if (classDef?.Parent?.Name != null)
            {
                Name = $"{classDef.Parent.Name}.{classDef.Name}";
            }
            else
            {
                Name = classDef?.Name;
            }
            Obj = classDef;
        }
        if (Value > 0)
        {
            var contentDef = structure.ContentTable[Value - 1];
            ReferenceText = contentDef.Name;
            Name = contentDef.Name;
            Obj = contentDef;
        }
    }
};

public static class UPropertyType
{
    public const byte Struct = 90;
    public const byte StructIndex = 218;
    public const byte Boolean = 211;
    public const byte ByteEnumerated = 1;
    public const byte Reference = 5;
    public const byte NameRef = 21;
    public const byte Color = 42;
    public const byte IntIndex = 162;
    public const byte Int = 34;
    public const byte Float = 36;

    public static string GetText(int type)
    {
        return type switch
        {
            Boolean => "Boolean",
            Struct => "Struct",
            StructIndex => "StructIndex",
            ByteEnumerated => "Boolean",
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

public class Header
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
}

public class UProperties: List<UProperty>
{
    public UProperty? Find(string propertyName) => this.Find(p => p.Name == propertyName);
    public T? GetRef<T>(string propertyName) where T : class
    {
        var property = this.Find(p => p.Name == propertyName)?.Value;
        if (property is ObjectReference objRef && objRef.Obj is ContentDef contentDef && contentDef.Obj is T requestedValue)
        {
            return requestedValue;
        }
        else return null;
    }
    public T? GetObjectValue<T>(string propertyName) where T : class
    {
        var property = this.Find(p => p.Name == propertyName)?.Value;
        if (property is T requestedValue)
        {
            return requestedValue;
        }
        else return null;
    }
    public T? GetValue<T>(string propertyName) where T : struct
    {
        var property = this.Find(p => p.Name == propertyName)?.Value;
        if (property is T requestedValue)
        {
            return requestedValue;
        }
        else return null;
    }
}

public class Image(string? name = null, string? group = null)
{
    public string? Name { get; set; } = name;
    public string? Group { get; set; } = group;
    public int Width { get; set; }
    public int Height { get; set; }
    public int Size { get => Width * Height; }
    public RawData? ImageHeaderRaw { get; set; }
    public ImageChunk? ImageData { get; set; }
    public bool IsCorrect { get; set; } = false;
    public UProperties Properties { get; } = [];
    public List<ImageChunk> MipMaps { get; } = [];

    public void AddProperty(UProperty property)
    {
        Properties.Add(property);
        switch (property.Name)
        {
            case "USize": Width = Convert.ToInt32(property.Value); break;
            case "VSize": Height = Convert.ToInt32(property.Value); break;
        }
    }

}

public class ProceduralImage(string? name = null, string? group = null)
{
    public string? Name { get; set; } = name;
    public string? Group { get; set; } = group;
    public int PaletteIndex { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public int Size { get => Width * Height; }
    public RawData? ImageHeaderRaw { get; set; }
    public UProperties Properties { get; } = [];

    public void AddProperty(UProperty property)
    {
        Properties.Add(property);
        switch (property.Name)
        {
            case "USize": Width = Convert.ToInt32(property.Value); break;
            case "VSize": Height = Convert.ToInt32(property.Value); break;
        }
    }

}

public class ImageChunk
{
    public int Width;
    public int Height;
    public int wPower;
    public int hPower;
    public byte[]? Pixels;
    public RawData? RawData;
    public bool IsCorrect;
}

public class Palette
{
    public string? Name { get; set; }
    public byte Type { get; set; }
    public byte W { get; set; }
    public byte H { get; set; }
    public UColor[]? Colors { get; set; }
}

public struct UColor(byte r, byte g, byte b, byte a = 255)
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

    public override readonly string ToString()
    {
        return $"#{r:X2}{g:X2}{b:X2}{a:X2}";
    }

    public System.Windows.Media.Color? ToColor()
    {
        return System.Windows.Media.Color.FromRgb(r, g, b);
    }
}

public class UsedClass
{
    public string? Package;
    public string? Type;
    public ObjectReference? Parent;
    public string? Name;

    public override string ToString() => Parent?.Name == null ? $"{Type} {Package} '{Name}'" : $"{Type} {Package} '{Parent.Name}.{Name}'";
}

public class ContentDef
{
    public ObjectReference? id;
    public ObjectReference? id2;
    public string? Name;
    public ObjectReference? Group;
    public int? id4;
    public RawData? RawData;
    public int? Size;
    public int? Offset;

    public object? Obj;
    public override string ToString() => $"{Name} {id} {id2} {Group} {id4} Size:{Size} Offset:{Offset}";
}

public class Structure(string filename)
{
    public string FileName { get; set; } = filename;
    public Signature? Signature { get; set; }
    public Header Header { get; set; } = new();
    public List<UString> Names { get; } = [];
    public List<Palette> Palettes { get; } = [];
    public List<Image> Images { get; } = [];
    public List<ProceduralImage> ProceduralImages { get; } = [];
    public List<UsedClass> UsedClasses { get; } = [];
    public List<ContentDef> ContentTable { get; } = [];
    public DebugInfo DebugInfo { get; } = new();

    public void AddName(string value, uint flags, RawData rawData)
    {
        Names.Add(new UString(value) { Flags = flags, RawData = rawData });
    }
}

public class DebugInfo
{
    public int FontCounter = 0;
    public int ProceduralTextureCounter = 0;
    public int TextureCounter = 0;
    public int PaletteCounter = 0;

    public string GetContentTableText() => 
        $"Found:" +
        $"\nFonts:{FontCounter}" +
        $"\nProceduralTextures:{ProceduralTextureCounter}" +
        $"\nTextures:{TextureCounter}" +
        $"\nPalettes:{PaletteCounter}";
}

class CompactIndex
{
    // https://en.wikipedia.org/wiki/Variable-length_quantity -> https://web.archive.org/web/20100820185656/http://unreal.epicgames.com/Packages.htm
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

public class RawData(byte[] data, long location)
{
    public readonly byte[] Data = data;
    public readonly long Location = location;

    public override string ToString() => $"RawData at 0x{Location:X} , len({Data.Length})";

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
