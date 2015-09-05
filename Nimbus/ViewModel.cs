
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
            TrackState = Nimbus.TrackState.Idle;
        }

        protected MediaDispatcher _dispatcher;

        public string Uri { get; set; }
        public string DestinationDirectory { get; set; }
        public string Title { get; set; }
        public TrackState TrackState { get; set; }
        public bool IsFetchingMetadata
        {
            get
            {
                return TrackState == TrackState.FetchingMetadata;
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
                    (TrackState == TrackState.Idle || TrackState == TrackState.Complete);
            }
        }

        public string TrackStateDescription
        {
            get
            {
                return EnumUtil.GetDescription<TrackState>(TrackState);
            }
        }

    }

    public enum TrackState
    {
        Idle,
        [Description("Fetching Metadata...")]
        FetchingMetadata,
        [Description("Downloading...")]
        Downloading,
        Complete
    }
}
