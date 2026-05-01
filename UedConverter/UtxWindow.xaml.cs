using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
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
    private Structure? _openedFile = null;
    private long _fileSize = 0;
    private void LoadTreeData()
    {
        var dlg = new Microsoft.Win32.OpenFileDialog()
        {
            Filter = "UEd Textures | *.utx",
            Multiselect = false
        };
        dlg.ShowDialog();
        if (!string.IsNullOrEmpty(dlg.FileName))
        {
            var result = UtxReader.ReadFile(dlg.FileName);
            if (result.Exception != null)
            {
                MessageBox.Show($"Error while reading file: {dlg.FileName}\n\n{result.Exception.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Stop);
            }
            _openedFile = result.Structure;
            var info = new FileInfo(dlg.FileName);
            _fileSize = info.Length;
            var root = CustomTreeElement.BuildTreeFromFile(_openedFile);
            root.DebugText = string.Join('\n', result.Problems);
            var elements = new List<CustomTreeElement> { root };
            treeView.ItemsSource = elements;
            ExpandFirstItem();
            UpdateBottomBar();
        }
    }

    private void ExpandFirstItem()
    {
        if (treeView.Items.Count == 0) return;
        if (treeView.ItemContainerGenerator.ContainerFromIndex(0) is not TreeViewItem firstItem)
        {
            treeView.Dispatcher.BeginInvoke(() => ExpandFirstItem(), System.Windows.Threading.DispatcherPriority.Background);
            return;
        }

        firstItem.IsExpanded = true;
        firstItem.IsSelected = true;
    }

    private void UpdateBottomBar(bool onlyCache = false)
    {
        var cacheSize = _bitmapCache.CurrentSize;
        long usage = 0;
        using (var proc = Process.GetCurrentProcess())
        {
            proc.Refresh();
            usage = proc.PrivateMemorySize64;
        }
        BottomText.Text = $"File: {GetSizeWithUnits(_fileSize)}, Cache: {GetSizeWithUnits(cacheSize)}, Used: {GetSizeWithUnits(usage)}";
    }


    private void TreeNodeSelected(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (e.NewValue is CustomTreeElement cte)
        {
            string path = GetElementPath(cte);
            StringBuilder desc = new();
            desc.AppendLine(path);
            if (cte.DebugText != null) desc.AppendLine(cte.DebugText);
            if (cte.RawData != null) desc.AppendLine(cte.RawData.GetText());

            rawBox.Text = desc.ToString();
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
        if (_openedFile == null) return null;
        var groups = Regexes.IsImageRegex.Match(path).Groups;
        if (groups.Count >= 2 && int.TryParse(groups[1].Value, out var imageIndex))
        {
            UtxFile.Image image = _openedFile.Images[imageIndex];
            if (!image.IsCorrect) return null;

            var palette = image.Properties.GetRef<Palette>("Palette");
            if (palette?.Colors == null) return null;
            if (groups.Count == 3 && int.TryParse(groups[2].Value, out var mipMapIndex))
            {
                var mipMap = image.MipMaps[mipMapIndex];
                if (!mipMap.IsCorrect || mipMap.Pixels == null) return null;
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
        var paletteDepth = 4;//might be useful if different palette is ever found
        var bytes = new byte[width * height * byteDepth];
        for (int i = 0; i < pixels.Length; i++)
        {
            var color = palette[pixels[i]];
            bytes[i * byteDepth + 0] = color.r;
            bytes[i * byteDepth + 1] = color.g;
            bytes[i * byteDepth + 2] = color.b;
        }
        return BitmapSource.Create(width, height, 96, 96, PixelFormats.Rgb24, null, bytes, width * byteDepth);
    }

    private static string GetElementPath(CustomTreeElement element)
    {
        var pathSegments = new List<string>();
        CollectPathSegments(element, pathSegments);
        pathSegments.Reverse();
        return string.Join(".", pathSegments);
    }

    private static void CollectPathSegments(CustomTreeElement element, List<string> pathSegments)
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
