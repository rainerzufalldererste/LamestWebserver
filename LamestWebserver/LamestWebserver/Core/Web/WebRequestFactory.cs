using LamestWebserver.Collections;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace LamestWebserver.Core.Web
{
    [Serializable]
    public class WebRequestFactory : NullCheckable
    {
        public AVLHashMap<string, string> Responses;
        public AVLHashMap<string, string> Redirects;
        public string UserAgentString = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit (KHTML, like Gecko)";
        public int Timeout = 20000;

        public WebRequestFactory(bool cacheResponses)
        {
            if(cacheResponses)
            {
                Responses = new AVLHashMap<string, string>();
                Redirects = new AVLHashMap<string, string>();
            }
        }

        public void FlushCache()
        {
            Responses.Clear();
            Redirects.Clear();
        }

        public string GetResponse(string URL, int maxRedirects = 10)
        {
            return GetResponse(URL, out HttpStatusCode statusCode, maxRedirects);
        }

        public string GetResponse(string URL, out HttpStatusCode statusCode, int maxRedirects = 10)
        {
            int redirects = 0;

            RESTART:

            if (redirects > maxRedirects)
            {
                statusCode = HttpStatusCode.BadGateway;
                return null;
            }

            if (Redirects && Redirects.ContainsKey(URL))
            {
                URL = Redirects[URL];
                redirects++;
                goto RESTART;
            }

            if (Responses && Responses.ContainsKey(URL))
            {
                statusCode = HttpStatusCode.NotModified;
                return Responses[URL];
            }

            WebRequest request = WebRequest.Create(URL);
            (request as HttpWebRequest).UserAgent = UserAgentString;
            (request as HttpWebRequest).Timeout = Timeout;
            (request as HttpWebRequest).AllowAutoRedirect = false;
            var response = request.GetResponse();
            string location = ((HttpWebResponse)response).GetResponseHeader("location");

            if (!string.IsNullOrEmpty(location) && location != URL)
            {
                if (Redirects)
                    Redirects.Add(URL, location);

                URL = location;
                redirects++;
                goto RESTART;
            }

            statusCode = ((HttpWebResponse)response).StatusCode;
            return new StreamReader(response.GetResponseStream()).ReadToEnd();
        }

        public static string GetResponseSimple(string URL, out HttpStatusCode statusCode)
        {
            RESTART:

            WebRequest request = WebRequest.Create(URL);
            (request as HttpWebRequest).UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit (KHTML, like Gecko)";
            (request as HttpWebRequest).Timeout = 20000;
            (request as HttpWebRequest).AllowAutoRedirect = false;
            var response = request.GetResponse();

            string location = ((HttpWebResponse)response).GetResponseHeader("location");

            if (!string.IsNullOrEmpty(location) && location != URL)
            {
                URL = location;
                goto RESTART;
            }

            statusCode = ((HttpWebResponse)response).StatusCode;
            return new StreamReader(response.GetResponseStream()).ReadToEnd();
        }
    }
}
