using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;
using System.Linq;

namespace UedConverter.UtxFile
{
    public class UString : ISizeable
    {
        public string Value { get; set; }
        public uint Flags { get; set; }
        public string RawData { get; set; }

        public long GetSize() => Sizeable.GetSize(Value) + sizeof(uint) + Sizeable.GetSize(RawData);
    }

    public class UProperty : ISizeable
    {
        public string Name { get; set; }
        public int Type { get; set; }
        public object Value { get; set; }
        public UColor? Color { get; set; }

        public override string ToString()
        {
            return $"UProperty( {Name} T={Type} V={Value} C={Color} )";
        }

        public long GetSize() => sizeof(int) + sizeof(int) + sizeof(int) + (Color?.GetSize() ?? 0);
    }
    public class Signature
    {
        private const long PATTERN = 0x9e2a83c1;
        public Signature(byte[] signature)
        {
            if (signature == null) throw new ArgumentNullException("Signature is null");
            if (signature.Length != 4) throw new ArgumentNullException($"Signature length ({signature.Length}) is not equal 4");
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
        public int FooterCount { get; set; }//grouping and names for images and packages
        public int FooterStart { get; set; }
        public int ClassesCount { get; set; }//some classes after images
        public int ClassesStart { get; set; }

        public string RawData { get; set; }

        public long GetSize()
        {
            return 8 * sizeof(int) + Sizeable.GetSize(RawData);
        }
    }

    public class MipMap
    {
        public int WidthUp { get; set; }
        public int HeightUp { get; set; }
        public int WidthPower { get; set; }
        public int HeightPower { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int UClamp { get; set; }
        public int VClamp { get; set; }
        public string HeaderRaw { get; set; }
        public byte[] Pixels { get; set; }
    }

    public class Image : ISizeable
    {
        public string Name { get; set; }
        public string Group { get; set; }
        public int Palette { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int Size { get => Width * Height; }
        public string ImageHeaderRaw { get; set; }
        public ImageChunk ImageData { get; set; }
        public bool IsCorrect { get; set; } = false;
        public List<UProperty> Properties { get; } = new List<UProperty>();
        public List<ImageChunk> MipMaps { get; } = new List<ImageChunk>();

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

        public long GetSize() => 6 * sizeof(int) + Sizeable.GetSize(ImageHeaderRaw) + ImageData.GetSize() + sizeof(bool) + Sizeable.GetSize(Properties) + Sizeable.GetSize(MipMaps);
    }

    public class UsedClass : ISizeable
    {
        public string Package;
        public string Type;
        public int id;
        public string Name;

        public long GetSize() => 4 * sizeof(int);
        public override string ToString() => $"{Type} {Package}.{Name} ({id})";
    }

    public class ImageChunk : ISizeable
    {
        public int Width;
        public int Height;
        public int wPower;
        public int hPower;
        public byte[] Pixels;
        public string RawData;
        public bool IsCorrect;
        public long GetSize() => 4 * sizeof(int) + Pixels.Length * sizeof(byte) + Sizeable.GetSize(RawData) + sizeof(bool);
    }

    public class Palette : ISizeable
    {
        public string Name { get; set; }
        public byte Type { get; set; }
        public byte W { get; set; }
        public byte H { get; set; }
        public UColor[] Colors { get; set; }
        public long GetSize() => 3 * sizeof(byte) + Colors.Length * sizeof(int) + sizeof(int);
    }

    public struct UColor : ISizeable
    {
        public byte r, g, b, a;
        public UColor(byte r, byte g, byte b, byte a = 255)
        {
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = a;
        }

        public static UColor FromBytes(byte[] value)
        {
            return new UColor(value[0], value[1], value[2], value[3]);
        }

        public long GetSize() => 4 * sizeof(byte);

        public override string ToString()
        {
            return $"#{r:X2}{g:X2}{b:X2}{a:X2}";
        }
    }

    public class ContentDef : ISizeable
    {
        public int id;
        public int id2;
        public string Name;
        public int id3;
        public int id4;
        public string RawData;
        public int Size;
        public int? Offset;
        public long GetSize() => 6 * sizeof(int) + Sizeable.GetSize(RawData);
        public override string ToString() => $"{Name} {id} {id2} {id3} {id4} Size:{Size} Offset:{Offset}";
    }

    public interface ISizeable
    {
        long GetSize();
    }

    public static  class Sizeable
    {
        public static long GetSize(string str) => (str != null) ? (sizeof(char) * str.Length) : 0;
        public static long GetSize<T>(List<T> lst) where T : ISizeable => (lst != null) ? lst.Sum(x => x.GetSize()) : 0;
    }

    public class Structure : ISizeable
    {
        public string FileName { get; set; }
        public Signature Signature { get; set; }
        public Header Header { get; set; }
        public List<UString> Names { get; } = new List<UString>();
        public List<Palette> Palettes { get; } = new List<Palette>();
        public List<Image> Images { get; } = new List<Image>();
        public List<UsedClass> UsedClasses { get; } = new List<UsedClass>();
        public List<ContentDef> ContentTable { get; } = new List<ContentDef>();

        public void AddName(string value, uint flags, string rawData)
        {
            Names.Add(new UString() { Value = value, Flags = flags, RawData = rawData });
        }

        public long GetSize() {
            return Sizeable.GetSize(FileName) 
                + Header.GetSize() 
                + Sizeable.GetSize(Names)
                + Sizeable.GetSize(Palettes)
                + Sizeable.GetSize(Images)
                + Sizeable.GetSize(UsedClasses)
                + Sizeable.GetSize(ContentTable);
        }
    }


    class ComplexIndex
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
    }
}
