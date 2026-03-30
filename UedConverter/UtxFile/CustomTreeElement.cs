using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

namespace UedConverter.UtxFile
{
    public interface ICustomTreeElement
    {
        Color? Color { get; set; }
        string Name { get; set; }
        string Value { get; set; }
        string RawData { get; set; }
        ICustomTreeElement Parent { get; set; }
        List<ICustomTreeElement> ChildElements { get; }
        void SpreadParent();
    }

    public class CustomTreeElement : ICustomTreeElement
    {
        public Color? Color { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
        public string RawData { get; set; }
        public BitmapSource Palette { get; set; }
        public ICustomTreeElement Parent { get; set; }
        public List<ICustomTreeElement> ChildElements { get; } = new List<ICustomTreeElement>();

        public CustomTreeElement(string name, string value, Color? color)
        {
            Name = name;
            Value = value;
            Color = color;
        }
        public CustomTreeElement(string name, Color? color, params CustomTreeElement[] childElements)
        {
            Name = name;
            Color = color;
            ChildElements.AddRange(childElements);
        }

        public void SpreadParent()
        {
            foreach (var c in ChildElements)
            {
                c.Parent = this;
                c.SpreadParent();
            }
        }

        public static ICustomTreeElement BuildTreeFromFile(Structure file)
        {
            var root = CTE("File", new CustomTreeElement[] {
                CTE("Header", CTE("NameCount", file.Header.NamesCount.ToString()), CTE("nameStart", file.Header.NamesStart.ToString())),
                CTE("Names", file.Names.Select((x, i) => ToCTE(i, x)).ToArray()),
                CTE("Palettes", file.Palettes.Select((x, i) => ToCTE(i, x)).ToArray()),
                CTE("Images", file.Images.Select((x, i) => ToCTE(i, x)).ToArray())
            });
            root.SpreadParent();
            return root;
        }

        private static CustomTreeElement ToCTE(int i, Palette p)
        {
            var bytes = new byte[p.W * p.H * 3];
            for (int j = 0; j < p.Colors.Length; j++)
            {
                bytes[j * 3] = p.Colors[j].r;
                bytes[j * 3 + 1] = p.Colors[j].g;
                bytes[j * 3 + 2] = p.Colors[j].b;
            }
            return new CustomTreeElement($"[{i}]", (p.W * p.H).ToString(), null) { Palette = BitmapSource.Create(p.W, p.H, 96, 96, PixelFormats.Rgb24, null, bytes, p.W * 3) };
        }

        private static CustomTreeElement ToCTE(int i, ImageChunk p)
        {
            return new CustomTreeElement($"[{i}]", "image", null) { RawData = p.RawData };
        }

        private static CustomTreeElement ToCTE(int i, UString str)
        {
            return new CustomTreeElement($"[{i}]", str.Value, null) { RawData = str.RawData };
        }

        private static CustomTreeElement ToCTE(UProperty p)
        {
            return new CustomTreeElement(p.Name, p.Value.ToString(), UColorToColor(p.Color)) { RawData = $"Type: {p.Type} = 0x{p.Type:X2}" };
        }

        private static System.Windows.Media.Color? UColorToColor(UColor? color)
        {
            if(color != null) 
                return System.Windows.Media.Color.FromRgb(color.Value.r, color.Value.g, color.Value.b);
            else return null;
        }

        private static CustomTreeElement ToCTE(int i, MipMap m)
        {
            return new CustomTreeElement($"[{i}]", null,
                CTE("WidthUp", m.WidthUp.ToString(), null),
                CTE("HeightUp", m.HeightUp.ToString(), null),
                CTE("WidthPower", m.WidthPower.ToString(), null),
                CTE("HeightPower", m.HeightPower.ToString(), null),
                CTE("Width(calculated)", m.Width.ToString(), null),
                CTE("Height(calculated)", m.Height.ToString(), null)
                ) { RawData = m.HeaderRaw };
        }

        private static CustomTreeElement ToCTE(int index, UtxFile.Image image)
        {
            return new CustomTreeElement($"[{index}]", null,
                CTE("Properties", image.Properties.Select(ToCTE).ToArray()),
                CTE("Palette", image.Palette.ToString(), null),
                CTE("Width", image.Width.ToString(), null),
                CTE("Height", image.Height.ToString(), null),
                CTE("MipMaps", image.MipMaps.Select((x, i) => ToCTE(i, x)).ToArray())
                )
            { RawData = image.ImageHeaderRaw };
        }

        private static CustomTreeElement CTE(string name, string value, Color? color = null)
        {
            return new CustomTreeElement(name, value, color);
        }

        private static CustomTreeElement CTE(string name, params CustomTreeElement[] childElements)
        {
            return new CustomTreeElement(name, null, childElements);
        }
    }
}
