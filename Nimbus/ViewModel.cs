
using PropertyChanged;
using System;
namespace Nimbus
{
    [ImplementPropertyChanged]
    public class ViewModel
    {
        public string Uri { get; set; }
        public string DestinationDirectory { get; set; }
        public bool IsProcessing { get; set; }
        public bool IsUriValid
        {
            get
            {
                return SoundCloudMedia.ValidateUri(Uri);
            }
        }
    }
}
