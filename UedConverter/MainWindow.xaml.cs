using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using UedConverter.Converter;
using UedConverter.UtxFile;

namespace UedConverter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            RegularColor = U2O_Destination_Textbox.Background;
            this.Title = this.Title + " - " + Version.GetVersion();
            calculateTxSpaceChBox.IsEnabled = TextureSizeDictionary.IsAvailable();
        }

        enum FileType
        {
            [StringValue("Wavefront | *.obj")]
            OBJ,
            [StringValue("UEd Brush | *.t3d")]
            T3D
        }

        enum ConversionType
        {
            ToObj,
            ToT3D,
        }

        private string? U2O_File_Path;
        private string? U2O_Destination_Path;
        private string? O2U_File_Path;
        private string? O2U_Destination_Path;

        private readonly SolidColorBrush HighlightColor = new(Color.FromArgb(255, 80, 80, 200));
        private readonly Brush? RegularColor = null;

        private static string OpenDialog(FileType type)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog()
            {
                Filter = StringValue.GetStringValue(type)
            };
            dlg.ShowDialog();
            return dlg.FileName;
        }

        private static string SaveDialog(FileType type)
        {
            var dlg = new Microsoft.Win32.SaveFileDialog()
            {
                Filter = StringValue.GetStringValue(type)
            };
            dlg.ShowDialog();
            return dlg.FileName;
        }

        private void U2O_File_Click(object? sender, RoutedEventArgs e)
        {
            var filename = OpenDialog(FileType.T3D);
            SetSourceFile(filename, ConversionType.ToObj);
        }

        private void U2O_Destination_Click(object? sender, RoutedEventArgs e)
        {
            var filename = SaveDialog(FileType.OBJ);
            SetDestination(filename, ConversionType.ToObj);
        }

        private void U2O_Convert_Click(object? sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(U2O_Destination_Path) || String.IsNullOrEmpty(U2O_File_Path))
            {
                MessageBox.Show(
                    "Select File and Destination first",
                    "Information",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                    );
                return;
            }
            if(ReadConvertSave(U2O_Destination_Path, U2O_File_Path, new U2O_Converter(calculateTxSpaceChBox.IsChecked ?? false)))
            {
                U2O_Mark.Show(TimeSpan.FromSeconds(2));
            }
        }

        private void O2U_File_Click(object? sender, RoutedEventArgs e)
        {
            var filename = OpenDialog(FileType.OBJ);
            if (!String.IsNullOrEmpty(filename))
            {
                O2U_File_Path = filename;
                this.O2U_File_Textbox.Text = filename;
            }
        }

        private void O2U_Destination_Click(object? sender, RoutedEventArgs e)
        {
            var filename = SaveDialog(FileType.T3D);
            SetDestination(filename, ConversionType.ToT3D);
        }

        private void O2U_Convert_Click(object? sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(O2U_Destination_Path) || string.IsNullOrEmpty(O2U_File_Path))
            {
                MessageBox.Show(
                    "Select File and Destination first",
                    "Information",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                    );
                return;
            }
            if(ReadConvertSave(O2U_Destination_Path, O2U_File_Path, new O2U_Converter()))
            {
                O2U_Mark.Show(TimeSpan.FromSeconds(2));
            }
        }

        private static bool ReadConvertSave(string destination, string file, IUedConverter converter)
        {
            var success = false;
            try
            {
                var contents = File.ReadAllLines(file);
                var convertedFileContents = converter.Convert(contents);
                File.WriteAllLines(destination, convertedFileContents);
                success = true;
            }
            catch(ConvertionException e)
            {
                ShowError(e.Message);
            }
            catch(Exception)
            {
                ShowError("Something is no yes");
            }

            if(converter is U2O_Converter u2o)
            {
                if (u2o.MissingTextureData.Count > 0)
                {
                    var result = MessageBox.Show($"Not all textures could be found in data file (total: {u2o.MissingTextureData.Count})." +
                        $"\nExamples:\n{string.Join("\n", u2o.MissingTextureData.Take(4))}" +
                        $"\n\nDo you wish to save all of them into log file?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (result == MessageBoxResult.Yes)
                    {
                        File.WriteAllLines("UedConverterLog.txt", u2o.MissingTextureData);
                    }
                }
            }
            return success;
        }

        private static void ShowError(string message)
        {
            MessageBox.Show(
                    message,
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                    );
        }

        private void SetDestination(string destination, ConversionType conversionType)
        {
            if (!string.IsNullOrEmpty(destination))
            {
                if (conversionType == ConversionType.ToObj)
                {
                    U2O_Destination_Path = destination;
                    U2O_Destination_Textbox.Text = destination;
                }
                if (conversionType == ConversionType.ToT3D)
                {
                    O2U_Destination_Path = destination;
                    O2U_Destination_Textbox.Text = destination;
                }
            }
        }

        private void SetSourceFile(string source, ConversionType conversionType)
        {
            if (!String.IsNullOrEmpty(source))
            {
                if (conversionType == ConversionType.ToObj)
                {
                    U2O_File_Path = source;
                    U2O_File_Textbox.Text = source;
                }
                if (conversionType == ConversionType.ToT3D)
                {
                    O2U_File_Path = source;
                    O2U_File_Textbox.Text = source;
                }
            }
        }

        private void About_Click(object? sender, RoutedEventArgs e)
        {
            new AboutWindow().ShowDialog();
        }

        private void OpenUtx_Click(object? sender, RoutedEventArgs e)
        {
            new UtxWindow().ShowDialog();
        }

        private void Extractor_Click(object? sender, RoutedEventArgs e)
        {
            new ExtractorWindow().ShowDialog();
        }

        private void File_DragEnter(object? sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] dataString = (string[])e.Data.GetData(DataFormats.FileDrop);
                try
                {
                    if (dataString.Length > 0)
                    {
                        var path = Path.GetFullPath(dataString[0]);
                        var fileType = GetFileType(path);
                        if (sender == O2U_Group && fileType != null)
                        {
                            HighlightGroup(ConversionType.ToT3D, fileType.Value);
                        }
                        if (sender == U2O_Group && fileType != null)
                        {
                            HighlightGroup(ConversionType.ToObj, fileType.Value);
                        }
                    } else
                    {
                        ClearHighlight();
                    }
                } catch(Exception) {
                    ClearHighlight();
                }
            }
        }

        private void File_DragLeave(object? sender, DragEventArgs e)
        {
            ClearHighlight();
        }

        private void File_Drop(object? sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] dataString = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (dataString.Length > 0)
                {
                    if (sender == O2U_Group)
                    {
                        FileDrop(dataString[0], ConversionType.ToT3D);
                    }
                    if (sender == U2O_Group)
                    {
                        FileDrop(dataString[0], ConversionType.ToObj);
                    }
                }
            }
            ClearHighlight();
        }

        private void FileDrop(string file, ConversionType conversionType)
        {
            var path = Path.GetFullPath(file);
            var fileType = GetFileType(path);
            if (conversionType == ConversionType.ToT3D)
            {
                if (fileType == FileType.OBJ) SetSourceFile(path, conversionType);
                if (fileType == FileType.T3D) SetDestination(path, conversionType);
            }
            if (conversionType == ConversionType.ToObj)
            {
                if (fileType == FileType.T3D) SetSourceFile(path, conversionType);
                if (fileType == FileType.OBJ) SetDestination(path, conversionType);
            }
        }

        private void HighlightGroup(ConversionType conversionType, FileType fileType)
        {
            if(conversionType == ConversionType.ToT3D)
            {
                if (fileType == FileType.OBJ) O2U_File_Textbox.Background = HighlightColor;
                if (fileType == FileType.T3D) O2U_Destination_Textbox.Background = HighlightColor;
            }

            if (conversionType == ConversionType.ToObj)
            {
                if (fileType == FileType.T3D) U2O_File_Textbox.Background = HighlightColor;
                if (fileType == FileType.OBJ) U2O_Destination_Textbox.Background = HighlightColor;
            }
        }

        private void ClearHighlight()
        {
            O2U_File_Textbox.Background = RegularColor;
            O2U_Destination_Textbox.Background = RegularColor;
            U2O_File_Textbox.Background = RegularColor;
            U2O_Destination_Textbox.Background = RegularColor;
        }

        private static FileType? GetFileType(string file)
        {
            if (file == null)  return null;
            if (file.EndsWith("t3d", true, CultureInfo.InvariantCulture))
            {
                return FileType.T3D;
            }
            if (file.EndsWith("obj", true, CultureInfo.InvariantCulture))
            {
                return FileType.OBJ;
            }
            return null;
        }
    }
}
