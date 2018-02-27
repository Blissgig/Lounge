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

namespace Lounge
{
    class LoungeEngine
    {
        #region Private Members
        private MainWindow mainWindow;
        private List<FileInfo> AudioFiles = new List<FileInfo>();
        private List<FileInfo> VideoFiles = new List<FileInfo>();
        private List<FileInfo> PhotoFiles = new List<FileInfo>();

        private string applicationName = "Lounge";
        private string acceptableMediaTypes = "*.avi,*.asf,*.mp4,*.m4v,*.mpg,*.mpeg,*.mpeg2,*.mpeg4,*.wmv,*.3gp,*.mov,*.mts,*.divx,*.mp3,*.wma,*.wav,*.m4a,*.png,*.jpg,*.jpeg";

        ObservableCollection<FileFolderData> filesFolders = new ObservableCollection<FileFolderData>();

        private List<DirectoryInfo> breadcrumbs = new List<DirectoryInfo>();

        private Analyzer loungeAnalyzer;
        #endregion

        public LoungeEngine(MainWindow window)
        {
            try
            {
                mainWindow = window;

                SettingsLoad(); //Call before anything else runs.
                
                loungeAnalyzer = new Analyzer(window);

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

                if (IsWin10() == true)
                {
                    //Win10 now supports Flac and MKV media types
                    if (acceptableMediaTypes.Contains("*.flac,") == false)
                    {
                        acceptableMediaTypes += "*.flac,";
                    }

                    if (acceptableMediaTypes.Contains("*.mkv,") == false)
                    {
                        acceptableMediaTypes += "*.mkv,";
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

                switch (ffd.Type)
                {
                    case FileFolderData.FileFolderType.Folder:
                        breadcrumbs.Add(ffd.Folder); //This is used when the user selects "Back"
                        ListFiles(ffd.Folder);
                        break;

                    case FileFolderData.FileFolderType.File:
                        
                        ffd.Selected = !ffd.Selected;
                        //TODO: Add/remove from media lists
                        break;
                }
            }
            catch (Exception ex)
            {
                logException(ex);
            }
        }

        public void ListFiles(DirectoryInfo directory)
        {
            try
            {
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

                    foreach (DirectoryInfo folder in folders)
                    {
                        if (!IsSpecialFolder(folder))
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
                        if (file.Extension.IndexOf(acceptableMediaTypes) > -1)
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
        }

        private bool IsSpecialFolder(DirectoryInfo folder)
        {
            bool result = false;

            foreach (Environment.SpecialFolder suit in Enum.GetValues(typeof(Environment.SpecialFolder)))
            {
                if (folder.FullName == Environment.GetFolderPath(suit))
                {
                    result = true;
                    break;
                }
            }

            return result;
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

        private void LoadWindows()
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


                        loungeMediaFrame.Left = workingArea.Left;
                        loungeMediaFrame.Top = workingArea.Top;
                        loungeMediaFrame.Width = workingArea.Width;
                        loungeMediaFrame.Height = workingArea.Height;

                        loungeMediaFrame.Show();
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void logException(Exception ex)
        {
            try
            {
                //TODO: option to log exceptions, on by default
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
        public List<byte> BediaSpectrumData = new List<byte>();   //spectrum data buffer
        private List<AudioDeviceInfo> AudioDevices = new List<AudioDeviceInfo>();     //NEW non-UI device list
        private bool _initialized;          //initialized flag
        private int devindex;               //used device index
        private MainWindow mainWindow = null;
        private int _lines = 64;            // number of spectrum lines


        public Analyzer(MainWindow Window)
        {  
            try
            {
                mainWindow = Window;

                _fft = new float[8192];
                _lastlevel = 0;
                _hanctr = 0;
                _t = new DispatcherTimer();
                _t.Tick += _t_Tick;
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

        // initialization
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
                    audioDeviceInfo.DeviceID = i; //device.id  //TODO SHOULDN'T THIS BE THE RIGHT VALUE... AND YET IT WORKS
                    audioDeviceInfo.Name = device.name;
                    AudioDevices.Add(audioDeviceInfo);

                    mainWindow.audioDevices.Items.Add(device.name);

                    //TO SET THE SPEAKER OUTPUT
                    //TODO: save setting and get it here
                    if (device.name.ToLower().IndexOf("speaker") > -1)
                    {
                        mainWindow.audioDevices.SelectedIndex = (mainWindow.audioDevices.Items.Count - 1);
                    }
                }
            }
            
            Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_UPDATETHREADS, false);
            result = Bass.BASS_Init(0, 44100, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero);
            if (!result) throw new Exception("Init Error");
        }

        //timer 
        private void _t_Tick(object sender, EventArgs e)
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
                    BediaSpectrumData.Add((byte)y);
                }

                //TODO: SET VISUALIZATION HERE
               // mainWindow.SetVisualisation(BediaSpectrumData);


                BediaSpectrumData.Clear();

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
            BassWasapi.BASS_WASAPI_Free();
            Bass.BASS_Free();
        }
    }

}
