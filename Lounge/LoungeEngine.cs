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
        //FileFolderData fileFolderData = new FileFolderData();
        ObservableCollection<FileFolderData> filesFolders = new ObservableCollection<FileFolderData>();
        #endregion

        public LoungeEngine(MainWindow window)
        {
            try
            {
                mainWindow = window;
                applicationName = AppName();  //This is because of the need for this value in some functions that are threaded

                //mainWindow.DataContext = FileFolderData;

                ListFiles(null);
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

                if (directory == null)
                {
                    DriveInfo drv;
                    FileFolderData ffd;
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

                            filesFolders.Add(ffd);
                        }
                    }
                }
                else
                {
                    //TODO:
                }

                mainWindow.FoldersFiles.ItemsSource = filesFolders;
            }
            catch (Exception)
            {
                throw;
            }
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
