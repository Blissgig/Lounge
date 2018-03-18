using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Un4seen.Bass;
using Un4seen.BassWasapi;
using System.Windows.Threading;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows;
using System.IO.Ports;
using System.Windows.Media.Imaging;


namespace Lounge
{
    public class LoungeEngine
    {
        #region Private Members
        public MainWindow mainWindow;
        private List<FileInfo> AudioFiles = new List<FileInfo>();
        private List<FileInfo> VideoFiles = new List<FileInfo>();
        private List<FileInfo> PhotoFiles = new List<FileInfo>();

        private int currentAudio = 0; //See "AudioNext"
        private int minimumSceneTime = 10;  //Seconds
        private int maximumSceneTime = 20;
        private int minimumPhoto = 1500;
        private int maximumPhoto = 3000;

        private Color currentColor = Color.FromRgb(144, 0, 0);
        private Color secondaryColor = Colors.Black;
        private string applicationName = "Lounge";
        private string acceptableMediaVideoTypes = "*.avi,*.asf,*.mp4,*.m4v,*.mpg,*.mpeg,*.mpeg2,*.mpeg4,*.wmv,*.3gp,*.mov,*.mts,*.divx,";
        private string acceptableMediaPhotoTypes = "*.png,*.jpg,*.jpeg,"; 
        private string acceptableMediaAudioTypes = "*.mp3,*.wma,*.wav,*.m4a,";
        private string acceptableMediaPlaylistTypes = "*.m3u,"; //  '".m3u,.wpl,";
        private List<LoungeMediaFrame> mediaFrames = new List<LoungeMediaFrame>();
        private List<DirectoryInfo> breadcrumbs = new List<DirectoryInfo>();
        private Random loungeRandom = new Random(DateTime.Now.Millisecond);
        private DispatcherTimer dispatchTimer;
        private SerialPort serialPort; //USB port used for LEDs
        private Analyzer loungeAnalyzer;
        private double currentVolume = 0.5;
        private const byte BASS_LEVEL = 150;
        private DateTime lastBoom = DateTime.Now;
        private string currentVisualization = "";
        private byte currentLEDBrightness = 88;
        private bool isAnimating = false; //Used to note when a storyboard animation is running, to avoid multiple at the same time.  Issue with app performance
        private string COMPort = "COM3";
        private int COMSpeed = 115200;
        #endregion

        #region Methods
        public LoungeEngine(MainWindow window)
        {
            try
            {
                mainWindow = window;

                ListFiles(null);
            }
            catch (Exception ex)
            {
                logException(ex);
            }
        }

        public void SettingsLoad()
        {
            try
            {
                applicationName = AppName();  //This is because of the need for this value in some functions that are threaded

                string sBassNetEmail = ""; //TODO
                string sBassNetRegistration = "";
                BassNet.Registration(sBassNetEmail, sBassNetRegistration); //for audio visualization //TODO

                loungeAnalyzer = new Analyzer(this);

                if (IsWin10() == true)
                {
                    //Win10 now supports Flac and MKV media types
                    if (acceptableMediaAudioTypes.Contains("*.flac,") == false)
                    {
                        acceptableMediaAudioTypes += "*.flac,";
                    }

                    if (acceptableMediaVideoTypes.Contains("*.mkv,") == false)
                    {
                        acceptableMediaVideoTypes += "*.mkv,";
                    }
                }

                mainWindow.ColorChoices.Items.Clear();
                mainWindow.ColorChoices.Items.Add("All");
                mainWindow.ColorChoices.Items.Add("Black and Gray");
                mainWindow.ColorChoices.Items.Add("Reds - All");
                mainWindow.ColorChoices.Items.Add("Reds - Bright");
                mainWindow.ColorChoices.Items.Add("Blues");
                mainWindow.ColorChoices.Items.Add("Greens");
                mainWindow.ColorChoices.Items.Add("Pinks");
                mainWindow.ColorChoices.Items.Add("Purples");


                //Audio Visualization Options
                mainWindow.visualizations.Items.Clear();

                System.Windows.Controls.CheckBox vis = new System.Windows.Controls.CheckBox();
                vis.Content = "Bars";
                vis.IsChecked = true;
                mainWindow.visualizations.Items.Add(vis);

                vis = new System.Windows.Controls.CheckBox();
                vis.Content = "Bounce";
                vis.IsChecked = true;
                mainWindow.visualizations.Items.Add(vis);

                vis = new System.Windows.Controls.CheckBox();
                vis.Content = "Champagne";
                vis.IsChecked = true;
                mainWindow.visualizations.Items.Add(vis);

                vis = new System.Windows.Controls.CheckBox();
                vis.Content = "Float";
                vis.IsChecked = true;
                mainWindow.visualizations.Items.Add(vis);

                //Set the users default audio device
                string sValue = SettingGet("AudioDevice");

                if (sValue.Trim().Length > 0)
                {
                    for (int i = 0; i < mainWindow.audioDevices.Items.Count; i++)
                    {
                        var s = (string)mainWindow.audioDevices.Items[i];

                        if (s == sValue)
                        {
                            mainWindow.audioDevices.SelectedIndex = i;
                            break;
                        }
                    }
                }

                bool bValue = SettingGetBool("PrimaryMonitor");
                mainWindow.primaryMonitor.IsChecked = bValue;

                bValue = SettingGetBool("ArdunioLEDs");
                mainWindow.LEDs.IsChecked = bValue;
                if (bValue)
                {
                    //The serial port needs to be opened a chunk
                    //of time before sending data to the Arduino
                    try
                    {
                        //In case the port is not available
                        serialPort = new SerialPort(COMPort, COMSpeed);
                        serialPort.Open();
                    }
                    catch 
                    {
                    }
                }

                bValue = SettingGetBool("LoopAudio");
                mainWindow.loopAudio.IsChecked = bValue;



                //For saving the current state to Settings
                mainWindow.primaryMonitor.Checked += PrimaryMonitor_Checked;
                mainWindow.primaryMonitor.Unchecked += PrimaryMonitor_Checked;

                mainWindow.LEDs.Checked += LEDs_Checked;
                mainWindow.LEDs.Unchecked += LEDs_Checked;

                mainWindow.loopAudio.Checked += LoopAudio_Checked;
                mainWindow.loopAudio.Unchecked += LoopAudio_Checked;

                //Move functionality from mainWindow to here
                mainWindow.selectHome.Click += SelectHome_Click;
                mainWindow.savePlaylist.Click += SavePlaylist_Click;
                mainWindow.back.Click += Back_Click;
                mainWindow.selectAll.Click += SelectAll_Click;
                mainWindow.clearAll.Click += ClearAll_Click;
                mainWindow.playMedia.Click += PlayMedia_Click;
                mainWindow.audioPrior.Click += AudioPrior_Click;
                mainWindow.audioNext.Click += AudioNext_Click;
                mainWindow.AudioCount.Checked += AudioCount_Checked;
                mainWindow.appInfo.Click += AppInfo_Click;
                mainWindow.AudioVolume.ValueChanged += AudioVolume_ValueChanged;
                mainWindow.audioDevices.SelectionChanged += AudioDevices_SelectionChanged;
                mainWindow.ColorChoices.SelectionChanged += ColorChoices_SelectionChanged;
                mainWindow.RedLow.KeyUp += ColorsRecalc;
                mainWindow.RedHigh.KeyUp += ColorsRecalc;
                mainWindow.GreenLow.KeyUp += ColorsRecalc;
                mainWindow.GreenHigh.KeyUp += ColorsRecalc;
                mainWindow.BlueLow.KeyUp += ColorsRecalc;
                mainWindow.BlueHigh.KeyUp += ColorsRecalc;
                mainWindow.AudioElement.MediaEnded += AudioElement_MediaNext;
                mainWindow.AudioElement.MediaFailed += AudioElement_MediaNext;
                mainWindow.KeyUp += KeyPress;
            }
            catch (Exception ex)
            {
                logException(ex);
            }
        }

        private void AudioCount_Checked(object sender, RoutedEventArgs e)
        {
            var checkbox = (System.Windows.Controls.CheckBox)sender;

            if ((bool)checkbox.IsChecked)
            {
                mainWindow.AudioElement.Volume = mainWindow.AudioVolume.Value;
            }
            else
            {
                mainWindow.AudioElement.Volume = 0;
            }
        }

        private void AudioElement_MediaNext(object sender, RoutedEventArgs e)
        {
            AudioNext();
        }

        private void ColorsRecalc(object sender, System.Windows.Input.KeyEventArgs e)
        {
            ColorsRecalc();
        }

        private void ColorChoices_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string value = mainWindow.ColorChoices.SelectedValue.ToString();

            ColorUpdated(value);
        }

