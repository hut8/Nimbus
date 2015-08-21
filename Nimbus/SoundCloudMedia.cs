using HtmlAgilityPack;
using System.IO;
using System.Net.Http;
using System.Linq;
using System.Text.RegularExpressions;

namespace Nimbus
{
    public class SoundCloudMedia
    {
        public string URL { get; private set; }

        private static readonly Regex _clientIdRegex = new Regex(@"[^_]client_id: ?""(?<client_id>\w+)""");

        public SoundCloudMedia(string url)
        {
            URL = url;
        }

        public async void Download(Stream destination)
        {
            HttpClient client = new HttpClient();
            client.MaxResponseContentBufferSize = 1024 * 1024 * 5;
            client.DefaultRequestHeaders.Add("user-agent", "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.2; WOW64; Trident/6.0)");

            // Download the URL given
            var html = await client.GetStringAsync(URL);

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
            var appJs = await client.GetStringAsync(appJsUrl);
            string clientId = _clientIdRegex
                .Matches(appJs)
                .Cast<Match>()
                .Select(x => x.Groups["client_id"].Value)
                .Distinct()
                .Single(); // Make sure they are all equal

        }
    }
}
