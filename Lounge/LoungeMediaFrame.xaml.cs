using System.Windows;
using System.Windows.Input;


namespace Lounge
{
    public partial class LoungeMediaFrame : Window
    {
        public LoungeMediaFrame()
        {
            InitializeComponent();
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            //TODO: pass keyboard keys to engine.  Will need a copy of engine to call
            //loungeEngine.KeyPress(sender, e);
        }
    }
}
