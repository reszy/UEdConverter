using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using UedConverter.UtxFile;
using static UedConverter.Common;

namespace UedConverter;

public class ColorToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Color color)
        {
            return new SolidColorBrush(color);
        }
        return Brushes.Transparent;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
public class ColorToHexConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Color color)
        {
            return $"#{color.R:X2}{color.G:X2}{color.B:X2}{color.A:X2}";
        }
        return "";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public partial class UtxWindow : Window
{

    public UtxWindow()
    {
        InitializeComponent();
        LoadTreeData();
    }

    private readonly ImageCache _bitmapCache = new(15_000_000);
    private Structure? openedFile = null;
    private long _fileSize = 0;
    private long _structureSize = 0;
    private void LoadTreeData()
    {
        var filename = "";
        openedFile = UtxReader.GetExample();
        try
        {
            var dlg = new Microsoft.Win32.OpenFileDialog()
            {
                Filter = "UEd Textures | *.utx",
                Multiselect = false
            };
            dlg.ShowDialog();
            if (dlg.FileName != null)
            {
                openedFile = UtxReader.ReadFile(dlg.FileName);
                filename = Path.GetFileName(dlg.FileName);
                var info = new FileInfo(dlg.FileName);
                _fileSize = info.Length;
            }
        }
        catch (Exception e)
        {
            MessageBox.Show($"Error while reading file: {filename}\n\n{e.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Stop);
        }
        var root = CustomTreeElement.BuildTreeFromFile(openedFile);
        var elements = new List<ICustomTreeElement> { root };
        treeView.ItemsSource = elements;
        UpdateBottomBar();
    }

    private void UpdateBottomBar(bool onlyCache = false)
    {
        if (!onlyCache) {
            _structureSize = openedFile?.GetSize() ?? 0;
        }
        var cacheSize = _bitmapCache.CurrentSize;
        long usage = 0;
        using (var proc = Process.GetCurrentProcess()) {
            proc.Refresh();
            usage = proc.PrivateMemorySize64;
        }
        BottomText.Text = $"Data: {GetSizeWithUnits(_structureSize)}, File: {GetSizeWithUnits(_fileSize)}, Cache: {GetSizeWithUnits(cacheSize)}, Used: {GetSizeWithUnits(usage)}";
    }

    
    private void TreeNodeSelected(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (e.NewValue is CustomTreeElement cte)
        {
            string path = GetElementPath(cte);
            rawBox.Text = path + "\n" + cte.RawData;
            if (IsImage(path))
            {
                if (_bitmapCache.Contains(path))
                {
                    imageBox.Source = _bitmapCache.Get(path);
                }
                else
                {
                    var image = GetImage(path);
                    if (!image.HasValue)
                    {
                        imageBox.Source = null;
                    }
                    else
                    {
                        var (pixels, width, height, palette) = image.Value;
                        var bitmap = CreateBitmap(pixels, width, height, palette);
                        _bitmapCache.Add(path, bitmap);
                        UpdateBottomBar(true);
                        imageBox.Source = bitmap;
                    }
                }
            }

        }
    }

    private static bool IsImage(string path)
    {
        return Regexes.IsImageRegex.IsMatch(path);
    }
    private (byte[] pixels, int width, int height, UColor[] palette)? GetImage(string path)
    {
        if (openedFile == null) return null;
        var groups = Regexes.IsImageRegex.Match(path).Groups;
        if (groups.Count >= 2 && int.TryParse(groups[1].Value, out var imageIndex))
        {
            var image = openedFile.Images[imageIndex];
            if (!image.IsCorrect) return null;

            var palette = openedFile.Palettes[image.Palette - 1];
            if (palette.Colors == null) return null;
            if (groups.Count == 3 && int.TryParse(groups[2].Value, out var mipMapIndex))
            {
                var mipMap = image.MipMaps[mipMapIndex];
                if(!mipMap.IsCorrect || mipMap.Pixels == null) return null;
                return (mipMap.Pixels, mipMap.Width, mipMap.Height, palette.Colors);
            }
            else
            {
                if (image.ImageData?.Pixels == null) return null;
                return (image.ImageData.Pixels, image.Width, image.Height, palette.Colors);
            }
        }
        return null;
    }

    private static BitmapSource CreateBitmap(byte[] pixels, int width, int height, UColor[] palette)
    {
        var byteDepth = 3;
        var paletteDepth = 4;
        var bytes = new byte[width * height * byteDepth];
        for(int i = 0; i< pixels.Length; i++)
        {
            var color = palette[pixels[i]];
            bytes[i * byteDepth + 0] = color.r;
            bytes[i * byteDepth + 1] = color.g;
            bytes[i * byteDepth + 2] = color.b;
        }
        return BitmapSource.Create(width, height, 96, 96, PixelFormats.Rgb24, null, bytes, width * byteDepth);
    }

    private static string GetElementPath(ICustomTreeElement element)
    {
        var pathSegments = new List<string>();
        CollectPathSegments(element, pathSegments);
        pathSegments.Reverse();
        return string.Join(".", pathSegments);
    }

    private static void CollectPathSegments(ICustomTreeElement element, List<string> pathSegments)
    {
        pathSegments.Add(element.Name);
        var parent = element.Parent;
        while (parent != null)
        {
            pathSegments.Add(parent.Name);
            parent = parent.Parent;
        }
    }
}

public static partial class Regexes
{
    public static Regex IsImageRegex { get; } = GetIsImageRegex();

    [GeneratedRegex(@"^File\.Images\.\[(\d+)\](?:\.MipMaps\.\[(\d+)\])?.*$", RegexOptions.Compiled)]
    private static partial Regex GetIsImageRegex();
}

class ImageCache(uint capacity)
{
    public uint Capacity { get; private set; } = capacity;
    public uint CurrentSize { get; private set; }
    private readonly Dictionary<string, CacheEntry> _cachedImages = [];

    private class CacheEntry(uint size, long timestamp, BitmapSource bitmap)
    {
        public uint Size { get; } = size;
        public long Timestamp { get; } = timestamp;
        public BitmapSource BitmapImage { get; } = bitmap;
    }

    public BitmapSource? Get(string key)
    {
        _cachedImages.TryGetValue(key, out CacheEntry? entry);
        return entry?.BitmapImage;
    }

    public bool Contains(string key)
    {
        return _cachedImages.ContainsKey(key);
    }

    public void Add(string key, BitmapSource bitmap)
    {
        var size = (uint)(bitmap.PixelWidth * bitmap.PixelHeight) * 3;
        var timestamp = DateTime.Now.Ticks;

        while (CurrentSize + size > Capacity && CurrentSize > 0)
        {
            DeleteEntry(size);
        }

        _cachedImages.Add(key, new CacheEntry(size, timestamp, bitmap));
        CurrentSize += size;
    }

    private void DeleteEntry(uint neededCapacity)
    {
        var entriesBySize = _cachedImages.OrderBy(e => e.Value.Timestamp);
        var capacityLeft = Capacity - CurrentSize;
        foreach (var entry in entriesBySize)
        {
            capacityLeft += entry.Value.Size;
            _cachedImages.Remove(entry.Key);
            CurrentSize -= entry.Value.Size;
            if (capacityLeft > neededCapacity) break;
        }
    }
}
