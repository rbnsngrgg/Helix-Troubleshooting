using System.Windows;
using System.Windows.Threading;

namespace HelixTroubleshootingWPF
{
    /// <summary>
    /// Interaction logic for ProgressBar.xaml
    /// </summary>
    public partial class ProgressBar : Window
    {

        public float ProgressTick { get; private set; } = 100f;
        public bool Canceled { get; private set; } = false;
        public ProgressBar()
        {
            InitializeComponent();
        }

        public void SetProgressTick(float count)
        {
            ProgressTick = 100f / count;
        }

        public void TickProgress()
        {
            mainProgressBar.Dispatcher.Invoke(() => mainProgressBar.Value += ProgressTick, DispatcherPriority.Background);
        }

        public void SetLabel(string message)
        {
            label.Dispatcher.Invoke(() => label.Text = message, DispatcherPriority.Background);
        }

        public void SetValueComplete()
        {
            mainProgressBar.Value = 100;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Canceled = true;
        }
    }
}
