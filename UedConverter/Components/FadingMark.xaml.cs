using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace UedConverter.Components
{
    public partial class FadingMark : UserControl
    {
        public FadingMark()
        {
            InitializeComponent();
        }
        public TimeSpan DisplayDuration { get; set; } = TimeSpan.FromSeconds(3);

        public void Show(TimeSpan? duration = null)
        {
            if (duration.HasValue) DisplayDuration = duration.Value;

            Visibility = Visibility.Visible;
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(250));
            this.BeginAnimation(OpacityProperty, fadeIn);

            _ = StartLifetimeAsync();
        }

        private async Task StartLifetimeAsync()
        {
            await Task.Delay(DisplayDuration);
            // trigger fade out storyboard defined in XAML
            var sb = (Storyboard)Resources["FadeOutStoryboard"];
            sb.Begin(this, true);
        }

        private void FadeOutStoryboard_Completed(object sender, EventArgs e)
        {
            Visibility = Visibility.Collapsed;
        }
    }
}