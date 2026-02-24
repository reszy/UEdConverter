using System;
using System.Diagnostics;
using System.Windows;

namespace UedConverter
{
    /// <summary>
    /// Interaction logic for AboutWindow.xaml
    /// </summary>
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            string name = assembly.GetName().Name;

            InitializeComponent();

            titleLabel.Content = name;
            versionLabel.Content = Version.GetVersion();
            string gitUrl = "https://github.com/reszy/UEdConverter/releases";
            gitLink.NavigateUri = new Uri(gitUrl);
            linkText.Text = gitUrl;
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void OpenUrl_Click(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }
    }
}
