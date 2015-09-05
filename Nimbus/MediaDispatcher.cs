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
                    x => new SoundCloudMedia(x)));
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

    public class MediaFactory
    {
        private Func<Uri, bool> _predicate;
        private Func<Uri, Media> _constructor;

        public MediaFactory(Func<Uri, bool> predicate, Func<Uri, Media> constructor)
        {
            _predicate = predicate;
            _constructor = constructor;
        }

        public bool CanAccept(Uri uri)
        {
            return _predicate(uri);
        }

        public Media Construct(Uri uri)
        {
            return _constructor(uri);
        }
    }
}
