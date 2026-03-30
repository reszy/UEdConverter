using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using UedConverter.UtxFile;

namespace UedConverter
{
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

    /// <summary>
    /// Interaction logic for UtxWindow.xaml
    /// </summary>
    public partial class UtxWindow : Window
    {

        public UtxWindow()
        {
            InitializeComponent();
            LoadTreeData();
        }

        static string filename = "";
        static string directory = "";

        private UtxFile.Structure openedFile = null;
        private ImageCache _bitmapCache = new ImageCache(15_000_000);
        private void LoadTreeData()
        {
            openedFile = UtxReader.GetExample();
            try
            {
                var dlg = new Microsoft.Win32.OpenFileDialog()
                {
                    InitialDirectory = directory,
                    Filter = "UEd Textures | *.utx",
                    Multiselect = false
                };
                dlg.ShowDialog();
                if(dlg.FileName != null) openedFile = UtxReader.ReadFile(dlg.FileName);
            }
            catch (Exception e)
            {
                MessageBox.Show($"Error while reading file: {filename}\n\n{e.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Stop);
            }
            var root = CustomTreeElement.BuildTreeFromFile(openedFile);
            var elements = new List<ICustomTreeElement> { root };
            treeView.ItemsSource = elements;
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
                            var i = image.Value;
                            var bitmap = CreateBitmap(i.pixels, i.width, i.height, i.palette);
                            _bitmapCache.Add(path, bitmap);
                            imageBox.Source = bitmap;
                        }
                    }
                }

            }
        }

        private bool IsImage(string path)
        {
            return Regexes.IsImageRegex.IsMatch(path);
        }
        private (byte[] pixels, int width, int height, UColor[] palette)? GetImage(string path)
        {
            var groups = Regexes.IsImageRegex.Match(path).Groups;
            if (groups.Count >= 2 && int.TryParse(groups[1].Value, out var imageIndex))
            {
                var image = openedFile.Images[imageIndex];
                if (!image.IsCorrect) return null;

                var palette = openedFile.Palettes[image.Palette - 1];
                if (groups.Count == 3 && int.TryParse(groups[2].Value, out var mipMapIndex))
                {
                    var mipMap = image.MipMaps[mipMapIndex];
                    if(!mipMap.IsCorrect) return null;
                    return (mipMap.Pixels, mipMap.Width, mipMap.Height, palette.Colors);
                }
                else
                {
                    return (image.ImageData.Pixels, image.Width, image.Height, palette.Colors);
                }
            }
            return null;
        }

        private BitmapSource CreateBitmap(byte[] pixels, int width, int height, UColor[] palette)
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

        private string GetElementPath(ICustomTreeElement element)
        {
            var pathSegments = new List<string>();
            CollectPathSegments(element, pathSegments);
            pathSegments.Reverse();
            return string.Join(".", pathSegments);
        }

        private void CollectPathSegments(ICustomTreeElement element, List<string> pathSegments)
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
        public static Regex IsImageRegex = new Regex(@"^File\.Images\.\[(\d+)\](?:\.MipMaps\.\[(\d+)\])?$", RegexOptions.Compiled);
    }

    class ImageCache
    {
        public uint Capacity { get; private set; }
        public uint CurrentSize { get; private set; }
        private Dictionary<string, CacheEntry> _cachedImages = new Dictionary<string, CacheEntry>();
        public ImageCache(uint capacity)
        {
            Capacity = capacity;
        }

        private class CacheEntry
        {
            public uint Size { get; }
            public long Timestamp { get; }
            public BitmapSource BitmapImage { get; }

            public CacheEntry(uint size, long timestamp, BitmapSource bitmap)
            {
                Size = size;
                Timestamp = timestamp;
                BitmapImage = bitmap;
            }
        }

        public BitmapSource Get(string key)
        {
            _cachedImages.TryGetValue(key, out CacheEntry entry);
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
}
