using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace UedConverter.UtxFile;

public interface ICustomTreeElement
{
    Color? Color { get; set; }
    string Name { get; set; }
    string? Value { get; set; }
    RawData? RawData { get; set; }
    public string? DebugText { get; set; }
    ICustomTreeElement? Parent { get; set; }
    List<ICustomTreeElement> ChildElements { get; }
    void SpreadParent();
}

public class CustomTreeElement : ICustomTreeElement
{
    public Color? Color { get; set; }
    public string Name { get; set; }
    public string? Value { get; set; }
    public RawData? RawData { get; set; }
    public string? DebugText { get; set; }
    public BitmapSource? Palette { get; set; }
    public ICustomTreeElement? Parent { get; set; }
    public List<ICustomTreeElement> ChildElements { get; } = [];

    public CustomTreeElement With(Action<CustomTreeElement> action)
    {
        action.Invoke(this);
        return this;
    }


    public CustomTreeElement(string name, string? value)
    {
        Name = name;
        Value = value;
    }
    public CustomTreeElement(string name, string? value, params CustomTreeElement[] childElements)
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
        var root = new CustomTreeElement("File", file.FileName, [
            ToCTE(file.Header),
            CTEArray("Names", [.. file.Names.Select((x, i) => ToCTE(i, x))]),
            CTEArray("Palettes", [.. file.Palettes.Select((x, i) => ToCTE(i, x))]),
            CTEArray("Images", [..file.Images.Select((x, i) => ToCTE(i, x, file.FileName))]),
            CTEArray("Classes", [..file.UsedClasses.Select((x, i) => ToCTE(i, x))]),
            CTEArray("ContentTable", [..file.ContentTable.Select((x, i) => ToCTE(i, x))]).With(cte => cte.DebugText = $"Found:\nTextures:{file.DebugInfo.TextureCounter}\nPalettes:{file.DebugInfo.PaletteCounter}")
        ]);
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
        if (p.Colors != null)
        {
            for (int j = 0; j < p.Colors.Length; j++)
            {
                bytes[j * 3] = p.Colors[j].r;
                bytes[j * 3 + 1] = p.Colors[j].g;
                bytes[j * 3 + 2] = p.Colors[j].b;
            }
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
        return new CustomTreeElement(
            p.Name, p.GetValueText(),
            CTE("Name", p.Name),
            CTE("Type", UPropertyType.GetText(p.Type)),
            CTE("Value", p.GetValueText())
        )
        {
            Color = UColorToColor(p.Color),
            RawData = p.RawData
        };
    }

    private static System.Windows.Media.Color? UColorToColor(UColor? color)
    {
        if (color != null)
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
            CTE("Offset", Hex(n.Offset))
        )
        {
            RawData = n.RawData
        };
    }

    private static CustomTreeElement ToCTE(int index, Image image, string Filename)
    {
        return new CustomTreeElement($"[{index}]", image.Name,
            CTE("EditorFullName", image.Group != null ? $"{Filename}.{image.Group}.{image.Name}" : $"{Filename}.{image.Name}", null),
            CTEArray("Properties", [.. image.Properties.Select(ToCTE)]),
            CTE("Palette", image.Palette.ToString()),
            CTE("Width", image.Width.ToString()),
            CTE("Height", image.Height.ToString()),
            CTEArray("MipMaps", [.. image.MipMaps.Select((x, i) => ToCTE(i, x))])
            )
        { RawData = image.ImageHeaderRaw };
    }

    private static CustomTreeElement ToCTE(Header header)
    {
        return new CustomTreeElement("Header", null,
            CTE("UtxVersion", header.Version.ToString()),
            CTE("NameCount", header.NamesCount.ToString()),
            CTE("NameStart", Hex(header.NamesStart)),
            CTE("ClassCount", header.ClassesCount.ToString()),
            CTE("ClassStart", Hex(header.ClassesStart)),
            CTE("ContentsTableCount", header.ContentsTableCount.ToString()),
            CTE("ContentsTableStart", Hex(header.ContentsTableStart))
        );
    }

    private static CustomTreeElement CTE(string name, string value, Color? color = null)
    {
        return new CustomTreeElement(name, value) { Color = color };
    }

    private static CustomTreeElement CTE(string name, params CustomTreeElement[] childElements)
    {
        return new CustomTreeElement(name, null, childElements);
    }

    private static CustomTreeElement CTEArray(string name, params CustomTreeElement[] childElements)
    {
        return new CustomTreeElement(name, $"({childElements.Length})", childElements);
    }

    private static string Hex(long? value) => value == null ? "" : $"0x{value:X}";
}
