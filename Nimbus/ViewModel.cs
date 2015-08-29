
using PropertyChanged;
using System;
namespace Nimbus
{
    [ImplementPropertyChanged]
    public class ViewModel
    {
        public string Uri { get; set; }
        public string DestinationDirectory { get; set; }
        public string Title { get; set; }
        public bool IsProcessing { get; set; }
        public bool IsUriValid
        {
            get
            {
                return SoundCloudMedia.ValidateUri(Uri);
            }
        }
        public bool DownloadEnabled
        {
            get
            {
                return IsUriValid && !IsProcessing;
            }
        }
    }
}
