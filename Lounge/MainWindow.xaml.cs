using System.Windows;
using System.Windows.Controls;


namespace Lounge
{

    public partial class MainWindow : Window
    {
        #region Private Members
        private LoungeEngine loungeEngine;

        #endregion

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            loungeEngine = new LoungeEngine(this);
        }

        private void FoldersFiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            loungeEngine.RowSelected();
        }

        private void play_Click(object sender, RoutedEventArgs e)
        {
            loungeEngine.MediaPlay();
        }

        private void back_Click(object sender, RoutedEventArgs e)
        {
            loungeEngine.Back();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            loungeEngine.Dispose();
            loungeEngine = null;
        }
    }
}
