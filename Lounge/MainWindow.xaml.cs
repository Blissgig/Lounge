using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Forms;

namespace Lounge
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

        private void TestLoad()
        {
            try
            {
                System.Drawing.Rectangle workingArea;
                bool bMonitor;

                foreach (Screen scr in Screen.AllScreens)
                {
                    workingArea = scr.WorkingArea;

                    bMonitor = true;

                    //The primary monitor is only false if the checkbox is NOT selected
                    if (scr.Primary == true)
                    {
                        bMonitor = true; // TEMP TODO: Convert.ToBoolean(this.chkPrimaryMonitor.IsChecked);
                    }


                    if (bMonitor == true)
                    {
                        LoungeMediaFrame loungeMediaFrame = new LoungeMediaFrame();
                        loungeMediaFrame.Name = "Display" + scr.DeviceName.Replace('\\', '_').Replace('.', 'A');

                        //if (bCount > 0)
                        //{
                        //    //this is to insure only the lead display is controlling media playback.  HACK!!!!
                        //    bediaDisplay.bNextMedia = false;
                        //}
                        //bCount++;

                        //if (this.chkRandomVisualizationPosition.IsChecked == true)
                        //{
                        //List<Point> VisualPositions = GetColumnRowSets();
                        //byte bCurrent = 0;
                        //Point pntCurrent;
                        //Border visualElement;


                        //for (byte b = 0; b < 60; b++)
                        //{
                        //    bCurrent = Convert.ToByte(BediaRandom.Next(0, (VisualPositions.Count - 1)));
                        //    pntCurrent = VisualPositions[bCurrent];

                        //    //Set new position
                        //    visualElement = (Border)bediaDisplay.FindName("square" + b.ToString("00"));
                        //    Grid.SetColumn(visualElement, Convert.ToInt16(pntCurrent.Y));
                        //    Grid.SetRow(visualElement, Convert.ToInt16(pntCurrent.X));

                        //    VisualPositions.RemoveAt(bCurrent);
                        //}
                        ////}

                        //bediaDisplays.Add(bediaDisplay);

                        loungeMediaFrame.Left = workingArea.Left;
                        loungeMediaFrame.Top = workingArea.Top;
                        loungeMediaFrame.Width = workingArea.Width;
                        loungeMediaFrame.Height = workingArea.Height;

                        //loungeMediaFrame.BackgroundMedia.Height = workingArea.Height;
                        //loungeMediaFrame.BackgroundMedia.Width = workingArea.Width;

                        loungeMediaFrame.Show();

                        //if (this.chkSameOnAllPlayers.IsChecked == false)
                        //{
                        //    bediaDisplay.NextMedia();
                        //}
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            TestLoad();
        }
    }

    
}
