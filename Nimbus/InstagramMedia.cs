using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.Net.Http;
using Newtonsoft.Json;
using System.IO;
using Flurl.Http;

namespace Nimbus
{
    public class InstagramMedia : Media
    {
        protected string _username;
        protected string _userId;
        protected string _fullName;
        protected Int32 _mediaCount;
        protected const string _userAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/44.0.2403.157 Safari/537.36";
        
        public InstagramMedia(Uri uri)
            : base()
        {
            URL = uri;
        }

        public override async Task Download()
        {
            string html = await _client
                .WithUrl(new Flurl.Url(URL.ToString()))
                .GetStringAsync();
            var sharedData = ExtractSharedDataJSON(html);
            var user = sharedData.entry_data.ProfilePage[0].user;
            _username = user.username;
            _userId = user.id;
            _fullName = user.full_name;
            var media = user.media;
            _mediaCount = media.count;

            string csrftoken = _client.GetCookies()["csrftoken"].Value;
            _client.HttpClient.DefaultRequestHeaders.Referrer = URL;
            _client.HttpClient.DefaultRequestHeaders.Add("X-CSRFToken", csrftoken);
            _client.HttpClient.DefaultRequestHeaders.Add("X-Instagram-AJAX", "1");
            _client.HttpClient.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");

            // page_info
            await Task.Run(async () =>
            {
                do {
                    ApplyNodes(media.nodes);
                    string endCursor = media.page_info.end_cursor;
                    dynamic page = await MediaPage(endCursor);
                    media = page.media;
                } while (media.page_info.has_next_page);
            });
        }

        protected async Task<dynamic> MediaPage(string startCursor)
        {
            // POST to 
            // POST Params (urlencoded):
            var qFormat = @"ig_user(" + _userId + ") { media.after(" + startCursor + @", 12) {
              count,
              nodes {
                caption,
                code,
                comments {
                  count
                },
                date,
                dimensions {
                  height,
                  width
                },
                display_src,
                id,
                is_video,
                likes {
                  count
                },
                owner {
                  id
                },
                thumbnail_src
              },
              page_info
            }
             }";
            string q = qFormat;//string.Format(qFormat, _userId, startCursor);

            dynamic response = await _client
                .WithUrl(new Flurl.Url("https://instagram.com/query/"))
                .PostUrlEncodedAsync(new {
                     q = q,
                     @ref = "users::show"
                 })
                .ReceiveJson();
            return response;
        }

        protected void ApplyNodes(dynamic nodes)
        {
            IEnumerable<dynamic> media = (IEnumerable<dynamic>)nodes;
            var imageCount = media.Count();

            foreach (var element in media.Select((n, i) => new { Node = n, Index = i }))
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

            _client
                .WithUrl(new Flurl.Url(uri.ToString()))
                .DownloadFileAsync(destDir);
        }

        protected dynamic ExtractSharedDataJSON(string html)
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);
            string json = doc.DocumentNode
                .SelectNodes("//script[not(@src)]")
                .Single(x => x.InnerText.Contains("window._sharedData"))
                .InnerText;
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
