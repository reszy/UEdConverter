using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace UedConverter.UtxFile
{
    public class UString
    {
        public string Value { get; set; }
        public uint Flags { get; set; }
        public string RawData { get; set; }
    }

    public class UProperty
    {
        public string Name { get; set; }
        public int Type { get; set; }
        public object Value { get; set; }
        public UColor? Color { get; set; }

        public override string ToString()
        {
            return $"UProperty( {Name} T={Type} V={Value} C={Color} )";
        }
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

    public class Header
    {
        public int Version { get; set; }
        public int NamesStart { get; set; }
        public int NamesCount { get; set; }

        public string RawData { get; set; }
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

    public class Image
    {
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
    }

    public class ImageChunk
    {
        public int Width;
        public int Height;
        public int wPower;
        public int hPower;
        public byte[] Pixels;
        public string RawData;
        public bool IsCorrect;
    }

    public class Palette
    {
        public byte Type { get; set; }
        public byte W { get; set; }
        public byte H { get; set; }
        public UColor[] Colors { get; set; }
    }

    public struct UColor
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

        public override string ToString()
        {
            return $"#{r:X2}{g:X2}{b:X2}{a:X2}";
        }
    }

    public class Structure
    {
        public Signature Signature { get; set; }
        public Header Header { get; set; }
        public List<UString> Names { get; } = new List<UString>();
        public List<Palette> Palettes { get; } = new List<Palette>();
        public List<Image> Images { get; } = new List<Image>();

        public void AddName(string value, uint flags, string rawData)
        {
            Names.Add(new UString() { Value = value, Flags = flags, RawData = rawData });
        }
    }
}
