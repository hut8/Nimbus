using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nimbus
{
    /// <summary>
    /// Dispatches request to a Media object given a URI
    /// </summary>
    public class MediaDispatcher
    {
        private List<MediaFactory> _factories;

#region Singleton Crap
        private static MediaDispatcher _instance;

        public static MediaDispatcher Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new MediaDispatcher();
                }
                return _instance;
            }
        }
#endregion

        private MediaDispatcher()
        {
            _factories = new List<MediaFactory>();
            _factories.Add(
                new MediaFactory(
                    x => SoundCloudMedia.CanAcceptUri(x),
                    x => new SoundCloudMedia(x),
                    "https://soundcloud.com/[username]/[track]"));
            _factories.Add(
                new MediaFactory(
                    x => InstagramMedia.CanAcceptUri(x),
                    x => new InstagramMedia(x),
                    "https://instagram.com/[username]/"));
        }

        public bool IsAcceptable(string uri)
        {
            if (!Uri.IsWellFormedUriString(uri, UriKind.Absolute))
            {
                return false;
            }
            return _factories.Any(x => x.CanAccept(new Uri(uri)));
        }

        public Media Dispatch(string uriString)
        {
            Uri uri = new Uri(uriString);
            return _factories
                    .Where(x => x.CanAccept(uri))
                    .Single()
                    .Construct(uri);
        }
    }
}
