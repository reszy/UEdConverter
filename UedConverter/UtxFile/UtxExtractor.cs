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
    private readonly bool all;
    private readonly bool saveImages;
    private readonly List<UtxFilePath> files = [];
    private readonly List<string> problematicFiles = [];

    private int extractionCurrentFile;
    private bool started = false;

    private readonly Dictionary<string, V2d> textureDictionary = [];

    public UtxExtractor(string[] path, string destination, bool all, bool saveImages)
    {
        this.path = path;
        this.destination = destination;
        this.all = all;
        this.saveImages = saveImages;
        if (path.Length <= 0) throw new ArgumentException("No files selected");
    }

    private readonly record struct UtxFilePath(string Path, long Size);
    public readonly record struct AnalyzeResult(int DirCount, int FileCount, long EstimatedSize, List<string> Directories);

    internal AnalyzeResult Analyze()
    {
        long estimatedSize = 0;
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
        else
        {
            foreach (var filePath in path)
            {
                var info = new FileInfo(filePath);
                files.AddRange(new UtxFilePath(info.FullName, info.Length));
            }
        }
        if (saveImages)
        {
            estimatedSize = files.Sum(f => f.Size);
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
                done = true;
            }
            else
            {
                ExtractFile();
            }
            extractionCurrentFile++;
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
        var result = UtxReader.ReadFile(file.Path);//TODO add skip for images
        if (result.Problems.Count > 0 || result.Exception != null) problematicFiles.Add(result.Structure.FileName);
        foreach (var image in result.Structure.Images)
        {
            if (!string.IsNullOrEmpty(image.Name) && image.IsCorrect)
            {
                if (textureDictionary.ContainsKey(image.Name))
                    textureDictionary.Add($"{result.Structure.FileName}.{image.Name}", new V2d(image.Height, image.Width));
                else
                    textureDictionary.Add(image.Name, new V2d(image.Height, image.Width));
            }
        }
    }
}
