using System.IO;
using System.Windows;
using UedConverter.Converter;

namespace UedConverter
{
    public class Settings
    {
        public string? LastObjLocation { get; set; }
        public string? LastT3dLocation { get; set; }


        public void SaveUsedPath(FileType type, string? path)
        {
            var directory = Path.GetDirectoryName(path);
            switch (type)
            {
                case FileType.T3D: LastT3dLocation = directory; break;
                case FileType.OBJ: LastObjLocation = directory; break;
            }
        }

        public string? GetUsedPath(FileType type)
        {
            return (type) switch
            {
                FileType.T3D => LastT3dLocation,
                FileType.OBJ => LastObjLocation,
                _ => null
            };
        }
    }

    class PersistentSettings
    {
        private const string filename = "settings.ini";
        public static Settings Settings { get; } = new();

        public static void Load()
        {
            var loadedPossibleSettings = new Dictionary<string, string>();

            if (!File.Exists(filename)) return;

            var lines = File.ReadAllLines(filename);
            var settingsProperties = typeof(Settings).GetProperties();
            foreach (var line in lines)
            {
                var equalsIndex = line.IndexOf('=');
                if (equalsIndex == -1) continue;

                var key = line.Substring(0, equalsIndex);
                var value = line[(equalsIndex + 1)..];
                loadedPossibleSettings[key] = value;
            }
            foreach (var property in settingsProperties)
            {
                if (loadedPossibleSettings.TryGetValue(property.Name, out var value) && value.GetType() == property.PropertyType)
                {
                    property.SetValue(Settings, value);
                }
            }
        }

        public static void Save()
        {
            var settings = typeof(Settings).GetProperties()
                .Select(p => ((string Name, object? Value))(p.Name, p.GetValue(Settings)))
                .Where(x => x.Value != null)
                .Select(x => $"{x.Name}={x.Value}")
                .ToList();
            try
            {
                File.WriteAllLines(filename, settings);
            }
            catch (IOException ex)
            {
                MessageBox.Show(ex.Message, "Error while saving settings", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

    }
}
