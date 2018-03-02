using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Lounge.Models;
using System.Collections.ObjectModel;
using Un4seen.Bass;
using Un4seen.BassWasapi;
using System.Windows.Threading;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows;

namespace Lounge
{
    class LoungeEngine
    {
        #region Private Members
        public MainWindow mainWindow;
        private List<FileInfo> AudioFiles = new List<FileInfo>();
        private List<FileInfo> VideoFiles = new List<FileInfo>();
        private List<FileInfo> PhotoFiles = new List<FileInfo>();

        private int currentAudio = 0; //See "AudioNext"
        private int minimumSceneTime = 10;  //Seconds
        private int maximumSceneTime = 20;

        private string applicationName = "Lounge";
        private string acceptableMediaVideoTypes = "*.avi,*.asf,*.mp4,*.m4v,*.mpg,*.mpeg,*.mpeg2,*.mpeg4,*.wmv,*.3gp,*.mov,*.mts,*.divx,";
        private string acceptableMediaPhotoTypes = "*.png,*.jpg,*.jpeg,";
        private string acceptableMediaAudioTypes = "*.mp3,*.wma,*.wav,*.m4a,";
        
        private ObservableCollection<FileFolderData> filesFolders = new ObservableCollection<FileFolderData>();
        private List<LoungeMediaFrame> mediaFrames = new List<LoungeMediaFrame>();
        private List<DirectoryInfo> breadcrumbs = new List<DirectoryInfo>();

        private Analyzer loungeAnalyzer;
        private Random loungeRandom = new Random(DateTime.Now.Millisecond);
        #endregion

        public LoungeEngine(MainWindow window)
        {
            try
            {
                mainWindow = window;

                SettingsLoad(); //Call before anything else runs.
                
                ListFiles(null);
            }
            catch (Exception ex)
            {
                logException(ex);
            }  
        }