        private void AudioDevices_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SettingSave("AudioDevice", mainWindow.audioDevices.SelectedValue.ToString());
        }

        private void AudioVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            AudioVolume();
        }

        private void AppInfo_Click(object sender, RoutedEventArgs e)
        {
            AppInfo();
        }

        private void AudioNext_Click(object sender, RoutedEventArgs e)
        {
            AudioNext();
        }

        private void AudioPrior_Click(object sender, RoutedEventArgs e)
        {
            AudioPrior();
        }

        private void PlayMedia_Click(object sender, RoutedEventArgs e)
        {
            MediaPlay();
        }

        private void ClearAll_Click(object sender, RoutedEventArgs e)
        {
            ClearAll();
        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            SelectAll();
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            Back();
        }

        private void SelectHome_Click(object sender, RoutedEventArgs e)
        {
            Home();
        }

        private void SavePlaylist_Click(object sender, RoutedEventArgs e)
        {
            SavePlaylist();
        }

        private string SettingGet(string setting)
        {
            string sReturn = "";

            try
            {
                sReturn = (string)Properties.Settings.Default[setting];
            }
            catch (Exception ex)
            {
                logException(ex);
            }

            return sReturn;
        }

        private bool SettingGetBool(string setting)
        {
            bool bReturn = true;

            try
            {
                bReturn = (bool)Properties.Settings.Default[setting];
            }
            catch (Exception ex)
            {
                logException(ex);
            }

            return bReturn;
        }

        private void SettingSave(string setting, string value)
        {
            try
            {
                Properties.Settings.Default[setting] = value;

                Properties.Settings.Default.Save();
            }
            catch (Exception ex)
            {
                logException(ex);
            }
        }

        private void SettingSave(string setting, bool value)
        {
            Properties.Settings.Default[setting] = value;

            Properties.Settings.Default.Save();
        }

        private void LEDs_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Windows.Controls.CheckBox checkbox = (System.Windows.Controls.CheckBox)sender;

                SettingSave("ArdunioLEDs", (bool)checkbox.IsChecked);

                if ((bool)checkbox.IsChecked == false)
                {
                    if (serialPort != null)
                    {
                        serialPort.Close();
                        serialPort.Dispose();
                        serialPort = null;
                    }
                }
            }
            catch (Exception ex)
            {
                logException(ex);
            }
        }

        private void PrimaryMonitor_Checked(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.CheckBox checkbox = (System.Windows.Controls.CheckBox)sender;

            if(checkbox.IsChecked == false)
            {
                //Remove the Primary Window's frame
                foreach(LoungeMediaFrame mediaFrame in mediaFrames)
                {
                    if (mediaFrame.PrimaryMonitor)
                    {
                        mediaFrame.Close();
                        mediaFrames.Remove(mediaFrame);
                        break;
                    }
                }
            }

            SettingSave("PrimaryMonitor", (bool)checkbox.IsChecked);
        }

        private void LoopAudio_Checked(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.CheckBox checkbox = (System.Windows.Controls.CheckBox)sender;

            SettingSave("LoopAudio", (bool)checkbox.IsChecked);
        }

        private void MediaPlay()
        {
            try
            {
                if ((AudioFiles.Count() == 0) && (VideoFiles.Count() == 0) && (PhotoFiles.Count() == 0))
                {
                    System.Windows.Forms.MessageBox.Show("Please add some media", "No media selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    if ((bool)mainWindow.LEDs.IsChecked == true)
                    {
                        if (IsArduinoAvailable() == false)
                        {
                            var result = System.Windows.Forms.MessageBox.Show(
                                "The Arduino is not connected" +
                                Environment.NewLine +
                                "Would you like to continue without LED support?"
                                , "Arduino Unavailable", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                            if (result == DialogResult.No)
                            {
                                return;
                            }
                        }
                        
                        serialPort = new SerialPort(COMPort, COMSpeed);
                        serialPort.Open();
                    }

                        
                    
                    mainWindow.WindowState = System.Windows.WindowState.Minimized;
                    
                    LoadWindows();

                    AudioNext(); //Has to happen after LoadWindows()

                    ColorUpdate();

                    foreach (LoungeMediaFrame mediaFrame in mediaFrames)
                    {
                        LoadScene(mediaFrame);
                    }
                    
                    if (AudioFiles.Count > 0)
                    {
                        loungeAnalyzer.Enable = true;
                        loungeAnalyzer.DisplayEnable = true;
                    }

                    CreateTimer();
                }
            }
            catch (Exception ex)
            {
                logException(ex);
            }
        }

        public void KeyPress(object sender, System.Windows.Input.KeyEventArgs e)
        {
            try
            {
                switch (e.Key)
                {
                    case System.Windows.Input.Key.VolumeMute:
                        if (mainWindow.AudioElement.Volume == 0)
                        {
                            mainWindow.AudioElement.Volume = currentVolume;
                        }
                        else
                        {
                            currentVolume = mainWindow.AudioElement.Volume;
                            mainWindow.AudioElement.Volume = 0;
                        }
                        break;

                    case System.Windows.Input.Key.Escape:
                        Dispose();
                        break;

                    case System.Windows.Input.Key.A: //Select All
                        SelectAll();
                        break;

                    case System.Windows.Input.Key.B:
                    case System.Windows.Input.Key.Back:
                    case System.Windows.Input.Key.BrowserBack:
                        Back();
                        break;

                    case System.Windows.Input.Key.H: //Home
                        ListFiles(null);
                        break;

                    case System.Windows.Input.Key.P: //Play
                        MediaPlay();
                        break;

                    case System.Windows.Input.Key.S: 
                        SavePlaylist();
                        break;
                }
            }
            catch (Exception ex)
            {
                logException(ex);
            }
        }

        private void LoadPlaylist(FileInfo file)
        {
            try
            {
                Cursor.Current = Cursors.WaitCursor;

                FileInfo MediaFile;

                using (StreamReader PlaylistReader = new StreamReader(file.FullName))
                {
                    string sLine;

                    while ((sLine = PlaylistReader.ReadLine()) != null)
                    {
                        if (sLine.Trim().Length > 0 && sLine.Substring(0, 1) != "#")
                        {
                            if (File.Exists(sLine) == true)
                            {
                                MediaFile = new FileInfo(sLine);
                                AddRemoveMedia(MediaFile);
                            }
                            else if (File.Exists(file.DirectoryName + "\\" + sLine) == true)
                            {
                                MediaFile = new FileInfo(file.DirectoryName + "\\" + sLine);
                                AddRemoveMedia(MediaFile);
                            }
                            else
                            {
                                //Loop through file path in combo with playlist drive
                                string sDirPath = file.DirectoryName;
                                string sFilePath = sLine;

                                while (sFilePath.IndexOf("\\") > -1)
                                {
                                    sFilePath = sFilePath.Substring(sFilePath.IndexOf("\\") + 1);

                                    if (File.Exists(sDirPath + "\\" + sFilePath) == true)
                                    {
                                        MediaFile = new FileInfo(sDirPath + "\\" + sFilePath);
                                        AddRemoveMedia(MediaFile);
                                        sFilePath = "";
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logException(ex);
            }
            finally
            {
                Cursor.Current = Cursors.Default;
            }
        }

        private void SelectAll(bool isSelected = true)
        {
            try
            {
                Cursor.Current = Cursors.WaitCursor;
                
                foreach (MediaItem mediaItem in mainWindow.mediaItems.Children)
                {
                    if (mediaItem.File != null)
                    {
                        mediaItem.Selected = isSelected;

                        //Only if true as the ClearAll removes everything and this would just add the items back.
                        if (isSelected)
                        {
                            AddRemoveMedia(mediaItem.File);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logException(ex);
            }
            finally
            {
                Cursor.Current = Cursors.Default;
            }
        }

        private void ClearAll()
        {
            try
            {
                AudioFiles.Clear();
                VideoFiles.Clear();
                PhotoFiles.Clear();

                SelectAll(false);

                mainWindow.AudioCount.Content = "0 audio files";
                mainWindow.PhotoCount.Content = "0 photo files";
                mainWindow.VideoCount.Content = "0 video files";
            }
            catch (Exception ex)
            {
                logException(ex);
            }
        }

        private void Home()
        {
            breadcrumbs.Clear();
            ListFiles(null);
        }

        private void Back()
        {
            if (breadcrumbs.Count > 0)
            {
                breadcrumbs.RemoveAt(breadcrumbs.Count - 1);

                DirectoryInfo directory = null;
                if (breadcrumbs.Count > 0)
                {
                    directory = breadcrumbs[breadcrumbs.Count - 1];
                }

                ListFiles(directory);
            }
        }

        private void AddRemoveMedia(List<FileInfo> files, FileInfo file)
        {
            try
            {
                FileInfo fileFound = files.Find(x => x.FullName == file.FullName);

                if (fileFound != null)
                {
                    files.Remove(fileFound);
                }
                else
                {
                    files.Add(file);
                }
                
                mainWindow.AudioCount.Content = AudioFiles.Count.ToString() + " audio files";
                mainWindow.PhotoCount.Content = PhotoFiles.Count.ToString() + " photo files";
                mainWindow.VideoCount.Content = VideoFiles.Count.ToString() + " video files";
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void AddRemoveMedia(FileInfo file)
        {
            try
            {
                if (acceptableMediaAudioTypes.IndexOf(file.Extension) > -1)
                {
                    AddRemoveMedia(AudioFiles, file);
                }
                else if (acceptableMediaPhotoTypes.IndexOf(file.Extension) > -1)
                {
                    AddRemoveMedia(PhotoFiles, file);
                }
                else if (acceptableMediaVideoTypes.IndexOf(file.Extension) > -1)
                {
                    AddRemoveMedia(VideoFiles, file);
                }
                else if (acceptableMediaPlaylistTypes.IndexOf(file.Extension) > -1)
                {
                    LoadPlaylist(file);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void AudioNext()
        {
            try
            {
                //To insure that this function is only called while in a loop or available media
                if ((currentAudio < AudioFiles.Count()) && ((bool)mainWindow.AudioCount.IsChecked))
                {
                    FileInfo file = AudioFiles[currentAudio];

                    mainWindow.AudioElement.Source = new Uri(file.FullName);
                    mainWindow.AudioElement.Volume = mainWindow.AudioVolume.Value;
                    mainWindow.AudioElement.Play();


                    currentAudio++;

                    if ((currentAudio >= AudioFiles.Count()) && (mainWindow.loopAudio.IsChecked) == true)
                    {
                        currentAudio = 0;
                    }

                    VisualizationSelect();  //Must come after the frames are loaded
                }
            }
            catch (Exception ex)
            {
                logException(ex);
            }
        }

        private void AudioPrior()
        {
            currentAudio--;
            if (currentAudio < 0)
            {
                currentAudio = 0;
            }

            AudioNext();
        }

        private void AudioVolume()
        {
            mainWindow.AudioElement.Volume = mainWindow.AudioVolume.Value;
        }
        
		private void SavePlaylist()
		{
			try
			{
                if ((AudioFiles.Count == 0) && (PhotoFiles.Count == 0) && (VideoFiles.Count == 0))
                {
                    System.Windows.Forms.MessageBox.Show(
                        "There are no media selected to add to a playlist" +
                        Environment.NewLine +
                        "Please add some media and then press Save again",
                        "No media to save", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    SaveFileDialog saveFileDialog = new SaveFileDialog();

                    saveFileDialog.Title = "Save Playlist";
                    saveFileDialog.Filter = "Playlists (*.m3u)|*.m3u|All files (*.*)|*.*";
                    saveFileDialog.FilterIndex = 1;
                    saveFileDialog.RestoreDirectory = true;

                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        Cursor.Current = Cursors.WaitCursor;

                        using (System.IO.StreamWriter file = new System.IO.StreamWriter(saveFileDialog.FileName))
                        {
                            file.WriteLine("#EXTM3U");

                            foreach (FileInfo mediaFile in AudioFiles)
                            {
                                file.WriteLine(mediaFile.FullName);
                            }

                            foreach (FileInfo mediaFile in PhotoFiles)
                            {
                                file.WriteLine(mediaFile.FullName);
                            }

                            foreach (FileInfo mediaFile in VideoFiles)
                            {
                                file.WriteLine(mediaFile.FullName);
                            }
                        }
                    }
                }
			}
			catch (Exception ex)
			{
				logException(ex);
			}
            finally
            {
                Cursor.Current = Cursors.Default;
            }
		}
		
        private void CreateBreadcrumb(string title)
        {
            try
            {
                System.Windows.Controls.Label label = new System.Windows.Controls.Label();
                label.Content = "● " + title;
                label.Margin = new Thickness(8, 0, 8, 0);
                label.FontSize = 18;
                label.Cursor = System.Windows.Input.Cursors.Hand;
                label.Tag = mainWindow.Breakcrumbs.Children.Count.ToString(); //To identify for the position when the user clicks this. Hack.
                label.MouseDown += Label_MouseDown;
                mainWindow.Breakcrumbs.Children.Add(label);
            }
            catch 
            {  }
        }

        private void Label_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            try
            {
                //Remove Breadcrumbs and set the new position
                System.Windows.Controls.Label label = (System.Windows.Controls.Label)sender;

                int iPosition = Convert.ToInt16(label.Tag);  //Holding the value here is a bit of a hack, I know.
                
                for(int i = (mainWindow.Breakcrumbs.Children.Count - 1); i > iPosition; i--)
                {
                    mainWindow.Breakcrumbs.Children.RemoveAt(i);
                    breadcrumbs.RemoveAt(i - 1);
                }

                //Remove the selected breadcrump as it is going to be readded by ListFiles
                if (breadcrumbs.Count > 0)
                {
                    DirectoryInfo folder = breadcrumbs[(breadcrumbs.Count - 1)];

                    mainWindow.Breakcrumbs.Children.RemoveAt(mainWindow.Breakcrumbs.Children.Count - 1);
                    breadcrumbs.RemoveAt(breadcrumbs.Count - 1);

                    ListFiles(folder);
                }
                else
                {
                    //The user has selected "Home", there are no values left in breadcrumbs
                    ListFiles(null);
                }
            }
            catch (Exception ex)
            {
                logException(ex);
            }
        }

        private void LEDUpdate()
        {
            try
            {
                if ((bool)mainWindow.LEDs.IsChecked)
                {
                    //- UPDATE LEDs -
                    //Pattern: Brightness (0 - 255); Red; Green; Blue|
                    //Example: 88; 0; 88; 255 |  //Pipe to end feed

                    string sLEDData =
                        currentLEDBrightness.ToString() + ";" +
                        currentColor.R.ToString() + ";" +
                        currentColor.G.ToString() + ";" +
                        currentColor.B.ToString() + "|";

                    serialPort.Write(sLEDData);
                }
            }
            catch (Exception ex)
            {
                logException(ex);
            }
        }

        public void ListFiles(DirectoryInfo Folder)
        {
            try
            {
                Cursor.Current = Cursors.WaitCursor;
                
                mainWindow.mediaItems.Children.Clear();
                
                MediaItem mediaItem;

                //In case no starting directory, assume a list of drives
                if (Folder == null)
                {
                    breadcrumbs.Clear();
                    mainWindow.Breakcrumbs.Children.Clear();
                    CreateBreadcrumb("Home");
                    
                    DriveInfo drv;
                    string sTemp;

                    string[] HardDrives = Directory.GetLogicalDrives();

                    foreach (string HardDrive in HardDrives)
                    {
                        drv = new DriveInfo(HardDrive);

                        if (drv.IsReady == true)
                        {
                            if (drv.VolumeLabel.Trim().Length > 0)
                            {
                                sTemp = drv.VolumeLabel;
                            }
                            else
                            {
                                sTemp = drv.Name;
                            }
                            
                            mediaItem = new MediaItem(this, new DirectoryInfo(drv.Name), sTemp);
                            mainWindow.mediaItems.Children.Add(mediaItem);
                        }
                    }

                    //Show selected files
                    foreach(FileInfo audioFile in AudioFiles)
                    {
                        mediaItem = new MediaItem(this, audioFile, System.IO.Path.GetFileNameWithoutExtension(audioFile.Name));
                        mediaItem.Icon.Source = new BitmapImage(new Uri(IconType(audioFile), UriKind.Relative));
                        mediaItem.Selected = true;

                        mainWindow.mediaItems.Children.Add(mediaItem);
                    }

                    foreach (FileInfo photoFile in PhotoFiles)
                    {
                        mediaItem = new MediaItem(this, photoFile, System.IO.Path.GetFileNameWithoutExtension(photoFile.Name));
                        mediaItem.Icon.Source = new BitmapImage(new Uri(IconType(photoFile), UriKind.Relative));
                        mediaItem.Selected = true;

                        mainWindow.mediaItems.Children.Add(mediaItem);
                    }

                    foreach (FileInfo videoFile in VideoFiles)
                    {
                        mediaItem = new MediaItem(this, videoFile, System.IO.Path.GetFileNameWithoutExtension(videoFile.Name));
                        mediaItem.Icon.Source = new BitmapImage(new Uri(IconType(videoFile), UriKind.Relative));
                        mediaItem.Selected = true;

                        mainWindow.mediaItems.Children.Add(mediaItem);
                    }
                }
                else
                {
                    breadcrumbs.Add(Folder);
                    CreateBreadcrumb(Folder.Name);

                    DirectoryInfo[] folders = Folder.GetDirectories();
                    FileInfo[] files = Folder.GetFiles();

                    string acceptableMediaTypes = acceptableMediaAudioTypes + acceptableMediaPhotoTypes + acceptableMediaVideoTypes + acceptableMediaPlaylistTypes;
                    foreach (DirectoryInfo folder in folders)
                    {
                        if (IsFolderValid(folder))
                        {
                            mediaItem = new MediaItem(this, folder, folder.Name);
                            mainWindow.mediaItems.Children.Add(mediaItem);
                        }
                    }

                    foreach (FileInfo file in files)
                    {
                        //Check if acceptable file type
                        if (acceptableMediaTypes.IndexOf(file.Extension) > -1)
                        {
                            mediaItem = new MediaItem(this, file, System.IO.Path.GetFileNameWithoutExtension(file.Name));
                            mediaItem.Icon.Source = new BitmapImage(new Uri(IconType(file), UriKind.Relative));

                            //Mark the media as Selected as it is already in the list
                            mediaItem.Selected = IsFileInList(file);
                         
                            mainWindow.mediaItems.Children.Add(mediaItem);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logException(ex);
            }
            finally
            {
                Cursor.Current = Cursors.Default;
            }
        }

        private bool IsFileInList(FileInfo file)
        {
            bool bReturn = false;
            FileInfo foundFile = null;

            if (acceptableMediaAudioTypes.Contains(file.Extension) == true)
            {
                foundFile = AudioFiles.Find(e => (e.FullName == file.FullName));
            }
            else if (acceptableMediaPhotoTypes.Contains(file.Extension) == true)
            {
                foundFile = PhotoFiles.Find(e => (e.FullName == file.FullName));
            }
            else if (acceptableMediaVideoTypes.Contains(file.Extension) == true)
            {
                foundFile = VideoFiles.Find(e => (e.FullName == file.FullName));
            }

            if (foundFile != null)
            {
                bReturn = true;
            }

            return bReturn;
        }

        private bool IsArduinoAvailable()
        {
            bool bReturn = true;
            SerialPort port = new SerialPort(COMPort, COMSpeed);

            try
            {
                port.Open(); //Will error here if current settings are incorrect, or the device is offline
            }
            catch 
            {
                bReturn = false;
            }
            finally
            {
                port.Close();
                port.Dispose();
                port = null;
            }

            return bReturn;

        }

        private string IconType(FileInfo file)
        {
            string sReturn = "Assets/Folder.png";

            if (acceptableMediaAudioTypes.Contains(file.Extension) == true)
            {
                sReturn = "Assets/audio.png";
            }
            else if (acceptableMediaPhotoTypes.Contains(file.Extension) == true)
            {
                sReturn = "Assets/photo.png";
            }
            else if (acceptableMediaPlaylistTypes.Contains(file.Extension) == true)
            {
                sReturn = "Assets/playlist.png";
            }
            else if (acceptableMediaVideoTypes.Contains(file.Extension) == true)
            {
                sReturn = "Assets/video.png";
            }

            return sReturn;
        }

        private bool IsPortrait(LoungeMediaFrame mediaFrame)
        {
            bool result = false;

            if (mediaFrame.ActualWidth < mediaFrame.ActualHeight)
            {
                result = true;
            }

            return result;
        }

        private bool IsFolderValid(DirectoryInfo folder)
        {
            //This function insures that only folders that have content, or at least subfolders with content are shown.
            //However on testing on folders with large number of files there becomes a speed issue, a NOTICABLE speed issue
            //Currently this functionality has been turned off, a Advanced Settings dialog may be valuable for a number of items
            
            
            //TEMP
            if ((folder.Attributes & FileAttributes.System) == (FileAttributes.System))
            {
                return false;
            }
            else
            {
                return true;
            }


            //-------------------------------------------------
            bool bResult = false;


            Console.WriteLine("IFV 1: " + DateTime.Now.Minute + "." + DateTime.Now.Second + "." + DateTime.Now.Millisecond);
            if ((folder.Attributes & FileAttributes.System) == (FileAttributes.System))
            {
                bResult = false;
            }
            else
            {
                //Check if there is media in the folder or sub folder, otherwise no need to show it.
                Console.WriteLine("IFV 2: " + DateTime.Now.Minute + "." + DateTime.Now.Second + "." + DateTime.Now.Millisecond);
                DirectoryInfo[] folders = folder.GetDirectories();
                Console.WriteLine("IFV 3: " + DateTime.Now.Minute + "." + DateTime.Now.Second + "." + DateTime.Now.Millisecond);
                bool bFolders = false;

                if (folders.Count() > 0)
                {
                    bFolders = true;
                }

                //No need to check for files, if there are folders
                FileInfo[] Files = folder.GetFiles();
                Console.WriteLine("IFV 4: " + DateTime.Now.Minute + "." + DateTime.Now.Second + "." + DateTime.Now.Millisecond);
                foreach (FileInfo file in Files)
                {
                    if (acceptableMediaAudioTypes.IndexOf(file.Extension.ToLower()) > -1)
                    {
                        bResult = true;
                    }
                    else if (acceptableMediaPhotoTypes.IndexOf(file.Extension.ToLower()) > -1)
                    {
                        bResult = true;
                    }
                    else if (acceptableMediaPlaylistTypes.IndexOf(file.Extension.ToLower()) > -1)
                    {
                        bResult = true;
                    }
                    else if (acceptableMediaVideoTypes.IndexOf(file.Extension.ToLower()) > -1)
                    {
                        bResult = true;
                    }

                    if (bResult == true)
                    {
                        break;
                    }
                }
                Console.WriteLine("IFV 5: " + DateTime.Now.Minute + "." + DateTime.Now.Second + "." + DateTime.Now.Millisecond);
                Files = null;

                //This recursion, on a very deep folder took < 500 milliseconds
                //It recurses all subfolders to see if there is an acceptable file somewhere within this folder.
                if (bResult == false && bFolders == true)
                {
                    foreach (DirectoryInfo subFolder in folders)
                    {
                        if (IsFolderValid(subFolder) == true)
                        {
                            bResult = true;
                            break;
                        }
                    }
                }
                Console.WriteLine("IFV 6: " + DateTime.Now.Minute + "." + DateTime.Now.Second + "." + DateTime.Now.Millisecond);
                folders = null;
            }

            return bResult;
        }

        private bool IsWin10()
        {
            bool bReturn = false;

            try
            {
                System.OperatingSystem osInfo = System.Environment.OSVersion;

                if (osInfo.Version.Major > 5)
                {
                    bReturn = true;
                }
            }
            catch (Exception ex)
            {
                logException(ex);
            }

            return bReturn;
        }

        private string AppName()
        {
            string sAppName = "Lounge"; //Default, just in case

            try
            {
                var list = System.Windows.Application.Current.MainWindow.GetType().Assembly.GetCustomAttributes(typeof(AssemblyProductAttribute), true);
                if (list != null)
                {
                    if (list.Length > 0)
                    {
                        sAppName = (list[0] as AssemblyProductAttribute).Product;
                    }
                }
            }
            catch (Exception ex)
            {
                logException(ex);
            }

            return sAppName;
        }

        private void AppInfo()
        {
            System.Windows.Forms.MessageBox.Show(
                "This application is copyright © 2018 by James Rose" + 
                Environment.NewLine +
                "Version: " + Assembly.GetExecutingAssembly().GetName().Version.ToString() +
                Environment.NewLine +
                "Source code is available at Github.com/Blissgig", 
                applicationName, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void UnloadScene(LoungeMediaFrame mediaFrame)
        {
            try
            {
                if (mediaFrame.Medias.Children.Count > 0)
                {
                    isAnimating = true;
                    List<LoungeMediaPlayer> mediaPlayers = new List<LoungeMediaPlayer>();
                    Storyboard storyboard = new Storyboard();

                    foreach(LoungeMediaPlayer mediaPlayer in mediaFrame.Medias.Children)
                    {
                        mediaPlayers.Add(mediaPlayer);

                        DoubleAnimation animation = new DoubleAnimation();
                        animation.Duration = TimeSpan.FromMilliseconds(1500);
                        animation.From = 1.0;
                        animation.To = 0.0;

                        Storyboard.SetTarget(animation, mediaPlayer);
                        Storyboard.SetTargetProperty(animation, new PropertyPath(System.Windows.Controls.UserControl.OpacityProperty));
                        storyboard.Children.Add(animation);
                    }

                    storyboard.Completed += (sndr, evts) =>
                    {
                        foreach(LoungeMediaPlayer mp in mediaPlayers)
                        {
                            mediaFrame.Medias.Children.Remove(mp);
                        }
                        isAnimating = false;
                    };
                    storyboard.Begin();
                }
            }
            catch (Exception ex)
            {
                logException(ex);
            }
        }

        private void LoadScene(LoungeMediaFrame mediaFrame)
        {
            try
            {
                UnloadScene(mediaFrame);

                //No need to display players if this is not checked off.
                if (!(bool)mainWindow.VideoCount.IsChecked)
                {
                    return;
                }

                byte bPlayerCount = 0;
                double dHeight = 400;
                double dWidth = 400;
                double dBorder = 10;
                var startPoint = new Point(0, 0);
                var endPoint = new Point(0, 0);
                List<byte> playerCount = new List<byte>();
                SolidColorBrush brush = new SolidColorBrush(currentColor);

                mediaFrame.Background = brush;

                //Note: I am not happy with this code
                //      There should be a better way to randomly distribute the media players around the scene.
                //      However, atm I do not have a better solution and I need to get this working at all, vs "academic"
                if (IsPortrait(mediaFrame))
                {
                    //Portrait mode can have 2 or 3 media players
                    playerCount.Add(2);
                    playerCount.Add(3);
                }
                else
                {
                    playerCount.Add(1);
                    playerCount.Add(4);
                }

                bPlayerCount = playerCount[Convert.ToByte(loungeRandom.Next(0, playerCount.Count()))];

                //Player Size
                switch (bPlayerCount)
                {
                    case 1:
                        dHeight = mediaFrame.ActualHeight;
                        dWidth = mediaFrame.ActualWidth;
                        break;

                    case 2:
                        dHeight = (mediaFrame.ActualHeight / 2);
                        dWidth = mediaFrame.ActualWidth;
                        break;

                    case 3:
                        dHeight = (mediaFrame.ActualHeight / 3);
                        dWidth = mediaFrame.ActualWidth;
                        break;

                    case 4:
                        dHeight = (mediaFrame.ActualHeight / 2);
                        dWidth = (mediaFrame.ActualWidth / 2);
                        break;
                }

                for (byte bPlayer = 0; bPlayer < bPlayerCount; bPlayer++)
                {
                    switch (bPlayerCount)
                    {
                        case 1:
                            startPoint = new Point(-mediaFrame.ActualWidth, 0);
                            endPoint = new Point(0, 0);
                            break;

                        case 2:
                            switch (bPlayer)
                            {
                                case 0:
                                    startPoint = new Point(-mediaFrame.ActualWidth, 0);
                                    endPoint = new Point(0, 0);
                                    break;

                                case 1:
                                    startPoint = new Point(-mediaFrame.ActualWidth, (mediaFrame.ActualHeight / 2));
                                    endPoint = new Point(0, (mediaFrame.ActualHeight / 2));
                                    break;
                            }
                            break;

                        case 3:
                            switch (bPlayer)
                            {
                                case 0:
                                    startPoint = new Point(-mediaFrame.ActualWidth, 0);
                                    endPoint = new Point(0, 0);
                                    break;

                                case 1:
                                    startPoint = new Point(-mediaFrame.ActualWidth, (mediaFrame.ActualHeight / 3));
                                    endPoint = new Point(0, (mediaFrame.ActualHeight / 3));
                                    break;

                                case 2:
                                    startPoint = new Point(-mediaFrame.ActualWidth, (mediaFrame.ActualHeight * .66));
                                    endPoint = new Point(0, (mediaFrame.ActualHeight * .66));
                                    break;
                            }
                            break;

                        case 4:
                            switch (bPlayer)
                            {
                                case 0:
                                    startPoint = new Point(-mediaFrame.ActualWidth, 0);
                                    endPoint = new Point(0, 0);
                                    break;

                                case 1:
                                    startPoint = new Point(-mediaFrame.ActualWidth, (mediaFrame.ActualHeight / 2));
                                    endPoint = new Point(0, (mediaFrame.ActualHeight / 2));
                                    break;

                                case 2:
                                    startPoint = new Point((mediaFrame.ActualWidth * 2), 0);
                                    endPoint = new Point((mediaFrame.ActualWidth / 2), 0);
                                    break;

                                case 3:
                                    startPoint = new Point((mediaFrame.ActualWidth * 2), (mediaFrame.ActualWidth / 2));
                                    endPoint = new Point((mediaFrame.ActualWidth / 2), (mediaFrame.ActualHeight / 2));
                                    break;
                            }
                            break;
                    }

                    LoungeMediaPlayer mediaPlayer = new LoungeMediaPlayer();
                    
                    mediaPlayer.Name = "MediaPlayer" + Guid.NewGuid().ToString().Replace("-", "");
                    mediaPlayer.border.BorderBrush = brush;
                    mediaPlayer.mask.Background = brush;
                    mediaPlayer.startPoint = startPoint;
                    mediaPlayer.endPoint = endPoint;

                    int i = loungeRandom.Next(0, VideoFiles.Count);
                    FileInfo media = VideoFiles[i];
                    
                    mediaPlayer.Width = dWidth;
                    mediaPlayer.Height = dHeight;
                    mediaPlayer.LoungeMediaElement.Height = dHeight - (dBorder * 2);
                    mediaPlayer.LoungeMediaElement.Width = dWidth - (dBorder * 2);
                    mediaPlayer.LoungeMediaElement.Margin = new Thickness(dBorder);

                    mediaFrame.Medias.Children.Add(mediaPlayer);
                    Canvas.SetTop(mediaPlayer, startPoint.Y);
                    Canvas.SetLeft(mediaPlayer, startPoint.X);

                    mediaPlayer.LoungeMediaElement.Source = new Uri(media.FullName);
                    mediaPlayer.LoungeMediaElement.Play();
                    mediaPlayer.LoungeMediaElement.MediaOpened += LoungeMediaElement_MediaOpened;
                    mediaPlayer.LoungeMediaElement.MediaEnded += LoungeMediaElement_MediaEnded;
                    mediaPlayer.LoungeMediaElement.MediaFailed += LoungeMediaElement_MediaEnded;
                }
            }
            catch (Exception ex)
            {
                logException(ex);
            }
        }

        private void LoungeMediaElement_MediaEnded(object sender, RoutedEventArgs e)
        {
            try
            {
                var mediaElement = (MediaElement)sender;
                MediaLoad(mediaElement);
            }
            catch (Exception ex)
            {
                logException(ex);
            }
        }

        private void LoungeMediaElement_MediaOpened(object sender, RoutedEventArgs e)
        {
            try
            {
                isAnimating = true;

                //This is awful, must find a better way to most parent object.
                var mediaElement = (MediaElement)sender;
                var grid = (Grid)mediaElement.Parent;
                grid = (Grid)grid.Parent;
                var border = (Border)grid.Parent;
                var canvas = (Canvas)border.Parent;
                var lmp = (LoungeMediaPlayer)canvas.Parent;
                int iMilliseconds = loungeRandom.Next(400, 800); //So that the players don't move in at the same time


                Storyboard storyboard = new Storyboard();
                QuadraticEase ease = new QuadraticEase();
                ease.EasingMode = EasingMode.EaseIn;

                DoubleAnimation animationTop = new DoubleAnimation();
                animationTop.Duration = TimeSpan.FromMilliseconds(iMilliseconds);
                animationTop.From = lmp.startPoint.Y;
                animationTop.To = lmp.endPoint.Y;
                animationTop.EasingFunction = ease;

                Storyboard.SetTarget(animationTop, lmp);
                Storyboard.SetTargetProperty(animationTop, new PropertyPath(Canvas.TopProperty));
                storyboard.Children.Add(animationTop);


                DoubleAnimation animationLeft = new DoubleAnimation();
                animationLeft.Duration = TimeSpan.FromMilliseconds(iMilliseconds);
                animationLeft.From = lmp.startPoint.X;
                animationLeft.To = lmp.endPoint.X;
                animationLeft.EasingFunction = ease;

                Storyboard.SetTarget(animationLeft, lmp);
                Storyboard.SetTargetProperty(animationLeft, new PropertyPath(Canvas.LeftProperty));
                storyboard.Children.Add(animationLeft);

                storyboard.Completed += (sndr, evts) =>
                {
                    isAnimating = false;
                };
                storyboard.Begin();
            }
            catch (Exception ex)
            {
                logException(ex);
            }
        }

        private void MediaTransition(MediaElement mediaElement)
        {
            try
            {
                isAnimating = true;

                var cover = new Rectangle();
                cover.RadiusX = 24;
                cover.RadiusY = 24;

                var grid = (Grid)mediaElement.Parent;
                grid = (Grid)grid.Parent;
                var border = (Border)grid.Parent;
                var canvas = (Canvas)border.Parent;
                var lmp = (LoungeMediaPlayer)canvas.Parent;


                cover.Height = canvas.ActualHeight;
                cover.Width = canvas.ActualWidth;
                cover.Fill = new SolidColorBrush(currentColor);

                lmp.Transition.Children.Add(cover);
                Canvas.SetLeft(cover, 0);
                Canvas.SetTop(cover, 0);

                //This "flash" is to hide the change of media or the media's position
                Storyboard storyboard = new Storyboard();
                DoubleAnimation animation = new DoubleAnimation();
                animation.Duration = TimeSpan.FromMilliseconds(1800);
                animation.From = 1.0;
                animation.To = 0.0;

                Storyboard.SetTarget(animation, cover);
                Storyboard.SetTargetProperty(animation, new PropertyPath(Rectangle.OpacityProperty));
                storyboard.Children.Add(animation);

                storyboard.Completed += (sndr, evts) =>
                {
                    lmp.Transition.Children.Clear();
                    isAnimating = false;
                };
                storyboard.Begin();
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void MediaLoad(MediaElement mediaElement)
        {
            try
            {
                MediaTransition(mediaElement);

                //The label is now a checkbox that can be disabled while playing
                if ((bool)mainWindow.VideoCount.IsChecked)
                {
                    int i = loungeRandom.Next(0, VideoFiles.Count);
                    FileInfo media = VideoFiles[i];
                    mediaElement.Source = new Uri(media.FullName);
                    mediaElement.Play();
                }
            }
            catch (Exception ex)
            {
                logException(ex);
            }
        }

        private void MediaJump(MediaElement mediaElement)
        {
            try
            {
                MediaTransition(mediaElement);

                double totalSeconds = mediaElement.NaturalDuration.TimeSpan.TotalSeconds;
                totalSeconds = loungeRandom.Next(5, Convert.ToInt32(totalSeconds));

                var jump = new TimeSpan(0, 0, Convert.ToInt32(totalSeconds));
                mediaElement.Position = jump;
            }
            catch (Exception ex)
            {
                logException(ex);
            }
        }

        private void MediaRandom(bool createTimer = true)
        {
            try
            {
                LoungeMediaFrame mediaFrame = mediaFrames[loungeRandom.Next(0, mediaFrames.Count)];
                LoungeMediaPlayer mediaPlayer = (LoungeMediaPlayer)mediaFrame.Medias.Children[loungeRandom.Next(0, mediaFrame.Medias.Children.Count)];

                dispatchTimer.Stop();
                dispatchTimer = null;

                int i = loungeRandom.Next(0, 200);

                //50% just change the position of the media
                //25% load new media
                //25% load new scene
                if (i < 100)
                {
                    MediaJump(mediaPlayer.LoungeMediaElement);
                }
                else if (i > 150)
                {
                    MediaLoad(mediaPlayer.LoungeMediaElement); 
                }
                else 
                {
                    //This can cause music and/or media that is playing to skip on some occasions with some systems.  Use infrequently
                    LoadScene(mediaFrame); 
                }

                if (createTimer)
                {
                    CreateTimer();
                }                
            }
            catch (Exception ex)
            {
                logException(ex);
            }
        }

        private void LoadWindows()
        {
            try
            {
                System.Drawing.Rectangle workingArea;
                bool bMonitor = true;

                foreach (Screen scr in Screen.AllScreens)
                {
                    workingArea = scr.WorkingArea;

                    //The primary monitor is only false if the checkbox is NOT selected
                    if (scr.Primary == true && (bool)mainWindow.primaryMonitor.IsChecked == false)
                    {
                        bMonitor = false;
                    }
                    else
                    {
                        bMonitor = true;
                    }

                    if (bMonitor == true)
                    {
                        LoungeMediaFrame loungeMediaFrame = new LoungeMediaFrame(this);
                        loungeMediaFrame.Name = "Display" + scr.DeviceName.Replace('\\', '_').Replace('.', 'A');
                        loungeMediaFrame.PrimaryMonitor = scr.Primary; 

                        loungeMediaFrame.Left = workingArea.Left;
                        loungeMediaFrame.Top = workingArea.Top;
                        loungeMediaFrame.Width = workingArea.Width;
                        loungeMediaFrame.Height = workingArea.Height;

                        loungeMediaFrame.Show();
                        mediaFrames.Add(loungeMediaFrame);
                    }
                }
            }
            catch (Exception ex)
            {
                logException(ex);
            }
        }

        public void UnloadWindow(string name)
        {
            try
            {
                LoungeMediaFrame mediaFrame = mediaFrames.Find(r => r.Name == name);

                if (mediaFrame != null)
                {
                    mediaFrames.Remove(mediaFrame);
                }
            }
            catch 
            {  }
        }

        private void UnloadWindows()
        {
            try
            {
                foreach(LoungeMediaFrame mediaFrame in mediaFrames)
                {
                    UnloadWindow(mediaFrame.Name);
                }
            }
            catch (Exception ex)
            {
                logException(ex);
            }
        }

        private void CreateTimer()
        {
            try
            {
                dispatchTimer = new DispatcherTimer();
                dispatchTimer.Tick += Dispatch_Tick;
                dispatchTimer.Interval = TimeSpan.FromSeconds(loungeRandom.Next(minimumSceneTime, maximumSceneTime));
                dispatchTimer.Start();
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void Dispatch_Tick(object sender, EventArgs e)
        {
            try
            {
                MediaRandom();
            }
            catch (Exception ex)
            {
                logException(ex);
            }
        }

        private void ColorUpdate()
        {
            try
            {
                //This should happen first because of possible delays (minor) to the LEDs
                LEDUpdate();
             
                var background = new SolidColorBrush(currentColor);

                foreach (LoungeMediaFrame mediaFrame in mediaFrames)
                {
                    mediaFrame.Background = background;
                    mediaFrame.Medias.Background = background;

                    foreach(LoungeMediaPlayer mediaPlayer in mediaFrame.Medias.Children)
                    {
                         mediaPlayer.mask.Background = background; 
                    }
                    
                    if (mediaFrame.Visualizations.Children.Count > 0)
                    {
                        foreach(var item in mediaFrame.Visualizations.Children)
                        {
                            if (item.GetType() == typeof(Border))
                            {
                                Border border = (Border)item;
                                border.Background = background;
                            }
                            else if (item.GetType() == typeof(Ellipse))
                            {
                                Ellipse bubble = (Ellipse)item;
                                bubble.Fill = background;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logException(ex);
            }
        }

        private void ColorUpdated(string value)
        {
            try
            {
                switch (value)
                {
                    case "All":
                        mainWindow.RedLow.Text = "0";
                        mainWindow.RedHigh.Text = "255";
                        mainWindow.GreenLow.Text = "0";
                        mainWindow.GreenHigh.Text = "255";
                        mainWindow.BlueLow.Text = "0";
                        mainWindow.BlueHigh.Text = "255";
                        break;

                    case "Black and Gray":
                        mainWindow.RedLow.Text = "0";
                        mainWindow.RedHigh.Text = "48";
                        mainWindow.GreenLow.Text = "0";
                        mainWindow.GreenHigh.Text = "48";
                        mainWindow.BlueLow.Text = "0";
                        mainWindow.BlueHigh.Text = "48";
                        break;

                    case "Reds - All":
                        mainWindow.RedLow.Text = "0";
                        mainWindow.RedHigh.Text = "255";
                        mainWindow.GreenLow.Text = "0";
                        mainWindow.GreenHigh.Text = "0";
                        mainWindow.BlueLow.Text = "0";
                        mainWindow.BlueHigh.Text = "0";
                        break;

                    case "Reds - Bright":
                        mainWindow.RedLow.Text = "100";
                        mainWindow.RedHigh.Text = "255";
                        mainWindow.GreenLow.Text = "0";
                        mainWindow.GreenHigh.Text = "0";
                        mainWindow.BlueLow.Text = "0";
                        mainWindow.BlueHigh.Text = "0";
                        break;

                    case "Blues":
                        mainWindow.RedLow.Text = "0";
                        mainWindow.RedHigh.Text = "0";
                        mainWindow.GreenLow.Text = "0";
                        mainWindow.GreenHigh.Text = "0";
                        mainWindow.BlueLow.Text = "0";
                        mainWindow.BlueHigh.Text = "255";
                        break;

                    case "Greens":
                        mainWindow.RedLow.Text = "0";
                        mainWindow.RedHigh.Text = "0";
                        mainWindow.GreenLow.Text = "0";
                        mainWindow.GreenHigh.Text = "250";
                        mainWindow.BlueLow.Text = "0";
                        mainWindow.BlueHigh.Text = "0";
                        break;

                    case "Pinks":
                        mainWindow.RedLow.Text = "240";
                        mainWindow.RedHigh.Text = "255";
                        mainWindow.GreenLow.Text = "180";
                        mainWindow.GreenHigh.Text = "130";
                        mainWindow.BlueLow.Text = "133";
                        mainWindow.BlueHigh.Text = "180";
                        break;

                    case "Purples":
                        mainWindow.RedLow.Text = "128";
                        mainWindow.RedHigh.Text = "255";
                        mainWindow.GreenLow.Text = "0";
                        mainWindow.GreenHigh.Text = "130";
                        mainWindow.BlueLow.Text = "200";
                        mainWindow.BlueHigh.Text = "250";
                        break;
                }

                ColorUpdate();
            }
            catch (Exception ex)
            {
                logException(ex);
            }
        }

        private void ColorsRecalc()
        {
            try
            {
                byte bR = Convert.ToByte(loungeRandom.Next(Convert.ToByte(mainWindow.RedLow.Text.Trim()), Convert.ToByte(mainWindow.RedHigh.Text.Trim())));
                byte bG = Convert.ToByte(loungeRandom.Next(Convert.ToByte(mainWindow.GreenLow.Text.Trim()), Convert.ToByte(mainWindow.GreenHigh.Text.Trim())));
                byte bB = Convert.ToByte(loungeRandom.Next(Convert.ToByte(mainWindow.BlueLow.Text.Trim()), Convert.ToByte(mainWindow.BlueHigh.Text.Trim())));
                currentColor = Color.FromRgb(bR, bG, bB);

                ColorUpdate();
            }
            catch {  }
        }

        public void Dispose()
        {
            try
            {
                loungeAnalyzer.Enable = false;
                loungeAnalyzer.DisplayEnable = false;
                loungeAnalyzer = null;
                
                mediaFrames.Clear();

                System.Windows.Application.Current.Shutdown();
            }
            catch 
            { }
        }
        
        private void VisualizationSelect()
        {
            try
            {
                List<string> visualizations = new List<string>();

                foreach(System.Windows.Controls.CheckBox checkbox in mainWindow.visualizations.Items)
                { 
                    if ((bool)checkbox.IsChecked)
                    {
                        visualizations.Add(checkbox.Content.ToString());
                    }
                }

                string newVisualization = visualizations[loungeRandom.Next(0, visualizations.Count)];

                //Only need to update if the visualization has changed
                if (newVisualization != this.currentVisualization)
                {
                    this.currentVisualization = newVisualization;
                    VisualizationSetup();
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void VisualizationSetup()
        {
            try
            {
                double width;
                double left = 0;
                double top = 0;
                byte seconds = 4;
                byte margin = 10;
                Ellipse Bubble;
                SolidColorBrush background = new SolidColorBrush(currentColor);
                SolidColorBrush secondary = new SolidColorBrush(secondaryColor);

                //Based on current visualization, setup the vis canvas.
                foreach (LoungeMediaFrame mediaFrame in mediaFrames)
                {
                    mediaFrame.Visualizations.Children.Clear(); //TODO: might be nice to fade out existing items

                    switch (currentVisualization.ToLower())
                    {
                        #region Bars
                        case "bars":
                            Border bar;
                            width = (mediaFrame.Width / loungeAnalyzer.spectrumLines);
                            top = (mediaFrame.Height / 2);
                            left = 0;

                            for (int iBar = 0; iBar < loungeAnalyzer.spectrumLines; iBar++)
                            {
                                bar = new Border();
                                bar.Name = "bar" + iBar.ToString();
                                bar.Width = width;
                                bar.BorderThickness = new Thickness(2);
                                bar.CornerRadius = new CornerRadius(10);
                                bar.Margin = new Thickness(1);
                                bar.BorderBrush = new SolidColorBrush(Colors.Black);
                                bar.Background = new SolidColorBrush(currentColor);
                                bar.Opacity = 1;

                                mediaFrame.Visualizations.Children.Add(bar);
                                Canvas.SetLeft(bar, left);
                                Canvas.SetTop(bar, top);
                                left += width;
                            }
                            break;
                        #endregion

                        #region Bounce
                        case "bounce":
                            width = Convert.ToInt16(mediaFrame.ActualWidth / loungeAnalyzer.spectrumLines);
                            top = mediaFrame.ActualHeight - width;

                            left = 0;
                            for (byte b = 0; b < loungeAnalyzer.spectrumLines; b++)
                            {
                                Ellipse bubble = new Ellipse();
                                bubble.Width = width;
                                bubble.Height = width;
                                bubble.Fill = background;
                                bubble.Opacity = 0.0;
                                bubble.Stroke = secondary;
                                bubble.StrokeThickness = 2;
                                mediaFrame.Visualizations.Children.Add(bubble);
                                Canvas.SetLeft(bubble, left);
                                Canvas.SetTop(bubble, top);

                                left += width;
                            }
                            break;
                        #endregion

                        #region Champagne
                        case "champagne":
                            Storyboard storyboardChampagne = new Storyboard();
                            
                            for (byte b = 0; b < loungeAnalyzer.spectrumLines; b++)
                            {
                                Bubble = new Ellipse();
                                Bubble.Width = loungeRandom.Next(16, 44);
                                Bubble.Height = Bubble.Width;
                                Bubble.Fill = background;
                                Bubble.Stroke = secondary;
                                Bubble.StrokeThickness = 4;
                                Bubble.Opacity = 1;
                                mediaFrame.Visualizations.Children.Add(Bubble);
                                left = loungeRandom.Next(100, Convert.ToInt16(mediaFrame.Width - 100));
                                top = loungeRandom.Next(Convert.ToInt16(mediaFrame.Height), Convert.ToInt16(mediaFrame.Height + 100)); //To give a random starting point
                                
                                Canvas.SetTop(Bubble, top);
                                Canvas.SetLeft(Bubble, left); //To give a random starting point

                                seconds = Convert.ToByte(loungeRandom.Next(4, 12));

                                //Left
                                DoubleAnimation animationLeft = new DoubleAnimation();
                                animationLeft.Duration = new Duration(new TimeSpan(0, 0, seconds));
                                animationLeft.From = left;
                                animationLeft.To = loungeRandom.Next(margin, Convert.ToInt16(mediaFrame.Height - margin)); ;
                                animationLeft.RepeatBehavior = RepeatBehavior.Forever;
                                Storyboard.SetTarget(animationLeft, Bubble);
                                Storyboard.SetTargetProperty(animationLeft, new PropertyPath(Canvas.LeftProperty));
                                storyboardChampagne.Children.Add(animationLeft);

                                //Top
                                DoubleAnimation animationTop = new DoubleAnimation();
                                animationTop.Duration = new Duration(new TimeSpan(0, 0, seconds));
                                animationTop.From = top;
                                animationTop.To = -loungeRandom.Next(100, 200);
                                animationTop.RepeatBehavior = RepeatBehavior.Forever;

                                Storyboard.SetTarget(animationTop, Bubble);
                                Storyboard.SetTargetProperty(animationTop, new PropertyPath(Canvas.TopProperty));
                                storyboardChampagne.Children.Add(animationTop);
                            }

                            storyboardChampagne.Begin();
                            break;

                        #endregion

                        #region Float
                        case "float":
                            Storyboard bubbleStoryboard = new Storyboard();

                            for (byte b = 0; b < loungeAnalyzer.spectrumLines; b++)
                            {
                                Bubble = new Ellipse();
                                Bubble.Width = loungeRandom.Next(16, 32);
                                Bubble.Height = Bubble.Width;
                                Bubble.Fill = background;
                                Bubble.Stroke = secondary;
                                Bubble.StrokeThickness = 2;
                                Bubble.Opacity = 0.4;


                                mediaFrame.Visualizations.Children.Add(Bubble);
                                left = loungeRandom.Next(margin, Convert.ToInt16(mediaFrame.Width - (margin + Bubble.Height)));
                                top = loungeRandom.Next(margin, Convert.ToInt16(mediaFrame.Height - (margin + Bubble.Height))); //To give a random starting point

                                Canvas.SetTop(Bubble, top);
                                Canvas.SetLeft(Bubble, left); //To give a random starting point

                                seconds = Convert.ToByte(loungeRandom.Next(4, 12));

                                DoubleAnimation aniLeft = new DoubleAnimation();
                                aniLeft.Duration = TimeSpan.FromSeconds(seconds);
                                aniLeft.From = left;
                                aniLeft.To = loungeRandom.Next(margin, Convert.ToInt16(mediaFrame.ActualWidth - (margin + Bubble.Width)));
                                aniLeft.AutoReverse = true;
                                aniLeft.RepeatBehavior = RepeatBehavior.Forever;

                                Storyboard.SetTarget(aniLeft, Bubble);
                                Storyboard.SetTargetProperty(aniLeft, new PropertyPath(Canvas.LeftProperty));
                                bubbleStoryboard.Children.Add(aniLeft);

                                DoubleAnimation aniTop = new DoubleAnimation();
                                aniTop.Duration = TimeSpan.FromSeconds(seconds);
                                aniTop.From = top;
                                aniTop.To = loungeRandom.Next(margin, Convert.ToInt16(mediaFrame.ActualHeight - (margin + Bubble.Width)));
                                aniTop.AutoReverse = true;
                                aniTop.RepeatBehavior = RepeatBehavior.Forever;

                                Storyboard.SetTarget(aniTop, Bubble);
                                Storyboard.SetTargetProperty(aniTop, new PropertyPath(Canvas.TopProperty));
                                bubbleStoryboard.Children.Add(aniTop);
                            }

                            bubbleStoryboard.Begin();
                            break;
                            #endregion

                    }
                }
            }
            catch (Exception ex)
            {
                logException(ex);
            }
        }

        public void Visualization(List<byte> visualData)
        {
            try
            {
                bool isBoom = false; //used to trigger function(s) when bass is high enough
                Ellipse bubble;

                //Get the boom
                for (int i = 0; i < 20; i++)
                {
                    if (visualData[i] > BASS_LEVEL)
                    {
                        currentLEDBrightness = Convert.ToByte(visualData[i] / 2);
                        isBoom = true;
                        break;
                    }
                }

                switch (currentVisualization.ToLower())
                {
                    #region Bars
                    case "bars":
                        Border bar;
                        for (int iValue = 0; iValue < visualData.Count; iValue++)
                        {
                            foreach (LoungeMediaFrame mediaFrame in mediaFrames)
                            {
                                bar = (Border)mediaFrame.Visualizations.Children[iValue];
                                bar.Opacity = (visualData[iValue] * .01);
                                bar.Height =  ((mediaFrame.ActualHeight / 255) * visualData[iValue]);
                                Canvas.SetTop(bar, (mediaFrame.Height / 2) - (bar.Height / 2));
                            }
                        }
                        break;
                    #endregion

                    #region Bounce
                    case "bounce":
                        for (int iValue = 0; iValue < visualData.Count; iValue++)
                        {
                            foreach (LoungeMediaFrame mediaFrame in mediaFrames)
                            {
                                bubble = (Ellipse)mediaFrame.Visualizations.Children[iValue];
                                bubble.Opacity = ((visualData[iValue] * 0.39) * .01);
                                Canvas.SetTop(bubble, (mediaFrame.ActualHeight - ((mediaFrame.ActualHeight / 255) * visualData[iValue])));
                            }
                        }
                        break;
                    #endregion

                    #region Champagne
                    case "champagne":
                        for (int iValue = 0; iValue < visualData.Count; iValue++)
                        {
                            foreach (LoungeMediaFrame mediaFrame in mediaFrames)
                            {
                                bubble = (Ellipse)mediaFrame.Visualizations.Children[iValue];
                                bubble.Opacity = ((visualData[iValue] * 0.39) * .01); //255  * .39 = 99.45, then *.01 = .99 max value
                            }
                        }
                        break;
                    #endregion

                    #region Float
                    case "float":
                        if (!isAnimating)
                        {
                            isAnimating = true;

                            double size = 0;
                            Storyboard storyboard = new Storyboard();
                            Duration duration = new Duration(new TimeSpan(0, 0, 0, 0, 400));

                            for (int iValue = 0; iValue < visualData.Count; iValue++)
                            {
                                foreach (LoungeMediaFrame mediaFrame in mediaFrames)
                                {
                                    bubble = (Ellipse)mediaFrame.Visualizations.Children[iValue];
                                    bubble.Opacity = ((visualData[iValue] * 0.39) * .01); //(255 * .39) = 99.45, then *.01 = .99 max value

                                    if (visualData[iValue] > 60)
                                    {
                                        size = (bubble.ActualWidth + (visualData[iValue] * 2.8)) / bubble.ActualWidth;

                                        ScaleTransform scale = new ScaleTransform(1.0, 1.0);
                                        bubble.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);
                                        bubble.RenderTransform = scale;

                                        DoubleAnimation anmWidth = new DoubleAnimation();
                                        anmWidth.Duration = duration;
                                        anmWidth.From = 1; 
                                        anmWidth.To = size;
                                        anmWidth.AutoReverse = true;

                                        Storyboard.SetTargetProperty(anmWidth, new PropertyPath("RenderTransform.ScaleX"));
                                        Storyboard.SetTarget(anmWidth, bubble);
                                        storyboard.Children.Add(anmWidth);


                                        DoubleAnimation anmHeight = new DoubleAnimation();
                                        anmHeight.Duration = duration;
                                        anmHeight.From = 1; 
                                        anmHeight.To = size;
                                        anmHeight.AutoReverse = true;

                                        Storyboard.SetTargetProperty(anmHeight, new PropertyPath("RenderTransform.ScaleY"));
                                        Storyboard.SetTarget(anmHeight, bubble);
                                        storyboard.Children.Add(anmHeight);
                                    }
                                }
                            }

                            storyboard.Completed += (sndr, evts) =>
                            {
                                isAnimating = false;
                            };
                            storyboard.Begin();
                        }
                        break;
                    #endregion
                }

                if (isBoom)
                {
                    //Not all Booms affect color changes
                    var diffInSeconds = (DateTime.Now - lastBoom).TotalMilliseconds;
                    if ((diffInSeconds > 1400) && (isAnimating == false))
                    {
                        ColorsRecalc();
                        MediaRandom(false);
                        lastBoom = DateTime.Now; //Reset the Boom
                    }

                    //All booms generate a photo process
                    PhotoDisplay();
                }
            }
            catch (Exception ex)
            {
                logException(ex);
            }
        }
        
        private void PhotoDisplay()
        {
            try
            {
                if ((PhotoFiles.Count > 0) && ((bool)mainWindow.PhotoCount.IsChecked))
                {
                    Border border = new Border();
                    Image photo = new Image();
                    double from = 0.0;
                    double to = 1.0;
                    byte stroke = 18;
                    bool reverse = true;

                    LoungeMediaFrame mediaFrame = mediaFrames[loungeRandom.Next(0, mediaFrames.Count)];
                    FileInfo file = PhotoFiles[loungeRandom.Next(0, PhotoFiles.Count)];
                    BitmapImage image = new BitmapImage(new Uri(file.FullName));
                    
                    double left = loungeRandom.Next(stroke, Convert.ToInt16(mediaFrame.ActualWidth * .7));
                    double top = loungeRandom.Next(stroke, Convert.ToInt16(mediaFrame.ActualHeight * .66)); //Images are defauled to 1/3 the size of the media frame.  This may change later based on input during testing.


                    System.Windows.Media.Effects.DropShadowEffect effect = new System.Windows.Media.Effects.DropShadowEffect();
                    effect.Color = currentColor;
                    effect.Opacity = 1.0;
                    effect.ShadowDepth = 0;
                    effect.BlurRadius = 50;

                    photo.Source = image;
                    photo.Effect = effect;

                    border.Background = new SolidColorBrush(currentColor); 
                    border.Height = (mediaFrame.ActualHeight * .34); 
                    border.Child = photo;
                    border.Effect = effect;

                    mediaFrame.Photos.Children.Add(border);
                    Canvas.SetLeft(border, top);
                    Canvas.SetTop(border, left);

                    Storyboard storyboard = new Storyboard();
                    DoubleAnimation animation = new DoubleAnimation();
                    animation.Duration = TimeSpan.FromMilliseconds(loungeRandom.Next(minimumPhoto, maximumPhoto));

                    //A few simple options for how the images are displayed
                    if (loungeRandom.Next(0, 100) < 50)
                    {
                        from = 1.0;
                        to = 0.0;
                        reverse = false;
                    }
                    
                    animation.From = from;
                    animation.To = to;
                    animation.AutoReverse = reverse;

                    Storyboard.SetTarget(animation, border);
                    Storyboard.SetTargetProperty(animation, new PropertyPath(Border.OpacityProperty));

                    storyboard.Children.Add(animation);
                    storyboard.Completed += (sndr, evts) =>
                    {
                        mediaFrame.Photos.Children.Remove(border);
                    };
                    storyboard.Begin();
                }
            }
            catch (Exception ex)
            {
                logException(ex);
            }
        }

        public void logException(Exception ex)
        {
            try
            {
                var st = new StackTrace(ex, true);

                for (int iFrame = 0; iFrame < st.FrameCount; iFrame++)
                {
                    var frame = st.GetFrame(iFrame);
                    var line = st.GetFrame(iFrame).GetFileLineNumber();

                    //Some .net internals are erroring but not returning a line number
                    if (line > 0)
                    {
                        MethodBase site = ex.TargetSite;
                        string sMethodName = site == null ? null : site.Name;


                        string sPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + "\\" + applicationName + "\\Exceptions.txt";

                        if (File.Exists(sPath) == false)
                        {
                            using (StreamWriter sw = File.CreateText(sPath))
                            {
                                sw.WriteLine(DateTime.Now.ToString() + ".  Line: " + line.ToString() + ".  Method: " + sMethodName + ".  Exception: " + ex.Message);
                            }
                        }
                        else
                        {
                            using (StreamWriter sw = File.AppendText(sPath))
                            {
                                sw.WriteLine("------------------");
                                sw.WriteLine(DateTime.Now.ToString());
                                sw.WriteLine("Line: " + line.ToString());
                                sw.WriteLine("Method: " + sMethodName);
                                sw.WriteLine("Exception: " + ex.Message);
                                sw.Write(ex.StackTrace.ToString());
                                sw.WriteLine("");
                            }
                        }
                    } //line > 0
                }
            }
            catch { }
        }
        #endregion
    }

    public class AudioDeviceInfo
    {
        public int DeviceID { get; set; }
        public string Name { get; set; }
    }

    internal class Analyzer
    {
        private bool _enable;               //enabled status
        private DispatcherTimer dispatchTimer;  //timer that refreshes the display
        private byte timerTime = 25;
        public float[] _fft;               //buffer for fft data
        private WASAPIPROC _process;        //callback function to obtain data
        private int _lastlevel;             //last output level
        private int _hanctr;                //last output level counter
        public List<byte> visualizationData = new List<byte>();   //spectrum data buffer
        private List<AudioDeviceInfo> AudioDevices = new List<AudioDeviceInfo>();     //NEW non-UI device list
        private bool _initialized;          //initialized flag
        private int devindex;               //used device index
        public int spectrumLines = 64;      //number of spectrum lines
        private LoungeEngine loungeEngine;

        public Analyzer(LoungeEngine loungeEngine)
        {  
            try
            {
                this.loungeEngine = loungeEngine;

                _fft = new float[8192];
                _lastlevel = 0;
                _hanctr = 0;
                dispatchTimer = new DispatcherTimer();
                dispatchTimer.Tick += Tick;
                dispatchTimer.Interval = TimeSpan.FromMilliseconds(timerTime); 
                _process = new WASAPIPROC(Process);
                _initialized = false;

                Init();
            }
            catch (Exception)
            {
                throw;
            }
        }
        
        // flag for display enable
        public bool DisplayEnable { get; set; }

        //flag for enabling and disabling program functionality
        public bool Enable
        {
            get { return _enable; }
            set
            {
                _enable = value;
                if (value)
                {
                    if (!_initialized)
                    {
                        if (loungeEngine.mainWindow.audioDevices.SelectedIndex > -1)
                        {
                            devindex = AudioDevices[loungeEngine.mainWindow.audioDevices.SelectedIndex].DeviceID;

                            bool result = BassWasapi.BASS_WASAPI_Init(devindex, 0, 0, BASSWASAPIInit.BASS_WASAPI_BUFFER, 1f, 0.05f, _process, IntPtr.Zero);
                            if (!result)
                            {
                                var error = Bass.BASS_ErrorGetCode();
                            }
                            else
                            {
                                _initialized = true;
                            }
                        }
                        else
                        {
                            _initialized = false;
                        }
                    }
                    BassWasapi.BASS_WASAPI_Start();
                    dispatchTimer.IsEnabled = true;
                }
                else
                {
                    BassWasapi.BASS_WASAPI_Stop(true);
                    System.Threading.Thread.Sleep(500);
                    dispatchTimer.IsEnabled = false;
                }
            }
        }

        private void Init()
        {
            bool result = false;
            int iCount = BassWasapi.BASS_WASAPI_GetDeviceCount();

            for (int i = 0; i < iCount; i++)
            {
                var device = BassWasapi.BASS_WASAPI_GetDeviceInfo(i);
                if (device.IsEnabled && device.IsLoopback)
                {
                    AudioDeviceInfo audioDeviceInfo = new AudioDeviceInfo();
                    audioDeviceInfo.DeviceID = i; 
                    audioDeviceInfo.Name = device.name;
                    AudioDevices.Add(audioDeviceInfo);

                    loungeEngine.mainWindow.audioDevices.Items.Add(device.name);
                }
            }
            
            Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_UPDATETHREADS, false);
            result = Bass.BASS_Init(0, 44100, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero);
            if (!result) throw new Exception("Init Error");
        }

        private void Tick(object sender, EventArgs e)
        {
            try
            {
                int ret = BassWasapi.BASS_WASAPI_GetData(_fft, (int)BASSData.BASS_DATA_FFT8192);  //get ch.annel fft data
                if (ret < -1) return;
                int x, y;
                int b0 = 0;

                //computes the spectrum data, the code is taken from a bass_wasapi sample.
                for (x = 0; x < spectrumLines; x++)
                {
                    float peak = 0;
                    int b1 = (int)Math.Pow(2, x * 10.0 / (spectrumLines - 1));
                    if (b1 > 1023) b1 = 1023;
                    if (b1 <= b0) b1 = b0 + 1;
                    for (; b0 < b1; b0++)
                    {
                        if (peak < _fft[1 + b0]) peak = _fft[1 + b0];
                    }

                    y = (int)(Math.Sqrt(peak) * 3 * 255 - 4);
                    if (y > 255)
                    {
                        y = 255;
                    }
                    else if (y < 0)
                    {
                        y = 0;
                    }
                    visualizationData.Add((byte)y);
                }

                //Send visualization data back to engine to render on all windows
                loungeEngine.Visualization(visualizationData);

                visualizationData.Clear();

                int level = BassWasapi.BASS_WASAPI_GetLevel();
                if (level == _lastlevel && level != 0) _hanctr++;
                _lastlevel = level;

                //Required, because some programs hang the output. If the output hangs for a 75ms
                //this piece of code re initializes the output so it doesn't make a gliched sound for long.
                if (_hanctr > 3)
                {
                    _hanctr = 0;
                    Free();
                    Bass.BASS_Init(0, 44100, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero);
                    _initialized = false;
                    Enable = true;
                }

            }
            catch 
            {
                //eat the error, I do not care atm
            }
        }

        // WASAPI callback, required for continuous recording
        private int Process(IntPtr buffer, int length, IntPtr user)
        {
            return length;
        }

        //cleanup
        public void Free()
        {
            try
            {
                BassWasapi.BASS_WASAPI_Free();
                Bass.BASS_Free();
            }
            catch { }
        }
    }
}