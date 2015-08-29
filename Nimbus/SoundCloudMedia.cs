using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Nimbus
{
    public class SoundCloudMedia
    {
        protected HttpClient _httpClient;
        public event Action<bool> ProcessStateChange;
        public event Action<string> TitleChange;

        protected string _downloadDirectory;
        protected string _trackId;
        protected bool _discovered;
        protected string _songDataURL;

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
        public string URL { get; private set; }
        public CancellationToken CancelDownloadToken { get; private set; }


        public static string DefaultDownloadDirectory
        {
            get
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "SoundCloud");
            }
        }

        private static readonly Regex _clientIdRegex = new Regex(@"[^_]client_id: ?""(?<client_id>\w+)""");
        private static readonly Regex _trackIdRegex = new Regex(@",""id"":(?<track_id>\d+)");
        private static readonly string _streamInfoUrlFormat = @"https://api.soundcloud.com/i1/tracks/{0}/streams?client_id={1}";

        // Make sure the URL looks like this:
        // https://soundcloud.com/majorlazer/major-lazer-dj-snake-lean-on-feat-mo
        public static bool ValidateUri(string uriString)
        {
            if (string.IsNullOrWhiteSpace(uriString))
            {
                return false;
            }
            Uri songUri = null;
            try
            {
               songUri  = new Uri(uriString);
            }
            catch (UriFormatException)
            {
                return false;
            }

            if (!(songUri.Host == "soundcloud.com" || songUri.Host == "www.soundcloud.com"))
            {
                return false;
            }
            
            return true;
        }

        public SoundCloudMedia(string url)
            : this(url, DefaultDownloadDirectory)
        { }

        public SoundCloudMedia(string url, string downloadBase)
        {
            URL = url;

            DownloadDirectory = downloadBase;
            CancelDownloadToken = new CancellationToken();
            _discovered = false;

            _httpClient = new HttpClient();
            _httpClient.MaxResponseContentBufferSize = 1024 * 1024 * 5;
            _httpClient.DefaultRequestHeaders.Add("user-agent", "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.2; WOW64; Trident/6.0)");
        }

        public string DownloadPath
        {
            get
            {
                return Path.Combine(DownloadDirectory, string.Format("{0}.mp3", Title));
            }
        }

        protected dynamic _trackData;
        protected dynamic _fetchTrackData()
        {
            // Some information about the track is available here:
            // https://api-v2.soundcloud.com/tracks?urns=soundcloud%3Atracks%3A193781466&client_id=02gUJC0hH2ct1EGOcYXQIzRFU91c72Ea&app_version=810b564
            // It looks like the client_id is optional and the app_version is too, but the one provided gives more data
            string trackDataUrl = string.Format("https://api-v2.soundcloud.com/tracks?urns=soundcloud%3Atracks%3A{0}&app_version=810b564", _trackId);
            string trackJson = _httpClient.GetStringAsync(trackDataUrl).Result;
            JArray tracksData = JArray.Parse(trackJson);
            return tracksData.Single();
        }

        protected dynamic TrackData
        {
            get
            {
                if (_trackData == null)
                {
                    _trackData = _fetchTrackData();
                    TitleChange((string)TrackData.title);
                }
                return _trackData;
            }
        }

        public string Title
        {
            get
            {
                return (string)TrackData.title;
            }
        }

        public async Task DiscoverData()
        {
            ProcessStateChange(true);
            TitleChange("Fetching song metadata...");
            // Download the URL given
            var html = await _httpClient.GetStringAsync(URL);

            // Extract the URL from a script tag that looks like this: https://a-v2.sndcdn.com/assets/app-009bd-1ba53b3.js
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);
            string appJsUrl = doc.DocumentNode.SelectNodes("//script[@src]")
                .Select(node => node.Attributes["src"].Value)
                .Where(src => src.Contains("app-"))
                .Distinct()
                .Single();

            // Download the JS file containing the client_id
            // client_id:"02gUJC0hH2ct1EGOcYXQIzRFU91c72Ea"
            var appJs = await _httpClient.GetStringAsync(appJsUrl);
            string clientId = _clientIdRegex
                .Matches(appJs)
                .Cast<Match>()
                .Select(x => x.Groups["client_id"].Value)
                .Distinct()
                .Single(); // Make sure they are all equal

            // Find track_id
            // Look in script tags for JSON
            _trackId = doc.DocumentNode
                .SelectNodes("//script[not(@src)]")
                .Select(node => node.InnerText) // strings containing JavaScript
                .Select(js => _trackIdRegex.Match(js)) // Regex.Match
                .Select(match => match.Groups["track_id"].Value) // strings of track IDs
                .Where(x => !string.IsNullOrEmpty(x))
                .Distinct()
                .Single();

            // Find URL of MP3
            // https://api.soundcloud.com/i1/tracks/102140448/streams?client_id=02gUJC0hH2ct1EGOcYXQIzRFU91c72Ea
            string streamInfoURL = string.Format(_streamInfoUrlFormat, _trackId, clientId);
            string streamInfoJSON = await _httpClient.GetStringAsync(streamInfoURL);
            dynamic streamInfo = JsonConvert.DeserializeObject(streamInfoJSON);
            _songDataURL = streamInfo.http_mp3_128_url;

            _discovered = true;
            ProcessStateChange(false);
        }

        public async Task Download()
        {
            if (!Directory.Exists(DownloadDirectory))
            {
                Directory.CreateDirectory(DownloadDirectory);
            }
            using (var destination = File.OpenWrite(DownloadPath))
            {
                await Download(destination);
            }
        }

        public async Task Download(Stream destination)
        {
            ProcessStateChange(true);
            if (!_discovered) { await DiscoverData(); }
            // Save the song locally
            Stream songSource = await _httpClient.GetStreamAsync(_songDataURL);
            await songSource.CopyToAsync(destination, 1024 * 1024, CancelDownloadToken);
            ProcessStateChange(false);
        }
    }
}
