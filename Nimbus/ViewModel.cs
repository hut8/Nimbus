
using PropertyChanged;
using System;
using System.ComponentModel;

namespace Nimbus
{
    [ImplementPropertyChanged]
    public class ViewModel
    {
        public ViewModel()
        {
            Title = "Nimbus";
            TotalSize = long.MaxValue;
            ProcessState = Nimbus.MediaProcessState.Idle;
        }

        protected MediaDispatcher _dispatcher;

        public string LogEntries { get; set; }
        public string Uri { get; set; }
        public string DestinationDirectory { get; set; }
        public string Title { get; set; }
        public MediaProcessState ProcessState { get; set; }
        public bool IsRunning
        {
            get
            {
                return ProcessState != MediaProcessState.Idle;
            }
        }
        public long TotalSize { get; set; }
        public long DownloadedSize { get; set; }
        public bool IsUriValid
        {
            get
            {
                return MediaDispatcher.Instance.IsAcceptable(Uri);
            }
        }

        public bool DownloadEnabled
        {
            get
            {
                return IsUriValid &&
                    (ProcessState == MediaProcessState.Idle || ProcessState == MediaProcessState.Complete);
            }
        }

        public string MediaStateDescription
        {
            get
            {
                return EnumUtil.GetDescription<MediaProcessState>(ProcessState);
            }
        }

    }

    public enum MediaProcessState
    {
        Idle,
        [Description("Fetching Metadata...")]
        FetchingMetadata,
        [Description("Downloading...")]
        Downloading,
        Complete
    }
}
