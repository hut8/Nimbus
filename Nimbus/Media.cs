using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Nimbus
{
    public abstract class Media
    {
        protected WebClient _webClient;
        protected string _downloadDirectory;
        
        protected const string UserAgent = "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.2; WOW64; Trident/6.0)";

        public event Action<TrackState> StateChange;
        public event Action<string> TitleChange;
        public string DownloadDirectory
        {
            get
            {
                return _downloadDirectory ?? DefaultDownloadDirectory;
            }
            set
            {
                _downloadDirectory = value;
            }
        }
        public Uri URL { get; protected set; }
        public CancellationToken CancelDownloadToken { get; protected set; }
        public DownloadProgressChangedEventHandler DownloadProgressChange { get; set; }

        public Media()
        {
            _webClient = new WebClient();
            _webClient.Headers.Add("user-agent", UserAgent);
        }

        public abstract Task Download();

        public static string DefaultDownloadDirectory
        {
            get
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "SoundCloud");
            }
        }

        protected virtual void OnTitleChange(string title)
        {
            if (TitleChange != null)
            {
                TitleChange(title);
            }
        }

        protected virtual void OnStateChange(TrackState state)
        {
            if (StateChange != null)
            {
                StateChange(state);
            }
        }
    }
}
