using System.IO;
using UedConverter.Converter;

namespace UedConverter.UtxFile;

public readonly struct ExtractorStatus(int current, int total, bool done)
{
    public int Current { get; } = current;
    public int Total { get; } = total;
    public bool Done { get; } = done;
}

internal class UtxExtractor
{
    private readonly string[] path;
    private readonly string destination;
    private readonly List<UtxFilePath> files;
    private readonly bool all;
    private readonly bool saveImages;

    private int extractionCurrentFile;
    private bool started = false;

    private readonly Dictionary<string, V2d> textureDictionary = [];

    public UtxExtractor(string[] path, string destination, bool all, bool saveImages)
    {
        files = [];
        this.path = path;
        this.destination = destination;
        this.all = all;
        this.saveImages = saveImages;
        if (path.Length <= 0) throw new ArgumentException("No files selected");
    }

    private readonly record struct UtxFilePath(string Path, long Size);
    public readonly record struct AnalyzeResult(int DirCount, int FileCount, int EstimatedSize, List<string> Directories);

    internal AnalyzeResult Analyze()
    {
        int estimatedSize = 0;
        List<string> directories = [];
        if (all)
        {
            directories = [.. path.Select(p => Path.GetDirectoryName(p)).Distinct().OfType<string>()];
            foreach (var directory in directories)
            {
                var info = new DirectoryInfo(directory);
                files.AddRange(info.EnumerateFiles("*.utx").Select(f => new UtxFilePath(f.FullName, f.Length)));
            }
        }
        return new AnalyzeResult(directories.Count, files.Count, estimatedSize, directories);
    }

    public ExtractorStatus ExtractPartial()
    {
        var done = false;
        if (started)
        {
            if (extractionCurrentFile == files.Count)
            {
                using var writer = new StreamWriter(destination);
                foreach (var entry in textureDictionary)
                {
                    writer.WriteLine($"{entry.Key} {entry.Value.X}x{entry.Value.Y}");
                }
            }
            else
            {
                ExtractFile();
            }
            extractionCurrentFile++;
            Thread.Sleep(800);
        }
        else
        {
            started = true;
            extractionCurrentFile = 0;
        }
        return new ExtractorStatus(extractionCurrentFile, files.Count, done);
    }

    private void ExtractFile()
    {
        var file = files[extractionCurrentFile];
        var structure = UtxReader.ReadFile(file.Path);//TODO add skip for images
        foreach (var image in structure.Images)
        {
            if (!string.IsNullOrEmpty(image.Name))
            {
                textureDictionary.Add(image.Name, new V2d(image.Height, image.Width));
            }
        }
    }
}
