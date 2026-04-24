using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;

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

        public CustomTreeElement(string name, string value)
        {
            Name = name;
            Value = value;
        }
        public CustomTreeElement(string name, string value, params CustomTreeElement[] childElements)
        {
            Name = name;
            Value = value;
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
                CTE("Images", file.Images.Select((x, i) => ToCTE(i, x, file.FileName)).ToArray()),
                CTE("Classes", file.UsedClasses.Select((x, i) => ToCTE(i, x)).ToArray()),
                CTE("ContentTable", file.ContentTable.Select((x, i) => ToCTE(i, x)).ToArray())
            });
            root.SpreadParent();
            return root;
        }

        private static CustomTreeElement ToCTE(int i, UsedClass c)
        {
            return new CustomTreeElement($"[{i}]", $"{c.Type} {c.Package}.{c.Name}",
                CTE("Package", $"{c.Package}"),
                CTE("Group", $"{c.Type}"),
                CTE("Id?", $"{c.id}"),
                CTE("Name", $"{c.Name}")
            );
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
            return new CustomTreeElement($"[{i}]", $"{p.Name} ({p.W * p.H})") { Palette = BitmapSource.Create(p.W, p.H, 96, 96, PixelFormats.Rgb24, null, bytes, p.W * 3) };
        }

        private static CustomTreeElement ToCTE(int i, ImageChunk p)
        {
            return new CustomTreeElement($"[{i}]", "image") { RawData = p.RawData };
        }

        private static CustomTreeElement ToCTE(int i, UString str)
        {
            return new CustomTreeElement($"[{i}]", str.Value) { RawData = str.RawData };
        }

        private static CustomTreeElement ToCTE(UProperty p)
        {
            return new CustomTreeElement(p.Name, p.Value.ToString()) { Color = UColorToColor(p.Color), RawData = $"Type: {p.Type} = 0x{p.Type:X2}" };
        }

        private static System.Windows.Media.Color? UColorToColor(UColor? color)
        {
            if(color != null) 
                return System.Windows.Media.Color.FromRgb(color.Value.r, color.Value.g, color.Value.b);
            else return null;
        }

        private static CustomTreeElement ToCTE(int i, ContentDef n)
        {

            return new CustomTreeElement($"[{i}]", $"{n.Name}",
                CTE("id1?", $"{n.id}"),
                CTE("id2?", $"{n.id2}"),
                CTE("Id3?", $"{n.id3}"),
                CTE("Id4?", $"{n.id4}"),
                CTE("Name", $"{n.Name}"),
                CTE("Size", $"{n.Size}"),
                CTE("Offset", $"0x{n.Offset:X}")
            )
            {
                RawData = n.RawData
            };
        }

        private static CustomTreeElement ToCTE(int i, MipMap m)
        {
            return new CustomTreeElement($"[{i}]", null,
                CTE("WidthUp", m.WidthUp.ToString()),
                CTE("HeightUp", m.HeightUp.ToString()),
                CTE("WidthPower", m.WidthPower.ToString()),
                CTE("HeightPower", m.HeightPower.ToString()),
                CTE("Width(calculated)", m.Width.ToString()),
                CTE("Height(calculated)", m.Height.ToString())
                ) { RawData = m.HeaderRaw };
        }

        private static CustomTreeElement ToCTE(int index, Image image, string Filename)
        {
            return new CustomTreeElement($"[{index}]", image.Name,
                CTE("EditorFullName", image.Group != null ? $"{Filename}.{image.Group}.{image.Name}" : $"{Filename}.{image.Name}", null),
                CTE("Properties", image.Properties.Select(ToCTE).ToArray()),
                CTE("Palette", image.Palette.ToString()),
                CTE("Width", image.Width.ToString()),
                CTE("Height", image.Height.ToString()),
                CTE("MipMaps", image.MipMaps.Select((x, i) => ToCTE(i, x)).ToArray())
                )
            { RawData = image.ImageHeaderRaw };
        }

        private static CustomTreeElement CTE(string name, string value, Color? color = null)
        {
            return new CustomTreeElement(name, value) { Color = color };
        }

        private static CustomTreeElement CTE(string name, params CustomTreeElement[] childElements)
        {
            return new CustomTreeElement(name, null, childElements);
        }
    }
}
