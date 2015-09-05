using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Nimbus
{
    public class InstagramMedia : Media
    {
        public InstagramMedia(Uri uri)
        {
            URL = uri;
            CancelDownloadToken = new CancellationToken();
        }

        public override async Task Download()
        {
            throw new NotImplementedException();
        }

        // Make sure the URL looks like this:
        // https://soundcloud.com/majorlazer/major-lazer-dj-snake-lean-on-feat-mo
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