        private void SettingsLoad()
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


            }
            catch (Exception ex)
            {
                logException(ex);
            }
        }

        public void MediaPlay()
        {
            try
            {
                if ((AudioFiles.Count() == 0) && (VideoFiles.Count() == 0) && (PhotoFiles.Count() == 0))
                {
                    System.Windows.Forms.MessageBox.Show("You have to add SOME media to play", "No media selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    mainWindow.WindowState = System.Windows.WindowState.Minimized;

                    AudioNext();

                    LoadWindows();
                    
                    foreach (LoungeMediaFrame mediaFrame in mediaFrames)
                    {
                        LoadScene(mediaFrame);
                    }
                }
            }
            catch (Exception ex)
            {
                logException(ex);
            }
        }

        public void RowSelected()
        {
            try
            {
                FileFolderData ffd = (FileFolderData)mainWindow.FoldersFiles.SelectedItem;

                if (ffd != null)
                {
                    switch (ffd.Type)
                    {
                        case FileFolderData.FileFolderType.Folder:
                            breadcrumbs.Add(ffd.Folder); //This is used when the user selects "Back"
                            ListFiles(ffd.Folder);
                            break;

                        case FileFolderData.FileFolderType.File:

                            ffd.Selected = !ffd.Selected;

                            //Add/Remove from media lists
                            if (acceptableMediaAudioTypes.IndexOf(ffd.File.Extension) > -1)
                            {
                                AddRemoveMedia(AudioFiles, ffd);
                            }
                            else if (acceptableMediaPhotoTypes.IndexOf(ffd.File.Extension) > -1)
                            {
                                AddRemoveMedia(PhotoFiles, ffd);
                            }
                            else if (acceptableMediaVideoTypes.IndexOf(ffd.File.Extension) > -1)
                            {
                                AddRemoveMedia(VideoFiles, ffd);
                            }
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                logException(ex);
            }
        }

        public void SelectAll()
        {
            try
            {
                foreach(FileFolderData ffd in filesFolders)
                {
                    if (ffd.Type == FileFolderData.FileFolderType.File)
                    {
                        ffd.Selected = true;

                        if (acceptableMediaAudioTypes.IndexOf(ffd.File.Extension) > -1)
                        {
                            AddRemoveMedia(AudioFiles, ffd);
                        }
                        else if (acceptableMediaPhotoTypes.IndexOf(ffd.File.Extension) > -1)
                        {
                            AddRemoveMedia(PhotoFiles, ffd);
                        }
                        else if (acceptableMediaVideoTypes.IndexOf(ffd.File.Extension) > -1)
                        {
                            AddRemoveMedia(VideoFiles, ffd);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logException(ex);
            }
        }

        public void Back()
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

        public void AddRemoveMedia(List<FileInfo> files, FileFolderData ffd)
        {
            try
            {
                if (ffd.Selected)
                {
                    files.Add(ffd.File);
                }
                else
                {
                    FileInfo fileFound = files.Find(x => x.FullName == ffd.File.FullName);

                    if (fileFound != null)
                    {
                        files.Remove(fileFound);
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void AudioNext()
        {
            try
            {
                //To insure that this function is only called while in a loop or available media
                if (currentAudio < AudioFiles.Count())
                {
                    FileInfo file = AudioFiles[currentAudio];

                    mainWindow.AudioElement.Source = new Uri(file.FullName);
                    mainWindow.AudioElement.Volume = mainWindow.AudioVolume.Value;
                    mainWindow.AudioElement.Play();
                    

                    currentAudio++;

                    if (currentAudio >= AudioFiles.Count() && mainWindow.loopAudio.IsChecked == true)
                    {
                        currentAudio = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                logException(ex);
            }
        }

        public void AudioPrior()
        {
            currentAudio--;
            if (currentAudio < 0)
            {
                currentAudio = 0;
            }

            AudioNext();
        }

        public void AudioVolume()
        {
            mainWindow.AudioElement.Volume = mainWindow.AudioVolume.Value;
        }

        public void ListFiles(DirectoryInfo directory)
        {
            try
            {
                Cursor.Current = Cursors.WaitCursor;

                //In case no starting directory, assume a list of drives
                filesFolders.Clear();

                FileFolderData ffd;

                if (directory == null)
                {
                    breadcrumbs.Clear();

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

                            ffd = new FileFolderData();
                            ffd.Type = FileFolderData.FileFolderType.Folder;
                            ffd.Date = DateTime.Now;
                            ffd.Name = sTemp;
                            ffd.Folder = new DirectoryInfo(drv.Name);

                            filesFolders.Add(ffd);
                        }
                    }
                }
                else
                {
                    DirectoryInfo[] folders = directory.GetDirectories();
                    FileInfo[] files = directory.GetFiles();
                    string acceptableMediaTypes = acceptableMediaAudioTypes + acceptableMediaPhotoTypes + acceptableMediaVideoTypes;
                    foreach (DirectoryInfo folder in folders)
                    {
                        if (IsFolderValid(folder))
                        {
                            ffd = new FileFolderData();
                            ffd.Type = FileFolderData.FileFolderType.Folder;
                            ffd.Date = Directory.GetCreationTime(folder.FullName);
                            ffd.Name = folder.Name;
                            ffd.Folder = folder;

                            filesFolders.Add(ffd);
                        }
                    }

                    foreach(FileInfo file in files)
                    {
                        //Check if acceptable file type
                        if (acceptableMediaTypes.IndexOf(file.Extension) > -1)
                        {
                            ffd = new FileFolderData();
                            ffd.Type = FileFolderData.FileFolderType.File;
                            ffd.Date = File.GetCreationTime(file.FullName);
                            ffd.Name = file.Name;
                            ffd.File = file;
                            filesFolders.Add(ffd);
                        }
                    }
                }

                mainWindow.FoldersFiles.ItemsSource = filesFolders;
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                Cursor.Current = Cursors.Default;
            }
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
            bool bResult = false;
            

            if ((folder.Attributes & FileAttributes.System) == (FileAttributes.System))
            {
                bResult = false;
            }
            else
            {
                //Check if there is media in the folder or sub folder, otherwise no need to show it.
                DirectoryInfo[] folders = folder.GetDirectories();
                bool bFolders = false;

                if (folders.Count() > 0)
                {
                    bFolders = true;
                }

                //No need to check for files, if there are folders
                FileInfo[] Files = folder.GetFiles();

                foreach (FileInfo file in Files)
                {
                    if (acceptableMediaAudioTypes.IndexOf(file.Extension.ToLower()) > -1)
                    {
                        bResult = true;
                    }

                    if (acceptableMediaPhotoTypes.IndexOf(file.Extension.ToLower()) > -1)
                    {
                        bResult = true;
                    }

                    if (acceptableMediaVideoTypes.IndexOf(file.Extension.ToLower()) > -1)
                    {
                        bResult = true;
                    }

                    if (bResult == true)
                    {
                        break;
                    }
                }

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

                if (osInfo.Version.Major > 9)
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

        private void LoadScene(LoungeMediaFrame mediaFrame)
        {
            try
            {
                byte bPlayerCount = 0;
                double dHeight = 400;
                double dWidth = 400;
                Point startPoint = new Point(0, 0);
                Point endPoint = new Point(0, 0);
                List<byte> playerCount = new List<byte>();


                if (IsPortrait(mediaFrame))
                {
                    //Portrait mode can have 2 or 3 media players
                    //playerCount.Add(2);
                    playerCount.Add(3);
                }
                else
                {
                    //playerCount.Add(1);
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
                    mediaPlayer.startPoint = startPoint;
                    mediaPlayer.endPoint = endPoint;

                    int i = loungeRandom.Next(0, VideoFiles.Count);
                    FileInfo media = VideoFiles[i];

                    
                    mediaPlayer.Width = dWidth;
                    mediaPlayer.Height = dHeight;
                    mediaPlayer.LoungeMediaElement.Height = dHeight;
                    mediaPlayer.LoungeMediaElement.Width = dWidth;

                    mediaFrame.Medias.Children.Add(mediaPlayer);
                    Canvas.SetTop(mediaPlayer, startPoint.Y);
                    Canvas.SetLeft(mediaPlayer, startPoint.X);

                    mediaPlayer.LoungeMediaElement.Source = new Uri(media.FullName);
                    mediaPlayer.LoungeMediaElement.Play();
                    mediaPlayer.LoungeMediaElement.MediaOpened += LoungeMediaElement_MediaOpened;
                }
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
                System.Threading.Thread.Sleep(100); //To insure that the media has time to load and play before moving the player into position

                var mediaElement = (MediaElement)sender;
                var canvas = (Canvas)mediaElement.Parent;
                var lmp = (LoungeMediaPlayer)canvas.Parent;
                int iMilliseconds = loungeRandom.Next(2000, 4000); //So that the players don't move in at the same time
                

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

                storyboard.Begin();
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
                        LoungeMediaFrame loungeMediaFrame = new LoungeMediaFrame();
                        loungeMediaFrame.Name = "Display" + scr.DeviceName.Replace('\\', '_').Replace('.', 'A');


                        loungeMediaFrame.Left = workingArea.Left;
                        loungeMediaFrame.Top = workingArea.Top;
                        loungeMediaFrame.Width = workingArea.Width;
                        loungeMediaFrame.Height = workingArea.Height;
                        
                        loungeMediaFrame.Show();
                        mediaFrames.Add(loungeMediaFrame);
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void Dispose()
        {
            try
            {
                foreach(LoungeMediaFrame mediaFrame in mediaFrames)
                {
                    mediaFrame.Close();
                }

                mediaFrames.Clear();
            }
            catch 
            { }
        }

        public void SetVisualization(List<byte> visualData)
        {
            try
            {
                foreach(LoungeMediaFrame lmf in mediaFrames)
                {

                }
            }
            catch (Exception ex)
            {
                logException(ex);
            }
        }

        public void SetupVisualization()
        {
            try
            {
                foreach (LoungeMediaFrame lmf in mediaFrames)
                {
                    lmf.Visualizations.Children.Clear();

                    //TODO: Based on current visualization, setup the vis canvas.

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

    }

    public class AudioDeviceInfo
    {
        public int DeviceID { get; set; }
        public string Name { get; set; }
    }

    internal class Analyzer
    {
        private bool _enable;               //enabled status
        private DispatcherTimer _t;         //timer that refreshes the display
        private byte mbTimerTime = 25;
        public float[] _fft;               //buffer for fft data
        private WASAPIPROC _process;        //callback function to obtain data
        private int _lastlevel;             //last output level
        private int _hanctr;                //last output level counter
        public List<byte> visualizationData = new List<byte>();   //spectrum data buffer
        private List<AudioDeviceInfo> AudioDevices = new List<AudioDeviceInfo>();     //NEW non-UI device list
        private bool _initialized;          //initialized flag
        private int devindex;               //used device index
        private MainWindow mainWindow = null;
        private int _lines = 64;            // number of spectrum lines
        private LoungeEngine loungeEngine;

        public Analyzer(LoungeEngine loungeEngine)
        {  
            try
            {
                //mainWindow = Window;
                this.loungeEngine = loungeEngine;

                _fft = new float[8192];
                _lastlevel = 0;
                _hanctr = 0;
                _t = new DispatcherTimer();
                _t.Tick += Tick;
                _t.Interval = TimeSpan.FromMilliseconds(mbTimerTime); 
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
                        devindex = AudioDevices[mainWindow.audioDevices.SelectedIndex].DeviceID;

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
                    BassWasapi.BASS_WASAPI_Start();
                }
                else BassWasapi.BASS_WASAPI_Stop(true);
                System.Threading.Thread.Sleep(500);
                _t.IsEnabled = value;
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

                    //TO SET THE SPEAKER OUTPUT
                    //TODO: save setting and get it here
                    //if (device.name.ToLower().IndexOf("speaker") > -1)
                    //{
                    //    mainWindow.audioDevices.SelectedIndex = (mainWindow.audioDevices.Items.Count - 1);
                    //}
                }
            }
            
            Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_UPDATETHREADS, false);
            result = Bass.BASS_Init(0, 44100, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero);
            if (!result) throw new Exception("Init Error");
        }

        //timer 
        private void Tick(object sender, EventArgs e)
        {
            try
            {
                int ret = BassWasapi.BASS_WASAPI_GetData(_fft, (int)BASSData.BASS_DATA_FFT8192);  //get ch.annel fft data
                if (ret < -1) return;
                int x, y;
                int b0 = 0;

                //computes the spectrum data, the code is taken from a bass_wasapi sample.
                for (x = 0; x < _lines; x++)
                {
                    float peak = 0;
                    int b1 = (int)Math.Pow(2, x * 10.0 / (_lines - 1));
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
                loungeEngine.SetVisualization(visualizationData);

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
            catch (Exception)
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
