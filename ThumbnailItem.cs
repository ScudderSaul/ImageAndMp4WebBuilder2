using System.IO;

namespace ImageAndMp4WebBuilder
{
    public class ThumbnailItem
    {
        public string OriginalPath { get; set; } = string.Empty;
        public string ThumbnailPath { get; set; } = string.Empty;
        public bool IsVideo { get; set; }
        public string DisplayName => Path.GetFileName(OriginalPath);
    }
}
