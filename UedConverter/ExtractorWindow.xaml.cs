using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows;
using UedConverter.UtxFile;
using static UedConverter.Common;

namespace UedConverter
{
    /// <summary>
    /// Interaction logic for ExtractorWindow.xaml
    /// </summary>
    public partial class ExtractorWindow : Window
    {
        private string[]? fileNames = null;
        private string? destination = null;
        private BackgroundWorker worker = new();
        private string materialPath = "";
        private string directoryPath = "";

        public ExtractorWindow()
        {
            InitializeComponent();
            ValidateExtractinoButton();
        }

        private void File_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog()
            {
                Filter = "UEd Textures | *.utx",
                Multiselect = true
            };
            dlg.ShowDialog();
            if (dlg.FileName != null)
            {
                fileNames = dlg.FileNames;
                if (fileNames != null && fileNames.Length == 1)
                {
                    ExtractedFileText.Text = fileNames[0];
                }
            }
            ValidateExtractinoButton();
        }

        private void Destination_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.SaveFileDialog()
            {
                Filter = "TextureMetadata | *.txt",
                AddExtension = true,
                DefaultExt = "txt",
                FileName = "texture_dict.txt"
            };
            var result = dlg.ShowDialog();
            if (result.HasValue && result.Value)
            {
                destination = dlg.FileName;
                DestinationText.Text = destination;
                directoryPath = Path.GetDirectoryName(destination) ?? "";
                materialPath = Path.Combine(directoryPath, "materials.mtl");
                DirectoryText.Text = directoryPath;
                MaterialText.Text = materialPath;
            }
            ValidateExtractinoButton();
        }

        private void Extract_Click(object sender, RoutedEventArgs e)
        {
            if (fileNames == null || destination == null) return;
            try
            {
                var extractor = new UtxExtractor(fileNames, destination, materialPath,
                                                 AllFilesCheckBox.IsChecked == true, 
                                                 ImagesCheckBox.IsChecked == true,
                                                 CreateMaterialCheckBox.IsChecked == true);

                var analysis = extractor.Analyze();
                if (!CreateWarning(analysis)) return;

                ExtractButton.IsEnabled = false;
                worker = new();
                worker.ProgressChanged += ExtractorUpdate;
                worker.RunWorkerCompleted += ExtractorFinished;
                worker.WorkerReportsProgress = true;
                worker.DoWork += (s, we) =>
                {
                    if (s is BackgroundWorker sentWorker)
                    {
                        ExtractorStatus status = new();
                        do
                        {
                            status = extractor.ExtractPartial();
                            sentWorker.ReportProgress(0, status);
                        }
                        while (!status.Done);
                    }
                };
                worker.RunWorkerAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static bool CreateWarning(UtxExtractor.AnalyzeResult analysis)
        {
            if (analysis.DirCount > 0 || analysis.EstimatedSize > 0)
            {
                var sb = new StringBuilder();
                var fileText = analysis.FileCount != 1 ? "files" : "file";
                sb.Append($"Selected {analysis.FileCount} {fileText}");
                if (analysis.DirCount > 1) sb.Append($" from {analysis.DirCount} selected directories:");
                if (analysis.DirCount == 1) sb.Append($" from {analysis.DirCount} selected directory:");
                if (analysis.DirCount != 0)
                {
                    sb.AppendLine();
                    foreach (var directory in analysis.Directories)
                    {
                        sb.AppendLine(directory);
                    }
                }
                sb.AppendLine();
                sb.AppendLine($"Do you want to extract data from those {fileText}?");
                sb.Append("Metadata file will be created");
                if (analysis.EstimatedSize > 0) sb.Append($" and multiple image files for estimated size of {GetSizeWithUnits(analysis.EstimatedSize)}");
                sb.Append('.');
                var result = MessageBox.Show(sb.ToString(), "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result != MessageBoxResult.Yes) return false;
            }
            return true;
        }

        private void ValidateExtractinoButton()
        {
            ExtractButton.IsEnabled = (fileNames != null && fileNames.Length > 0 && destination != null && !worker.IsBusy);
        }

        private void ExtractorFinished(object? sender, RunWorkerCompletedEventArgs e)
        {
            if(e.Error == null)
            {
                ExtractorMark.Show(TimeSpan.FromSeconds(2));
            }
            ValidateExtractinoButton();
        }

        private void ExtractorUpdate(object? sender, ProgressChangedEventArgs e)
        {
            if (e.UserState is ExtractorStatus status)
            {
                ProgressText.Text = $"{status.Current}/{status.Total}";
                if (status.Current >= status.Total) ExtractorMark.Show(TimeSpan.FromSeconds(2));
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            worker.Dispose();
        }

        private void MaterialOrImages_Checked(object sender, RoutedEventArgs e)
        {
            var show = ImagesCheckBox.IsChecked == true && CreateMaterialCheckBox.IsChecked == true;
            var visibility = show ? Visibility.Visible : Visibility.Collapsed;
            MaterialText.Visibility = visibility;
            MaterialLabel.Visibility = visibility;
        }
    }
}
