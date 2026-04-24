using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using UedConverter.Converter;

namespace UedConverter.UtxFile
{
    public struct ExtractorStatus
    {
        public ExtractorStatus(int current, int total, bool done)
        {
            Current = current;
            Total = total;
            Done = done;
        }

        public int Current { get; }
        public int Total { get; }
        public bool Done { get; }
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

        private Dictionary<string, V2d> textureDictionary = new Dictionary<string, V2d>();

        public UtxExtractor(string[] path, string destination, bool all, bool saveImages)
        {
            files = new List<UtxFilePath>();
            this.path = path;
            this.destination = destination;
            this.all = all;
            this.saveImages = saveImages;
            if (path.Length <= 0) throw new ArgumentException("No files selected");
        }

        private struct UtxFilePath
        {
            public UtxFilePath(string Path, long Size)
            {
                this.Path = Path;
                this.Size = Size;
            }
            public string Path { get; }
            public long Size { get; }
        };

        public struct AnalyzeResult
        {
            public AnalyzeResult(int dirCount, int fileCount, int estimatedSize, List<string> directories)
            {
                DirCount = dirCount;
                FileCount = fileCount;
                EstimatedSize = estimatedSize;
                Directories = directories;
            }

            public int DirCount { get; }
            public int FileCount { get; }
            public int EstimatedSize { get; }//for extracting images
            public List<string> Directories { get; }
        };

        internal AnalyzeResult Analyze()
        {
            int estimatedSize = 0;
            List<string> directories = new List<string>();
            if (all)
            {
                directories = path.Select(p => Path.GetDirectoryName(p)).Distinct().ToList();
                foreach (var directory in directories)
                {
                    var info = new DirectoryInfo(directory);
                    files.AddRange(info.EnumerateFiles("*.utx").Select(f => new UtxFilePath(f.FullName, f.Length)).ToList());
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
                    using (var writer = new StreamWriter(destination))
                    {
                        foreach(var entry in textureDictionary)
                        {
                            writer.WriteLine($"{entry.Key} {entry.Value.X}x{entry.Value.Y}");
                        }
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
                textureDictionary.Add(image.Name, new V2d(image.Height, image.Width));
            }
        }
    }
}
