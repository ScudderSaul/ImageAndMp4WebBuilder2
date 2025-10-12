using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace ImageAndMp4WebBuilder
{
    public partial class ImagePreviewWindow : Window
    {
        public ImagePreviewWindow(string path)
        {
            InitializeComponent();
            Load(path);
        }

        private void Load(string path)
        {
            if (!File.Exists(path)) return;
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var decoder = BitmapDecoder.Create(fs, BitmapCreateOptions.IgnoreColorProfile, BitmapCacheOption.OnLoad);
            PreviewImage.Source = decoder.Frames[0];
        }
    }
}
