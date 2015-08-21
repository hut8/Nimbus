using System.IO;

namespace Nimbus
{
    public class SoundCloudMedia
    {
        public string URL { get; private set; }

        public SoundCloudMedia(string url)
        {
            URL = url;
        }

        public async void Download(Stream destination)
        {

        }
    }
}
