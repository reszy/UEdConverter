using System.Runtime.CompilerServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace UedConverter.UtxFile;

public class CustomTreeElement
{
    public string Name { get; set; }
    public Color? Color { get; set; }
    public BitmapSource? InlineImage { get; set; }
    public string? Value { get; set; }
    public int? ArrayIndex { get; set; }
    public RawData? RawData { get; set; }
    public string? DebugText { get; set; }
    public CustomTreeElement? Parent { get; set; }
    public List<CustomTreeElement> ChildElements { get; } = [];

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
    private CustomTreeElement(string name, string? value, List<CustomTreeElement> childElements)
    {
        Name = name;
        Value = value;
        ChildElements = childElements;
    }

    public CustomTreeElement With(Action<CustomTreeElement> action)
    {
        action.Invoke(this);
        return this;
    }

    public void SpreadParent()
    {
        foreach (var c in ChildElements)
        {
            c.Parent = this;
            c.SpreadParent();
        }
    }

    public static CustomTreeElement BuildTreeFromFile(Structure file)
    {
        var root = new CustomTreeElement("File", file.FileName,
            ToCTE(file.Header),
            CTEArray("Names", file.Names, (x, i) => ToCTE(i, x)),
            CTEArray("Palettes", file.Palettes, (x, i) => ToCTE(i, x)),
            CTEArray("Images", file.Images, (x, i) => ToCTE(i, x, file.FileName)),
            CTEArray("ProceduralImages", file.ProceduralImages, (x, i) => ToCTE(i, x, file.FileName)),
            CTEArray("UsedClasses", file.UsedClasses, (x, i) => ToCTE(i, x)),
            CTEArray("ContentTable", file.ContentTable, (x, i) => ToCTE(i, x)).With(cte => cte.DebugText = file.DebugInfo.GetContentTableText())
        );
        root.SpreadParent();
        return root;
    }

    private static CustomTreeElement ToCTE(int i, UsedClass c)
    {
        return new CustomTreeElement($"[{i}]", c.ToString(),
            CTE("Package", $"{c.Package}"),
            CTE("Group", $"{c.Type}"),
            CTE("Parent", $"{c.Parent}"),
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
        return new CustomTreeElement($"[{i}]", $"{p.Name} ({p.W * p.H})") { InlineImage = BitmapSource.Create(p.W, p.H, 96, 96, PixelFormats.Rgb24, null, bytes, p.W * 3) };
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
        string text = $"{p.Value}";
        CustomTreeElement valueCTE = CTE("Value", text);
        if (p.Value != null && p.Value is IUnrealStruct s)
        {
            text = s.GetText();
            valueCTE.Value = s.GetText();
            valueCTE.ChildElements.Add(s.CreateCTE());
        }

        return new CustomTreeElement(
            p.Name, text,
            CTE("Name", p.Name),
            CTE("Type", UPropertyType.GetText(p.Type)),
            valueCTE
        )
        {
            Color = p.Color?.ToColor(),
            RawData = p.RawData
        };
    }

    private static CustomTreeElement ToCTE(int i, ContentDef n)
    {

        return new CustomTreeElement($"[{i}]", $"{n.Name}",
            CTE("id1?", $"{n.id}"),
            CTE("id2?", $"{n.id2}"),
            CTE("Id3?", $"{n.Group}"),
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
            CTE("Properties", [.. image.Properties.Select(ToCTE)]),
            CTE("Width", image.Width.ToString()),
            CTE("Height", image.Height.ToString()),
            CTEArray("MipMaps", image.MipMaps, (x, i) => ToCTE(i, x))
            )
        { RawData = image.ImageHeaderRaw };
    }

    private static CustomTreeElement ToCTE(int index, ProceduralImage image, string Filename)
    {
        return new CustomTreeElement($"[{index}]", image.Name,
            CTE("EditorFullName", image.Group != null ? $"{Filename}.{image.Group}.{image.Name}" : $"{Filename}.{image.Name}", null),
            CTE("Properties", [.. image.Properties.Select(ToCTE)]),
            CTE("Width", image.Width.ToString()),
            CTE("Height", image.Height.ToString())
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

    private static CustomTreeElement CTEArray<T>(
        string name,
        IEnumerable<T> array,
        Func<T, int, CustomTreeElement> transform
        )
    {
        var childElements = array.Select(transform.Invoke).ToList();
        return new CustomTreeElement(name, $"({childElements.Count})", childElements);
    }

    public static CustomTreeElement FromValue(object value, [CallerArgumentExpression(nameof(value))] string valueName = "value")
    {
        return new CustomTreeElement(valueName, value.ToString());
    }

    public static string Hex(long? value) => value == null ? "" : $"0x{value:X}";
}
