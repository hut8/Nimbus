using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Flurl.Http;

namespace Nimbus
{
    public abstract class Media
    {
        protected FlurlClient _client;
        protected string _downloadDirectory;
        
        protected const string UserAgent = "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.2; WOW64; Trident/6.0)";

        public event Action<MediaProcessState> StateChange;
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
            _client = new FlurlClient()
                .EnableCookies()
                .WithHeader("User-Agent", UserAgent);
        }

        public abstract Task Download();

        public static string DefaultDownloadDirectory
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "Nimbus Downloads");
            }
        }

        protected virtual void OnTitleChange(string title)
        {
            if (TitleChange != null)
            {
                TitleChange(title);
            }
        }

        protected virtual void OnStateChange(MediaProcessState state)
        {
            if (StateChange != null)
            {
                StateChange(state);
            }
        }
    }
}
