using System.IO;
using System.Windows.Controls;

namespace Lounge
{
    public partial class MediaItem : UserControl
    {
        private bool isSelected = false;

        public FileInfo File { get; set; }

        public DirectoryInfo Folder { get; set; }

        public bool Selected
        {
            get { return isSelected; }

            set
            {
                isSelected = value;
                 if (isSelected)
                {
                    this.SelectedIcon.Opacity = 1;
                }
                else
                {
                    this.SelectedIcon.Opacity = 0;
                }
            }
        }

        private LoungeEngine loungeEngine;

        public MediaItem()
        {
            InitializeComponent();
        }

        public MediaItem(LoungeEngine engine, FileInfo file, string title)
        {
            InitializeComponent();

            this.loungeEngine = engine;
            this.Title.Text = title;
            this.File = file;
        }

        public MediaItem(LoungeEngine engine, DirectoryInfo folder, string title)
        {
            InitializeComponent();

            this.loungeEngine = engine;
            this.Title.Text = title;
            this.Folder = folder;
        }

        private void MediaItem_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (this.Folder != null)
            {
                loungeEngine.ListFiles(this.Folder);
            }
            else
            {
                this.Selected = !this.Selected;
                loungeEngine.SelectMediaItem(this.File);
            }
        }
    }
}
