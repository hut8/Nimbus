using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Newtonsoft.Json;
using System.IO;

namespace Nimbus
{
    public class InstagramMedia : Media
    {
        protected string _username;
        protected string _fullName;
        protected Int32 _mediaCount;

        public InstagramMedia(Uri uri) : base()
        {
            URL = uri;
            CancelDownloadToken = new CancellationToken();
        }

        public override async Task Download()
        {
            _webClient.DownloadProgressChanged += DownloadProgressChange;
            string html = _webClient.DownloadString(URL);
            var sharedData = ExtractSharedDataJSON(html);
            var user = sharedData.entry_data.ProfilePage[0].user;
            _username = user.username;
            _fullName = user.full_name;
            var media = user.media;
            _mediaCount = media.count;
            // page_info
            // nodes
            // count
            await Task.Run(() =>
            {
                ApplyNodes(media.nodes);
            });
        }

        protected void ApplyNodes(dynamic nodes)
        {
            IEnumerable<dynamic> media = (IEnumerable<dynamic>)nodes;
            var imageCount = media.Count();

            foreach (var element in media.Select((n, i) => new { Node = n, Index = i  }))
            {
                String uriStr = element.Node.display_src;
                var uri = new Uri(uriStr);
                OnTitleChange(string.Format("Downloading image {0} out of {1} ({2})",
                    element.Index + 1,
                    imageCount,
                    uri.Segments.Last()));
                DownloadFile(uri);
            }
        }

        protected void DownloadFile(Uri uri)
        {
            string destDir = Path.Combine(DownloadDirectory, _username);
            if (!Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
            }

            string filename = uri.Segments.Last();
            string dest = Path.Combine(destDir, filename);

            if (File.Exists(dest))
            {
                return;
            }

            _webClient.DownloadFile(uri, dest);
        }

        protected dynamic ExtractSharedDataJSON(string html)
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);
            string json = doc.DocumentNode
                .SelectNodes("//script[not(@src)]")
                .Single(x => x.InnerText.Contains("window._sharedData"))
                .InnerText;
            // FIXME Kind of ugly...
            json = json.Replace("window._sharedData = ", string.Empty).TrimEnd(';');
            return JsonConvert.DeserializeObject(json);
        }

        public static bool CanAcceptUri(Uri uri)
        {
            if (!(uri.Host == "instagram.com" || uri.Host == "www.instagram.com"))
            {
                return false;
            }

            return true;
        }
    }
}
