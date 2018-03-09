using System.Windows;
using System.Windows.Controls;


namespace Lounge
{
    public partial class MainWindow : Window
    {
        #region Private Members
        private LoungeEngine loungeEngine;

        #endregion

        #region Methods
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            loungeEngine = new LoungeEngine(this);
            
            Dispatcher.BeginInvoke(new System.Action(() => loungeEngine.SettingsLoad()), System.Windows.Threading.DispatcherPriority.ContextIdle, null);
        }

        private void audioDevices_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            loungeEngine.SettingSave("AudioDevice", audioDevices.SelectedValue.ToString());
        }

        private void play_Click(object sender, RoutedEventArgs e)
        {
            loungeEngine.MediaPlay();
        }

        private void home_Click(object sender, RoutedEventArgs e)
        {
            loungeEngine.Home();
        }

        private void savePlaylist_Click(object sender, RoutedEventArgs e)
        {
            loungeEngine.SavePlaylist();
        }

        private void back_Click(object sender, RoutedEventArgs e)
        {
            loungeEngine.Back();
        }
        
        private void Media_Ended(object sender, RoutedEventArgs e)
        {
            loungeEngine.AudioNext();
        }
        
        private void appInfo_Click(object sender, RoutedEventArgs e)
        {
            loungeEngine.AppInfo();
        }

        private void selectAll_Click(object sender, RoutedEventArgs e)
        {
            loungeEngine.SelectAll();
        }

        private void clearAll_Click(object sender, RoutedEventArgs e)
        {
            loungeEngine.ClearAll();
        }
        
        private void AudioNext_Click(object sender, RoutedEventArgs e)
        {
            loungeEngine.AudioNext();
        }
        
        private void AudioPrior_Click(object sender, RoutedEventArgs e)
        {
            loungeEngine.AudioPrior();
        }

        private void AudioVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (loungeEngine != null)
            {
                loungeEngine.AudioVolume();
            }
        }

        private void ColorChoices_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string value = ColorChoices.SelectedValue.ToString();

            loungeEngine.ColorUpdated(value);
        }

        private void ColorsReCalc(object sender, System.Windows.Input.KeyEventArgs e)
        {
            loungeEngine.ColorsRecalc();
        }

        private void Window_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            loungeEngine.KeyPress(sender, e);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            loungeEngine.Dispose();
            loungeEngine = null;
        }
        #endregion
    }
}