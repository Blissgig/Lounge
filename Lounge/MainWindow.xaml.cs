using System.Windows;
using System.Windows.Controls;


namespace Lounge
{
    public partial class MainWindow : Window
    {
        private LoungeEngine loungeEngine;

        public MainWindow()
        {
            InitializeComponent();

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            loungeEngine = new LoungeEngine(this);
            
            Dispatcher.BeginInvoke(new System.Action(() => loungeEngine.SettingsLoad()), System.Windows.Threading.DispatcherPriority.ContextIdle, null);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            loungeEngine.Dispose();
            loungeEngine = null;
        }
    }
}