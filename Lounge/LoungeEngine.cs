using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Lounge.Models;
using System.Collections.ObjectModel;

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
            catch (Exception)
            {
                throw;
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

}
