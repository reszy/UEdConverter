using System.IO;
using UedConverter.Converter;
using UedConverter.Image;

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
                //Temporary solution for errors
                if (problematicFiles.Count > 0)
                {
                    File.WriteAllLines($"error_log_{DateTime.Now:yyyyMMdd_HHmmss}.txt", problematicFiles);
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
                if (textureDictionary.TryGetValue(image.Name, out V2d? texture))
                {
                    if (texture.X != image.Width || texture.Y != image.Height)
                        textureDictionary.Add($"{result.Structure.FileName}.{image.Name}", new V2d(image.Width, image.Height));
                }
                else
                {
                    textureDictionary.Add(image.Name, new V2d(image.Width, image.Height));
                }
            }
            if(saveImages)
            {
                SaveImage(file.Path, image);
            }
        }
    }

    private static void SaveImage(string orginalFilePath, Image image)
    {
        var directory = Path.GetDirectoryName(orginalFilePath);
        var filename = Path.GetFileNameWithoutExtension(orginalFilePath);
        if (directory == null) return;

        
        var extractionDirectory = (image.Group != null) ? Path.Combine(directory, "ExtractedImages", filename, image.Group) : Path.Combine(directory, "ExtractedImages", filename);
        if(!Directory.Exists(extractionDirectory))
        {
            Directory.CreateDirectory(extractionDirectory);
        }

        if (image.IsCorrect)
        {
            var finalFilename = Path.Combine(extractionDirectory, image.Name + ".png");
            var file = new PNGFile(finalFilename, image.Width, image.Height);
            file.SaveImage(ToPngBytes(image));
        }
    }
    public static byte[] ToPngBytes(Image image)
    {
        var pixels = image.ImageData?.Pixels;
        var paletteColors = image.Properties.GetRef<Palette>("Palette")?.Colors;
        var width = image.Width;
        var height = image.Height;
        var maskedValue = image.Properties.GetValue<byte>("bMasked");
        var masked = maskedValue != null && maskedValue == 0;
        if (pixels != null && paletteColors != null)
        {
            long length = pixels.Length * 4 + height;
            byte[] output = new byte[length];
            var outPosition = 0;
            for (int i = 0; i < pixels.Length; i++)
            {
                if (i % width == 0) output[outPosition++] = 0x00;
                output[outPosition++] = paletteColors[pixels[i]].r;
                output[outPosition++] = paletteColors[pixels[i]].g;
                output[outPosition++] = paletteColors[pixels[i]].b;
                output[outPosition++] = (masked && pixels[i] == 0x00) ? (byte)0x00 : (byte)0xFF;
            }
            return output;
        }
        return [];
    }
}
