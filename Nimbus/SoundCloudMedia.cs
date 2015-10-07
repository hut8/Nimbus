using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Windows;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Flurl.Http;
using Flurl;
using Flurl.Util;

namespace Nimbus
{
    public class SoundCloudMedia : Media
    {
        //protected HttpClient _httpClient;

        protected string _trackId;
        protected bool _discovered;
        protected string _songDataURL;
        protected enum SoundCloudMediaType { MP3, Playlist }
        protected SoundCloudMediaType _mediaType;

        private static readonly string _naughtyCharacterPattern = @"[^\w\.& -]";
        private static readonly Regex _clientIdRegex = new Regex(@"[^_]client_id: ?""(?<client_id>\w+)""");
        private static readonly Regex _trackIdRegex = new Regex(@",""id"":(?<track_id>\d+)");
        private static readonly string _streamInfoUrlFormat = @"https://api.soundcloud.com/i1/tracks/{0}/streams?client_id={1}";

        public SoundCloudMedia(Uri url)
        {
            URL = url;

            CancelDownloadToken = new CancellationToken();
            _discovered = false;
        }

        public string DownloadPath
        {
            get
            {
                string cleanTitle = Regex.Replace(Title, _naughtyCharacterPattern, "", RegexOptions.None);
                return Path.Combine(DownloadDirectory, string.Format("{0}.mp3", cleanTitle));
            }
        }

        protected dynamic _trackData;
        protected async Task<dynamic> _fetchTrackData()
        {
            // Some information about the track is available here:
            // https://api-v2.soundcloud.com/tracks?urns=soundcloud%3Atracks%3A193781466&client_id=02gUJC0hH2ct1EGOcYXQIzRFU91c72Ea&app_version=810b564
            // It looks like the client_id is optional and the app_version is too, but the one provided gives more data
            string trackDataUrl = string.Format("https://api-v2.soundcloud.com/tracks?urns=soundcloud%3Atracks%3A{0}&app_version=810b564", _trackId);
            string trackJson = await _client
                .WithUrl(new Url(trackDataUrl))
                .GetStringAsync();
            JArray tracksData = JArray.Parse(trackJson);
            return tracksData.Single();
        }

        protected dynamic TrackData
        {
            get
            {
                if (_trackData == null)
                {
                    _trackData = Task.Run(() => _fetchTrackData()).Result;
                    OnTitleChange((string)_trackData.title);
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

        protected async Task DiscoverData()
        {
            OnStateChange(MediaProcessState.FetchingMetadata);
            // Download the URL given
            var html = await _client
                .WithUrl(URL.ToString())
                .GetStringAsync();

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
            var appJs = await _client
                .WithUrl(new Url(appJsUrl))
                .GetStringAsync();
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

            // Find URL of MP3 or the stupid playlist thing
            // https://api.soundcloud.com/i1/tracks/102140448/streams?client_id=02gUJC0hH2ct1EGOcYXQIzRFU91c72Ea
            string streamInfoURL = string.Format(_streamInfoUrlFormat, _trackId, clientId);
            string streamInfoJSON = await _client
                .WithUrl(streamInfoURL)
                .GetStringAsync();
            var urlMap = new { http_mp3_128_url = "", hls_mp3_128_url = "" };
            urlMap = JsonConvert.DeserializeAnonymousType(streamInfoJSON, urlMap);

            if (!string.IsNullOrWhiteSpace(urlMap.http_mp3_128_url))
            {
                _songDataURL = urlMap.http_mp3_128_url;
                _mediaType = SoundCloudMediaType.MP3;
            }
            else if (!string.IsNullOrWhiteSpace(urlMap.hls_mp3_128_url))
            {
                _songDataURL = urlMap.hls_mp3_128_url;
                _mediaType = SoundCloudMediaType.Playlist;
            }
            else
            {
                Clipboard.SetText(streamInfoJSON);
                throw new InvalidDataException(
                    "Could not find the HTTP MP3/Playlist URL. JSON dump was copied to clipboard.");
            }

            _discovered = true;
            OnStateChange(MediaProcessState.Idle);
        }

        /// <summary>
        /// This is some of the worst code I've ever written
        /// </summary>
        /// <returns></returns>
        protected async Task DownloadPlaylistToMP3()
        {
            await Task.Run(async () =>
            {
                string playlistString = await _client
                    .WithUrl(_songDataURL)
                    .GetStringAsync();
                var urlList = playlistString
                    .Split('\n')
                    .Where(line => !(line.StartsWith("#") || string.IsNullOrWhiteSpace(line)))
                    .ToList();
                MemoryStream buffer = new MemoryStream(1024 * 1024 * 5);
                foreach (var uri in urlList)
                {
                    OnTitleChange(string.Format("Part {0} of {1}: {2}",
                        urlList.FindIndex(x => x == uri) + 1,
                        urlList.Count,
                        Title));
                    byte[] fragment = await _client.WithUrl(uri).GetBytesAsync();
                    buffer.Write(fragment, 0, fragment.Length);
                }
                OnTitleChange(Title);
                File.WriteAllBytes(DownloadPath, buffer.ToArray());
            });
        }

        protected async Task DownloadDirectMP3()
        {
            Stream s = await _client
                .WithUrl(_songDataURL)
                .GetStreamAsync();
            using (var outStream = File.OpenWrite(DownloadPath))
            {
                s.CopyTo(outStream);
            }
        }

        protected async Task DownloadMP3()
        {
            switch (_mediaType)
            {
                case SoundCloudMediaType.MP3:
                    await DownloadDirectMP3();
                    break;
                case SoundCloudMediaType.Playlist:
                    await DownloadPlaylistToMP3();
                    break;
            }
        }

        public override async Task Download()
        {
            try
            {
                if (!_discovered) { await DiscoverData(); }

                if (!Directory.Exists(DownloadDirectory))
                {
                    Directory.CreateDirectory(DownloadDirectory);
                }
                if (File.Exists(DownloadPath))
                {
                    throw new IOException("File already exists");
                }

                // Save the song locally
                OnStateChange(MediaProcessState.Downloading);
                await DownloadMP3();
                OnStateChange(MediaProcessState.Complete);
            }
            catch (Exception)
            {
                OnStateChange(MediaProcessState.Idle);
                throw;
            }
        }


        // Make sure the URL looks like this:
        // https://soundcloud.com/majorlazer/major-lazer-dj-snake-lean-on-feat-mo
        public static bool CanAcceptUri(Uri uri)
        {
            if (!(uri.Host == "soundcloud.com" || uri.Host == "www.soundcloud.com"))
            {
                return false;
            }

            return true;
        }
    }
}
