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
    private readonly string materialDestination;
    private readonly bool all;
    private readonly bool createMtl;
    private readonly bool saveImages;
    private readonly List<UtxFilePath> files = [];
    private readonly List<string> problematicFiles = [];

    private int extractionCurrentFile;
    private bool started = false;
    private bool done = false;

    private readonly Dictionary<string, V2d> textureDictionary = [];
    private readonly Dictionary<string, string> savedImages = [];

    public UtxExtractor(string[] path, string destination, string materialDestination, bool all, bool saveImages, bool createMtl)
    {
        this.path = path;
        this.destination = destination;
        this.materialDestination = materialDestination;
        this.all = all;
        this.saveImages = saveImages;
        this.createMtl = createMtl;
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
        if (started)
        {
            if (extractionCurrentFile == files.Count)
            {
                SaveOutput();
                done = true;
            }
            else
            {
                ExtractFile();
                extractionCurrentFile++;
            }
        }
        else
        {
            extractionCurrentFile = 0;
            started = true;
        }
        return new ExtractorStatus(extractionCurrentFile, files.Count, done);
    }

    private void SaveOutput()
    {
        using var writer = new StreamWriter(destination);
        foreach (var entry in textureDictionary)
        {
            writer.WriteLine($"{entry.Key} {entry.Value.X}x{entry.Value.Y}");
        }
        if (createMtl && savedImages.Count > 0)
        {
            using var mtlWriter = new StreamWriter(materialDestination);
            foreach (var entry in savedImages)
            {
                WriteMtlEntry(mtlWriter, entry);
            }
        }
        //Temporary solution for errors
        if (problematicFiles.Count > 0)
        {
            File.WriteAllLines($"error_log_{DateTime.Now:yyyyMMdd_HHmmss}.txt", problematicFiles);
        }
    }

    private static void WriteMtlEntry(StreamWriter writer, KeyValuePair<string, string> entry)
    {
        writer.WriteLine();
        writer.WriteLine($"newmtl {entry.Key}");
        writer.WriteLine("   Ns 250.000000");
        writer.WriteLine("   Ks 0.500000 0.500000 0.500000");
        writer.WriteLine("   d 1.000000");
        writer.WriteLine("   illum 0");
        writer.WriteLine($"   map_Kd {entry.Value}");
        writer.WriteLine();
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
                var savedAs = AddTextureToDictionary(result.Structure.FileName, image.Name, image.Width, image.Height);
                if (saveImages)
                {
                    SaveImage(file.Path, image, savedAs);
                }
            }
        }
        foreach (var proceduralImage in result.Structure.ProceduralImages)
        {
            if (!string.IsNullOrEmpty(proceduralImage.Name))
            {
                AddTextureToDictionary(result.Structure.FileName, proceduralImage.Name, proceduralImage.Width, proceduralImage.Height);
            }
        }
    }

    private string AddTextureToDictionary(string filename, string name, int width, int height)
    {
        var finalTextureName = name;
        if (textureDictionary.TryGetValue(name, out V2d? texture))
        {
            if (texture.X != width || texture.Y != height)
            {
                finalTextureName = $"{filename}.{name}";
                textureDictionary.Add(finalTextureName, new V2d(width, height));
            }
        }
        else
        {
            textureDictionary.Add(finalTextureName, new V2d(width, height));
        }
        return finalTextureName;
    }


    private void SaveImage(string orginalFilePath, Image image, string textureName)
    {
        var directory = Path.GetDirectoryName(orginalFilePath);
        var filename = Path.GetFileNameWithoutExtension(orginalFilePath);
        if (directory == null) return;


        var extractionDirectory = (image.Group != null) ? Path.Combine(directory, "ExtractedImages", filename, image.Group) : Path.Combine(directory, "ExtractedImages", filename);
        if (!Directory.Exists(extractionDirectory))
        {
            Directory.CreateDirectory(extractionDirectory);
        }

        var finalFilename = Path.Combine(extractionDirectory, image.Name + ".png");
        var file = new PngFile(finalFilename, image.Width, image.Height);
        file.SaveImage(ToPngBytes(image));

        if (createMtl)
        {
            savedImages[textureName] = Path.GetRelativePath(directory, finalFilename);
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
