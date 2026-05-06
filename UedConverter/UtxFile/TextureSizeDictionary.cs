using System.IO;
using System.Text.RegularExpressions;
using System.Windows;

namespace UedConverter.UtxFile;

public struct USize(int Width, int Height)
{
    public int Width = Width;
    public int Height = Height;
};

internal static partial class TextureSizeDictionary
{
    public const string TextureSizeDictionaryFilename = "texture_dict.txt";

    /// <summary>
    /// Only check file existence
    /// </summary>
    public static bool IsAvailable()
    {
        var filePath = GetFilePath();
        Console.WriteLine($"Checking for texture size dictionary at: {filePath}");
        return File.Exists(filePath);
    }

    private static string GetFilePath()
    {
        var directory = Directory.GetCurrentDirectory();
        return Path.Combine(directory, TextureSizeDictionaryFilename);
    }

    public static Dictionary<string, USize> Load()
    {
        var filePath = GetFilePath();

        if (!IsAvailable())
        {
            MessageBox.Show("Cannot find texture data file. All Textures will be threated as 64x64", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        var entries = new Dictionary<string, USize>();
        using (var f = File.OpenText(filePath))
        {
            while (!f.EndOfStream)
            {
                var text = f.ReadLine();
                var rMatch = dimensionToNameRegex.Match(text);
                if (rMatch.Success && rMatch.Groups.Count == 4)
                {
                    var name = rMatch.Groups[1].Value;
                    if (int.TryParse(rMatch.Groups[2].Value, out var w) &&
                        int.TryParse(rMatch.Groups[3].Value, out var h) &&
                        name != null)
                    {
                        entries[name] = new USize(w, h);
                        Console.WriteLine($"Found texture \"{name}\" with dimensions w:{w} h:{h}");
                    }
                }
            }
        }
        return entries;
    }

    private static readonly Regex dimensionToNameRegex = DimensionToNameRegex();

    [GeneratedRegex(@"^(.+) (\d+)x(\d+)$", RegexOptions.Compiled)]
    private static partial Regex DimensionToNameRegex();
}
