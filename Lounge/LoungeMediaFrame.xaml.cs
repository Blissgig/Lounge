using System.Windows;
using System.Windows.Input;


namespace Lounge
{
    public partial class LoungeMediaFrame : Window
    {
        private LoungeEngine loungeEngine;

        public bool PrimaryMonitor { get; set; }

        public LoungeMediaFrame()
        {
            InitializeComponent();
        }

        public LoungeMediaFrame(LoungeEngine engine)
        {
            InitializeComponent();
            loungeEngine = engine;
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            loungeEngine.KeyPress(sender, e);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            loungeEngine.UnloadWindow(this.Name);
        }
    }
}
