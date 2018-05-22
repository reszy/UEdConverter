using System;
using System.IO;
using System.Windows;
using UedConverter.Converter;

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
        }

        enum FileType
        {
            [StringValue("Wavefront | *.obj")]
            OBJ,
            [StringValue("UEd Brush | *.t3d")]
            T3D
        }

        private string U2O_File_Path;
        private string U2O_Destination_Path;
        private string O2U_File_Path;
        private string O2U_Destination_Path;

        private string OpenDialog(FileType type)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog()
            {
                Filter = StringValue.GetStringValue(type)
            };
            dlg.ShowDialog();
            return dlg.FileName;
        }

        private string SaveDialog(FileType type)
        {
            var dlg = new Microsoft.Win32.SaveFileDialog()
            {
                Filter = StringValue.GetStringValue(type)
            };
            dlg.ShowDialog();
            return dlg.FileName;
        }

        private void U2O_File_Click(object sender, RoutedEventArgs e)
        {
            var filename = OpenDialog(FileType.T3D);
            if (!String.IsNullOrEmpty(filename))
            {
                U2O_File_Path = filename;
                this.U2O_File_Textbox.Text = filename;
            }
        }

        private void U2O_Destination_Click(object sender, RoutedEventArgs e)
        {
            var filename = SaveDialog(FileType.OBJ);
            if (!String.IsNullOrEmpty(filename))
            {
                U2O_Destination_Path = filename;
                this.U2O_Destination_Textbox.Text = filename;
            }
        }

        private void U2O_Convert_Click(object sender, RoutedEventArgs e)
        {
            if(String.IsNullOrEmpty(U2O_Destination_Path) || String.IsNullOrEmpty(U2O_File_Path))
            {
                MessageBox.Show(
                    "Select File and Destination first",
                    "Information",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                    );
                return;
            }

            ReadConvertSave(U2O_Destination_Path, U2O_File_Path, new U2O_Converter());
        }

        private void O2U_File_Click(object sender, RoutedEventArgs e)
        {
            var filename = OpenDialog(FileType.OBJ);
            if (!String.IsNullOrEmpty(filename))
            {
                O2U_File_Path = filename;
                this.O2U_File_Textbox.Text = filename;
            }
        }

        private void O2U_Destination_Click(object sender, RoutedEventArgs e)
        {
            var filename = SaveDialog(FileType.T3D);
            if (!String.IsNullOrEmpty(filename))
            {
                O2U_Destination_Path = filename;
                this.O2U_Destination_Textbox.Text = filename;
            }
        }

        private void O2U_Convert_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(O2U_Destination_Path) || String.IsNullOrEmpty(O2U_File_Path))
            {
                MessageBox.Show(
                    "Select File and Destination first",
                    "Information",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                    );
                return;
            }
            ReadConvertSave(O2U_Destination_Path, O2U_File_Path, new O2U_Converter());
        }

        private void ReadConvertSave(string destination, string file, IUedConverter converter)
        {
            try
            {
                var contents = File.ReadAllLines(file);
                var convertedFileContents = converter.Convert(contents);
                File.WriteAllLines(destination, convertedFileContents);
            }
            catch(ConvertionException e)
            {
                ShowError(e.Message);
            }
            catch(Exception)
            {
                ShowError("Something is no yes");
            }
        }

        private void ShowError(string message)
        {
            MessageBox.Show(
                    message,
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                    );
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            (new AboutWindow()).ShowDialog();
        }
    }
}
