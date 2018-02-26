using System;
using System.IO;

namespace Lounge.Models
{
    class FileFolderData
    {
        public string Name { get; set; }

        public DateTime Date { get; set; }

        public bool Selected { get; set; }

        public string Icon { get; set; }

        public FileFolderType Type { get; set; }

        public FileInfo File { get; set; }

        public DirectoryInfo Folder { get; set; }

        public enum FileFolderType
        {
            Folder,
            File
        }

        public FileFolderData()
        {
            this.Name = "";
            this.Date = DateTime.Now;
            this.Selected = false;
            this.Icon = ""; //Temp
            this.Type = FileFolderType.File;
        }
    }
}
