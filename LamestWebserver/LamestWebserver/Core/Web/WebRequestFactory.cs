using LamestWebserver.Collections;
using LamestWebserver.Synchronization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace LamestWebserver.Core.Web
{
    /// <summary>
    /// Provivides functionality for quickly generating a lot of similar WebRequests.
    /// </summary>
    [Serializable]
    public class WebRequestFactory : NullCheckable
    {
        /// <summary>
        /// The cached Responses of recent Requests.
        /// </summary>
        protected SynchronizedDictionary<string ,string> Responses;

        /// <summary>
        /// The cached Redirects of recent Redirects.
        /// </summary>
        protected SynchronizedDictionary<string, string> Redirects;

        /// <summary>
        /// The specified Cookies for the WebRequests.
        /// </summary>
        public CookieContainer Cookies;

        /// <summary>
        /// The UserAgent string to send in every WebRequest.
        /// </summary>
        public string UserAgentString = "LamestWebserver Webcrawler";

        /// <summary>
        /// The default Timeout for every request.
        /// </summary>
        public int Timeout = 2500;


        /// <summary>
        /// Creates a new WebRequestFactory instance.
        /// </summary>
        /// <param name="cacheResponses">Shall responses and redirects be cached?</param>
        public WebRequestFactory(bool cacheResponses = false)
        {
            if(cacheResponses)
            {
                Responses = new SynchronizedDictionary<string, string>(new AVLHashMap<string, string>());
                Redirects = new SynchronizedDictionary<string, string>(new AVLHashMap<string, string>());
            }
        }

        /// <summary>
        /// Clears the caches for Responses and Redirects if they were enabled.
        /// </summary>
        public virtual void FlushCache()
        {
            if(Responses)
                Responses.Clear();

            if (Redirects)
                Redirects.Clear();
        }

        /// <summary>
        /// Retrieves a Response as string.
        /// </summary>
        /// <param name="URL">The requested URL.</param>
        /// <param name="maxRedirects">The maximum amount of redirects before stop following.</param>
        /// <returns>Returns the response as string.</returns>
        public string GetResponse(string URL, int maxRedirects = 10)
        {
            HttpStatusCode statusCode;

            return GetResponse(URL, out statusCode, maxRedirects);
        }

        /// <summary>
        /// Retrieves a Response as string.
        /// </summary>
        /// <param name="URL">The requested URL.</param>
        /// <param name="statusCode">The status code of the Request.</param>
        /// <param name="maxRedirects">The maximum amount of redirects before stop following.</param>
        /// <returns>Returns the response as string.</returns>
        public virtual string GetResponse(string URL, out HttpStatusCode statusCode, int maxRedirects = 10)
        {
            if (URL == null)
                throw new ArgumentNullException(nameof(URL));

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
            (request as HttpWebRequest).CookieContainer = Cookies;
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

        /// <summary>
        /// Simply retrieves a response of the given URL.
        /// </summary>
        /// <param name="URL">The requested URL.</param>
        /// <param name="statusCode">The returned status code of the response.</param>
        /// <returns></returns>
        public static string GetResponseSimple(string URL, out HttpStatusCode statusCode)
        {
            if (URL == null)
                throw new ArgumentNullException(nameof(URL));

            RESTART:

            WebRequest request = WebRequest.Create(URL);
            (request as HttpWebRequest).UserAgent = "LamestWebserver Webcrawler";
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
