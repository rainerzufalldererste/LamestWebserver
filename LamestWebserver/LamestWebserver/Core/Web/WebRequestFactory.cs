using LamestWebserver.Collections;
using LamestWebserver.Serialization;
using LamestWebserver.Synchronization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
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
        protected SynchronizedDictionary<string, string, OutOfCoreHashmap<string, string>> Responses;

        /// <summary>
        /// The cached Redirects of recent Redirects.
        /// </summary>
        protected SynchronizedDictionary<string, string, AVLHashMap<string, string>> Redirects;

        /// <summary>
        /// The specified Cookies for the WebRequests.
        /// </summary>
        public CookieContainer Cookies;

        /// <summary>
        /// The UserAgent string to send in every WebRequest.
        /// </summary>
        public string UserAgentString = "LamestWebserver Webcrawler";

        /// <summary>
        /// The default Timeout in milliseconds for every request.
        /// </summary>
        public int Timeout = 2500;

        /// <summary>
        /// The maximum number of Retries per Request on Timeout.
        /// </summary>
        public int MaximumRetries = 2;

        /// <summary>
        /// The time to randomly wait after a completed request. (Item1 is MinimumTime, Item2 is MaximumTime)
        /// </summary>
        public Tuple<int, int> RandomWaitTimeMs
        {
            get
            {
                return _randomWaitTimeMs;
            }
            set
            {
                if (value == null || value.Item1 <= value.Item2)
                    _randomWaitTimeMs = value;
                else
                    _randomWaitTimeMs = new Tuple<int, int>(value.Item2, value.Item1);
            }
        }

        private Tuple<int, int> _randomWaitTimeMs = null;
        private DateTime _lastRequestTime = new DateTime(0);
        private Random _random = new Random();


        /// <summary>
        /// Creates a new WebRequestFactory instance.
        /// </summary>
        /// <param name="cacheResponses">Shall responses and redirects be cached?</param>
        public WebRequestFactory(bool cacheResponses = false)
        {
            if (cacheResponses)
            {
                Responses = new SynchronizedDictionary<string, string, OutOfCoreHashmap<string, string>>();
                Redirects = new SynchronizedDictionary<string, string, AVLHashMap<string, string>>();
            }
        }

        /// <summary>
        /// Clears the caches for Responses and Redirects if they were enabled.
        /// </summary>
        public virtual void FlushCache()
        {
            if (Responses)
                Responses.Clear();

            if (Redirects)
                Redirects.Clear();
        }

        /// <summary>
        /// Loads the cached responses and redirects from files.
        /// </summary>
        /// <param name="fileName">the filename. (will be suffixed with .Responses or .Redirects)</param>
        public void LoadCacheState(string fileName)
        {
            if (fileName == null)
                throw new ArgumentNullException(nameof(fileName));

            if (Responses && Responses.Count() > 0)
                Logger.LogWarning($"Loading cached {nameof(WebRequestFactory)} will override {Responses.Count} responses from the cache. If you want to keep them consider using {nameof(WebRequestFactory)}.{nameof(WebRequestFactory.AppendCacheState)}.");

            if (Redirects && Redirects.Count() > 0)
                Logger.LogWarning($"Loading cached {nameof(WebRequestFactory)} will override {Redirects.Count} redirects from the cache. If you want to keep them consider using {nameof(WebRequestFactory)}.{nameof(WebRequestFactory.AppendCacheState)}.");

            if (File.Exists(fileName + ".ooc.json"))
            {
                Responses = new SynchronizedDictionary<string, string, OutOfCoreHashmap<string, string>>(new OutOfCoreHashmap<string, string>(fileName + ".ooc.json"));
            }
            else if(File.Exists(fileName + "." + nameof(Responses)))
            {
                SynchronizedDictionary<string, string, AVLHashMap<string, string>> old = Serializer.ReadJsonData<SynchronizedDictionary<string, string, AVLHashMap<string, string>>>(fileName + "." + nameof(Responses));

                Responses = new SynchronizedDictionary<string, string, OutOfCoreHashmap<string, string>>(new OutOfCoreHashmap<string, string>(1024, fileName + ".ooc.json"));

                LamestWebserver.Core.Logger.LogInformation($"Importing {old.Count} responses for {nameof(WebRequestFactory)} from '{fileName}.{nameof(Responses)}'...");

                foreach (var kvpair in old)
                    Responses[kvpair.Key] = kvpair.Value;
            }
            else
            {
                Logger.LogExcept(new IOException($"File not found '{fileName}.ooc.json'."));
            }

            Redirects = Serializer.ReadJsonData<SynchronizedDictionary<string, string, AVLHashMap<string, string>>>(fileName + "." + nameof(Redirects));
        }

        /// <summary>
        /// Loads the cached responses and redirects from files and appends them to the current collection of responses and redirects.
        /// </summary>
        /// <param name="fileName">the filename. (will be suffixed with .Responses or .Redirects)</param>
        public void AppendCacheState(string fileName)
        {
            if (fileName == null)
                throw new ArgumentNullException(nameof(fileName));
            
            if (File.Exists(fileName + ".ooc.json"))
            {
                SynchronizedDictionary<string, string, OutOfCoreHashmap<string, string>> responses = new SynchronizedDictionary<string, string, OutOfCoreHashmap<string, string>>(new OutOfCoreHashmap<string, string>(fileName + ".ooc.json"));

                foreach (var key in responses.Keys)
                    Responses[key] = responses[key];
            }
            else if (File.Exists(fileName + "." + nameof(Responses)))
            {
                SynchronizedDictionary<string, string, AVLHashMap<string, string>> responses = Serializer.ReadJsonData<SynchronizedDictionary<string, string, AVLHashMap<string, string>>>(fileName + "." + nameof(Responses));

                LamestWebserver.Core.Logger.LogInformation($"Importing {responses.Count} responses for {nameof(WebRequestFactory)} from '{fileName}.{nameof(Responses)}'...");
                
                foreach (var response in responses)
                    Responses.Add(response);
            }
            else
            {
                Logger.LogExcept(new IOException($"File not found '{fileName}.ooc.json'."));
            }

            SynchronizedDictionary<string, string, AVLHashMap<string, string>> redirects = Serializer.ReadJsonData<SynchronizedDictionary<string, string, AVLHashMap<string, string>>>(fileName + "." + nameof(Redirects));

            foreach (var redirect in redirects)
                Redirects.Add(redirect);
        }

        /// <summary>
        /// Saves the cached responses and redirects to files.
        /// </summary>
        /// <param name="fileName">the filename. (will be suffixed with .Responses or .Redirects)</param>
        public void SaveCacheState(string fileName)
        {
            if (!Redirects || !Responses)
                Logger.LogExcept(new InvalidOperationException($"Trying to serialize cache of uncached {nameof(WebRequestFactory)}."));

            if (fileName == null)
                throw new ArgumentNullException(nameof(fileName));
            
            Serializer.WriteJsonData(Redirects, fileName + "." + nameof(Redirects));
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

            if(RandomWaitTimeMs != null)
            {
                int randomWaitTime = _random.Next(RandomWaitTimeMs.Item1, RandomWaitTimeMs.Item2);
                DateTime _minRequestTime = DateTime.UtcNow - TimeSpan.FromMilliseconds(randomWaitTime);
                
                if (_minRequestTime > _lastRequestTime)
                    Thread.Sleep(System.Math.Max(1, (int)System.Math.Ceiling((_lastRequestTime - _minRequestTime).TotalMilliseconds)));

                _lastRequestTime = DateTime.UtcNow;
            }

            int redirects = 0;
            int retries = 0;

            RESTART:

            try
            {
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
                string responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();

                if (Responses)
                    Responses.Add(URL, responseString);

                return responseString;
            }
            catch(WebException e)
            {
                if (e.Status == WebExceptionStatus.Timeout)
                {
                    retries++;

                    if (retries > MaximumRetries)
                        Logger.LogExcept(e);

                    goto RESTART;
                }
                else
                {
                    statusCode = HttpStatusCode.ExpectationFailed;

                    Logger.LogExcept(e);
                }
            }

            Logger.LogExcept(new InvalidOperationException("This is Dead Code. You are not supposed to get here. Please report this bug."));

            statusCode = HttpStatusCode.ExpectationFailed;
            return null;
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
