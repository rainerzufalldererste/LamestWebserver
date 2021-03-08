using System;
using System.Text;
using LamestWebserver.Caching;
using LamestWebserver.Core;
using LamestWebserver.RequestHandlers.DebugView;
using LamestWebserver.Synchronization;
using LamestWebserver.UI;
using LamestWebserver.Collections;
using System.Linq;
using System.Collections.Generic;

namespace LamestWebserver
{
    /// <summary>
    /// A Page referencable by URL
    /// </summary>
    public interface IURLIdentifyable
    {
        /// <summary>
        /// The URL of this Page
        /// </summary>
        string URL { get; }
    }

    /// <summary>
    /// Simplifies implementation of IURLIdentifyables.
    /// </summary>
    public abstract class ResponseCoreImplementation : IURLIdentifyable, IDebugRespondable, IDebugUpdateableResponse<Exception, TimeSpan, HttpRequest, HttpResponse>
    {
        /// <inheritdoc />
        public string URL { get; protected set; }

        private DebugContainerResponseNode _debugResponseNode;
        private FixedSizeQueue<Tuple<Exception, DateTime>> _exceptions = new FixedSizeQueue<Tuple<Exception, DateTime>>(20);
        private FixedSizeQueue<Tuple<TimeSpan, DateTime, string, string, string>> _responseTimes = new FixedSizeQueue<Tuple<TimeSpan, DateTime, string, string, string>>(20);
        private int _totalPageViews = 0;
        private double _averageResponseTime = 0;
        private TimeSpan _minimumResponseTime = TimeSpan.FromDays(365);
        private TimeSpan _maximumResponseTime = TimeSpan.FromMilliseconds(0);
        private int _totalExceptions = 0;
        private int _averageResponseSize = 0, _minimumResponseSize = int.MaxValue, _maximumResponseSize = int.MinValue;

        /// <summary>
        /// Creates a new ResponseCoreImplementation element.
        /// </summary>
        /// <param name="URL">The URL of the Response.</param>
        protected ResponseCoreImplementation(string URL)
        {
            if (URL == null)
                throw new ArgumentNullException(nameof(URL));

            this.URL = URL;
            _debugResponseNode = new DebugContainerResponseNode($"[{GetType().Name}] '{URL}'", null, GetDebugViewResponse, null, false);
        }

        /// <inheritdoc />
        DebugResponseNode IDebugRespondable.GetDebugResponseNode() => _debugResponseNode;

        /// <summary>
        /// The response for the DebugView for this Response.
        /// </summary>
        /// <param name="sessionData">The current SessionData.</param>
        /// <returns>Returns a HElement containing the Response.</returns>
        protected virtual HElement GetDebugViewResponse(SessionData sessionData)
        {
            HMultipleElements ret = new HMultipleElements();

            ret += new HText($"This page has been called {_totalPageViews} times.");

            if (_totalPageViews > 0)
            {
                ret += new HText($"The average response time of this page is {_averageResponseTime} milliseconds. (maximum: {_maximumResponseTime} | minimum {_minimumResponseTime})");

                ret += new HHeadline("Last Response Times", 2);
                ret += new HTable((from r in _responseTimes select r.ToEnumerable())) { TableHeader = new List<HElement>() { "Response Time", "Time", "IP Address", "HTTP Head Variables", "HTTP Post Variables" } };
                ret += new HNewLine();
            }

            if (_totalExceptions > 0)
            {
                ret += new HText($"This page has thrown an exception {_totalExceptions} times.");

                ret += new HHeadline("Last Exceptions", 2);
                ret += new HTable((from e in _exceptions select e.ToEnumerable())) { TableHeader = new List<HElement>() { "Exception", "Time" } };
                ret += new HNewLine();
            }

            return ret;
        }

        /// <inheritdoc />
        public void UpdateDebugResponseData(Exception exception, TimeSpan timeSpan, HttpRequest request, HttpResponse response)
        {
            _totalPageViews++;

            if (exception != null)
            {
                _exceptions.Push(new Tuple<Exception, DateTime>(exception, DateTime.Now));

                _totalExceptions++;
            }

            if (request != null)
            {
                string ip = "";
                string head = "";
                string post = "";

                if(request.Stream != null && request.Stream is System.Net.Sockets.NetworkStream) // not a nice solution...
                    ip = ((System.Net.Sockets.Socket)(typeof(System.Net.Sockets.NetworkStream).GetProperty("Socket", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)).GetValue((System.Net.Sockets.NetworkStream)request.Stream, null)).RemoteEndPoint.ToString();

                foreach (var param in request.VariablesHttpHead)
                    head += $"&{param.Key}={param.Value}";

                head = head.TrimStart('&');

                foreach (var param in request.VariablesHttpPost)
                    post += $"&{param.Key}={param.Value}";

                post = post.TrimStart('&');

                _responseTimes.Push(new Tuple<TimeSpan, DateTime, string, string, string>(timeSpan, DateTime.Now, ip, head, post));
            }
            else
            {
                _responseTimes.Push(new Tuple<TimeSpan, DateTime, string, string, string>(timeSpan, DateTime.Now, "", "", ""));
            }

            _averageResponseTime = (_averageResponseTime * (_totalPageViews - 1) + timeSpan.TotalMilliseconds) / _totalPageViews;

            if (_maximumResponseTime < timeSpan)
                _maximumResponseTime = timeSpan;

            if (_minimumResponseTime > timeSpan)
                _minimumResponseTime = timeSpan;

            
            if (response != null && response.BinaryData != null)
            {
                _averageResponseSize = (int)System.Math.Round((double)_averageResponseSize * (_totalPageViews - 1) + (double)response.BinaryData.Length) / _totalPageViews;

                if (_maximumResponseSize < response.BinaryData.Length)
                    _maximumResponseSize = response.BinaryData.Length;

                if (_minimumResponseSize > response.BinaryData.Length)
                    _minimumResponseSize = response.BinaryData.Length;
            }
            else
            {

            }
        }
    }

    /// <summary>
    /// A abstract class for directly responding with a string to the client request.
    /// </summary>
    public abstract class PageResponse : ResponseCoreImplementation
    {
        /// <summary>
        /// Constructs (and also registers if you want to) a new Page Response.
        /// </summary>
        /// <param name="URL">The URL of this page.</param>
        /// <param name="register">Shall this page automatically be registered?</param>
        public PageResponse(string URL, bool register = true) : base(URL)
        {
            if (register)
                Master.AddPageResponseToServer(URL, GetContents);
        }

        /// <summary>
        /// This method removes the current page from the server (as URL identifyable object)
        /// </summary>
        protected void RemoveFromServer()
        {
            Master.RemovePageResponseFromServer(URL);
        }

        /// <summary>
        /// A direct answer to the client as string.
        /// </summary>
        /// <param name="sessionData">The current SessionData.</param>
        /// <returns>The response.</returns>
        protected abstract string GetContents(SessionData sessionData);
    }

    /// <summary>
    /// A syncronized direct response as string to the client request
    /// </summary>
    public abstract class SyncronizedPageResponse : PageResponse
    {
        private UsableLockSimple mutex = new UsableLockSimple();

        /// <summary>
        /// Constructs a new SyncronizedPageResponse and registers it if specified at the given URL
        /// </summary>
        /// <param name="URL">the URL of this Page</param>
        /// <param name="register">shall this page be automatically registered?</param>
        public SyncronizedPageResponse(string URL, bool register = true) : base(URL, false)
        {
            if (register)
                Master.AddPageResponseToServer(URL, GetContentSyncronously);
        }

        private string GetContentSyncronously(SessionData sessionData)
        {
            using (mutex.Lock())
            {
                return GetContents(sessionData);
            }
        }

        /// <summary>
        /// A direct answer to the client as string
        /// </summary>
        /// <param name="sessionData">the current sessionData</param>
        /// <returns>the response</returns>
        protected abstract override string GetContents(SessionData sessionData);
    }

    /// <summary>
    /// A direct response as HElement to the client request
    /// </summary>
    public abstract class ElementResponse : ResponseCoreImplementation
    {
        /// <summary>
        /// Constructs a new ElementResponse and registers it if specified at the given URL
        /// </summary>
        /// <param name="URL">the URL of this page</param>
        /// <param name="register">shall this page be automatically registered?</param>
        public ElementResponse(string URL, bool register = true) : base(URL)
        {
            if (register)
                Master.AddPageResponseToServer(URL, GetContents);
        }

        /// <summary>
        /// This method is used to remove the current page from the server (as URL identifyable object)
        /// </summary>
        protected virtual void RemoveFromServer()
        {
            Master.RemovePageResponseFromServer(URL);
        }

        private string GetContents(SessionData sessionData)
        {
            return GetElement(sessionData) * sessionData;
        }

        /// <summary>
        /// A direct answer to the clients request as HElement
        /// </summary>
        /// <param name="sessionData">the current sessionData</param>
        /// <returns>the response</returns>
        protected abstract HElement GetElement(SessionData sessionData);
    }

    /// <summary>
    /// A syncronized direct response as HElement to the clients request
    /// </summary>
    public abstract class SyncronizedElementResponse : ElementResponse
    {
        private UsableLockSimple mutex = new UsableLockSimple();

        /// <summary>
        /// Constructs a new SyncronizedElementResponse and registers it if specified at the given URL
        /// </summary>
        /// <param name="URL">the URL of this page</param>
        /// <param name="register">shall this page be automatically registered at the server?</param>
        public SyncronizedElementResponse(string URL, bool register = true) : base(URL, false)
        {
            if (register)
                Master.AddPageResponseToServer(URL, getContents);
        }

        private string getContents(SessionData sessionData)
        {
            using (mutex.Lock())
            {
                return GetElement(sessionData) * sessionData;
            }
        }

        /// <summary>
        /// The direct pre-syncronized response to the clients request as HElement
        /// </summary>
        /// <param name="sessionData">the current sessionData</param>
        /// <returns>the response</returns>
        protected abstract override HElement GetElement(SessionData sessionData);
    }

    /// <summary>
    /// A direct response as string to the client directory / directory item request
    /// </summary>
    public abstract class DirectoryResponse : IURLIdentifyable, IDebugRespondable, IDebugUpdateableResponse<Exception, TimeSpan, string, HttpRequest, HttpResponse>
    {
        /// <inheritdoc />
        public string URL { get; protected set; }

        private DebugContainerResponseNode _debugResponseNode;
        private FixedSizeQueue<Tuple<Exception, DateTime>> _exceptions = new FixedSizeQueue<Tuple<Exception, DateTime>>(20);
        private FixedSizeQueue<Tuple<TimeSpan, DateTime, string, string, string, string>> _responseTimes = new FixedSizeQueue<Tuple<TimeSpan, DateTime, string, string, string, string>>(50);
        private int _totalPageViews = 0;
        private double _averageResponseTime = 0;
        private TimeSpan _minimumResponseTime = TimeSpan.FromDays(365);
        private TimeSpan _maximumResponseTime = TimeSpan.FromMilliseconds(0);
        private int _totalExceptions = 0;
        private int _averageResponseSize = 0, _minimumResponseSize = int.MaxValue, _maximumResponseSize = int.MinValue;

        /// <summary>
        /// Constructs a new Directory Response object
        /// </summary>
        /// <param name="URL">the URLL of the directory</param>
        /// <param name="register">shall this directory be automatically registered at the server?</param>
        public DirectoryResponse(string URL, bool register = true)
        {
            if (URL == null)
                throw new ArgumentNullException(nameof(URL));

            this.URL = URL;
            _debugResponseNode = new DebugContainerResponseNode($"[{GetType().Name}] '{URL}'", null, GetDebugViewResponse, null, false);

            if (register)
                Master.AddDirectoryPageToServer(this.URL, GetContent);
        }

        /// <summary>
        /// Retrieves the content of this Directory as string to the response
        /// </summary>
        /// <param name="sessionData">the current SessionData</param>
        /// <param name="subUrl">the requested Sub-URL of the request</param>
        /// <returns></returns>
        protected abstract string GetContent(SessionData sessionData, string subUrl);
        
        /// <summary>
        /// Removes this DirectoryResponse from the Server.
        /// </summary>
        protected void RemoveFromServer()
        {
            Master.RemoveDirectoryPageFromServer(URL);
        }

        /// <inheritdoc />
        DebugResponseNode IDebugRespondable.GetDebugResponseNode() => _debugResponseNode;

        /// <summary>
        /// The response for the DebugView for this Response.
        /// </summary>
        /// <param name="sessionData">The current SessionData.</param>
        /// <returns>Returns a HElement containing the Response.</returns>
        protected virtual HElement GetDebugViewResponse(SessionData sessionData)
        {
            HMultipleElements ret = new HMultipleElements();

            ret += new HText($"This page has been called {_totalPageViews} times.");

            if (_totalPageViews > 0)
            {
                ret += new HText($"The average response time of this page is {_averageResponseTime} milliseconds. (maximum: {_maximumResponseTime} | minimum {_minimumResponseTime})");

                ret += new HHeadline("Last Response Times", 2);
                ret += new HTable((from r in _responseTimes select r.ToEnumerable())) { TableHeader = new List<HElement>() { "Response Time", "Time", "Requested Sub-URL", "IP Address", "HTTP Head Variables", "HTTP Post Variables" } };
                ret += new HNewLine();
            }

            if (_totalExceptions > 0)
            {
                ret += new HText($"This page has thrown an exception {_totalExceptions} times.");

                ret += new HHeadline("Last Exceptions", 2);
                ret += new HTable((from e in _exceptions select e.ToEnumerable())) { TableHeader = new List<HElement>() { "Exception", "Time" } };
                ret += new HNewLine();
            }

            return ret;
        }
        
        /// <inheritdoc />
        public void UpdateDebugResponseData(Exception exception, TimeSpan timeSpan, string subUrl, HttpRequest request, HttpResponse response)
        {
            _totalPageViews++;

            if (exception != null)
            {
                _exceptions.Push(new Tuple<Exception, DateTime>(exception, DateTime.Now));

                _totalExceptions++;
            }

            if (request != null)
            {
                string ip = "";
                string head = "";
                string post = "";

                if (request.Stream != null && request.Stream is System.Net.Sockets.NetworkStream) // not a nice solution...
                    ip = ((System.Net.Sockets.Socket)(typeof(System.Net.Sockets.NetworkStream).GetProperty("Socket", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)).GetValue((System.Net.Sockets.NetworkStream)request.Stream, null)).RemoteEndPoint.ToString();

                foreach (var param in request.VariablesHttpHead)
                    head += $"&{param.Key}={param.Value}";

                head = head.TrimStart('&');

                foreach (var param in request.VariablesHttpPost)
                    post += $"&{param.Key}={param.Value}";

                post = post.TrimStart('&');

                _responseTimes.Push(new Tuple<TimeSpan, DateTime, string, string, string, string>(timeSpan, DateTime.Now, subUrl, ip, head, post));
            }
            else
            {
                _responseTimes.Push(new Tuple<TimeSpan, DateTime, string, string, string, string>(timeSpan, DateTime.Now, subUrl, "", "", ""));
            }

            _averageResponseTime = (_averageResponseTime * (_totalPageViews - 1) + timeSpan.TotalMilliseconds) / _totalPageViews;

            if (_maximumResponseTime < timeSpan)
                _maximumResponseTime = timeSpan;

            if (_minimumResponseTime > timeSpan)
                _minimumResponseTime = timeSpan;
            
            if (response != null && response.BinaryData != null)
            {
                _averageResponseSize = (int)System.Math.Round((double)_averageResponseSize * (_totalPageViews - 1) + (double)response.BinaryData.Length) / _totalPageViews;

                if (_maximumResponseSize < response.BinaryData.Length)
                    _maximumResponseSize = response.BinaryData.Length;

                if (_minimumResponseSize > response.BinaryData.Length)
                    _minimumResponseSize = response.BinaryData.Length;
            }
        }
    }

    /// <summary>
    /// A direct response as HElement to the client directory / directory item request
    /// </summary>
    public abstract class DirectoryElementResponse : DirectoryResponse
    {
        /// <summary>
        /// Constructs a new Directory Element Response object
        /// </summary>
        /// <param name="URL">the URLL of the directory</param>
        /// <param name="register">shall this directory be automatically registered at the server?</param>
        public DirectoryElementResponse(string URL, bool register = true) : base(URL, register) { }

        /// <inheritdoc />
        protected override string GetContent(SessionData sessionData, string subUrl) => GetResponse(sessionData, subUrl) * sessionData;

        /// <summary>
        /// Retrieves the content of this Directory as HElement to the response
        /// </summary>
        /// <param name="sessionData">the current SessionData</param>
        /// <param name="subUrl">the requested Sub-URL of the request</param>
        /// <returns>A HElement as response.</returns>
        protected abstract HElement GetResponse(SessionData sessionData, string subUrl);
    }

    /// <summary>
    /// A direct response as serializable object that will be serialized to JSON before being returned to the client directory / directory item request
    /// </summary>
    public abstract class DirectoryJsonResponse : DirectoryResponse
    {
        /// <summary>
        /// Constructs a new Directory Element Response object
        /// </summary>
        /// <param name="URL">the URLL of the directory</param>
        /// <param name="register">shall this directory be automatically registered at the server?</param>
        public DirectoryJsonResponse(string URL, bool register = true) : base(URL, register) { }

        /// <inheritdoc />
        protected override string GetContent(SessionData sessionData, string subUrl) => Serialization.Serializer.WriteJsonDataInMemory(GetResponse(sessionData, subUrl));

        /// <summary>
        /// Retrieves the content of this Directory as serializable object to the response.
        /// </summary>
        /// <param name="sessionData">the current SessionData</param>
        /// <param name="subUrl">the requested Sub-URL of the request</param>
        /// <returns>A serializable object.</returns>
        protected abstract object GetResponse(SessionData sessionData, string subUrl);
    }

    /// <summary>
    /// An automatically caching derivate of ElementResponse.
    /// </summary>
    public abstract class CachedResponse : ElementResponse
    {
        /// <summary>
        /// The default size of a response.
        /// </summary>
        public static int StartingStringBuilderSize = 1024;

        private int MaxStringBuilderSize = StartingStringBuilderSize;
        private int ConsecutiveResultsBelow80PercentSize = 0;
        private ID CacheID;

        /// <summary>
        /// Constructs a new CachedResponse.
        /// </summary>
        /// <param name="URL">The URL to register at.</param>
        /// <param name="register">Shall this page already be registered?</param>
        public CachedResponse(string URL, bool register = true) : base(URL, register)
        {
            CacheID = new ID();
        }

        /// <summary>
        /// Retrieves the auto-cached Element and it's subelements 
        /// (if CachingType in HSelectivelyCacheableElement is set to ECachingType.Cacheable for all elements or subelements that should be cached).
        /// </summary>
        /// <param name="sessionData">The current SessionData.</param>
        /// <returns></returns>
        protected override HElement GetElement(SessionData sessionData)
        {
            StringBuilder stringBuilder = new StringBuilder(MaxStringBuilderSize);

            HElement contents = GetContents(sessionData);

            if (contents == null)
                throw new ArgumentNullException(nameof(GetContents));

            if (contents.IsStaticResponse(CacheID.Value + "/", ECachingType.Default, null))
            {
                string responseString;

                if (ResponseCache.CurrentCacheInstance.Instance.GetCachedStringResponse(CacheID.Value + "/", out responseString))
                {
                    stringBuilder.Append(responseString);
                }
                else
                {
                    contents.IsStaticResponse(CacheID.Value + "/", ECachingType.Default, stringBuilder);

                    ResponseCache.CurrentCacheInstance.Instance.SetCachedStringResponse(CacheID.Value + "/", stringBuilder.ToString());
                }
            }
            else
            {
                contents.IsStaticResponse(CacheID.Value + "/", ECachingType.Default, stringBuilder);
            }

            if (stringBuilder.Length > MaxStringBuilderSize)
            {
                MaxStringBuilderSize = stringBuilder.Length;
                ConsecutiveResultsBelow80PercentSize = 0;
            }
            else if(stringBuilder.Length < MaxStringBuilderSize * 0.8)
            {
                ConsecutiveResultsBelow80PercentSize++;

                if(ConsecutiveResultsBelow80PercentSize > 5)
                    MaxStringBuilderSize = (int)System.Math.Round(MaxStringBuilderSize * 0.95);
            }
            else
            {
                ConsecutiveResultsBelow80PercentSize = 0;
            }

            return new HStringBuilderContainerElement(stringBuilder);
        }

        /// <summary>
        /// Returns a HElement that contains the contents of the requested page.
        /// </summary>
        /// <param name="sessionData">The current SessionData.</param>
        /// <returns>Returns a HElement that contains the contents of the requested page.</returns>
        protected abstract HElement GetContents(SessionData sessionData);

        /// <inheritdoc />
        protected override void RemoveFromServer()
        {
            base.RemoveFromServer();

            ResponseCache.CurrentCacheInstance.Instance.RemoveCachedPrefixes(CacheID.Value);
        }

        private class HStringBuilderContainerElement : HElement
        {
            StringBuilder StringBuilder;

            internal HStringBuilderContainerElement(StringBuilder stringBuilder)
            {
                if (stringBuilder == null)
                    throw new ArgumentNullException(nameof(stringBuilder));

                StringBuilder = stringBuilder;
            }

            public override string GetContent(SessionData sessionData)
            {
                return StringBuilder.ToString();
            }
        }
    }


    /// <summary>
    /// Inherit from this class to allow for replying to request with data (byte[]).
    /// </summary>
    public abstract class DataResponse : IURLIdentifyable, IDebugRespondable, IDebugUpdateableResponse<Exception, TimeSpan, HttpRequest, HttpResponse>
    {
        private DebugContainerResponseNode _debugResponseNode;
        private FixedSizeQueue<Tuple<Exception, DateTime>> _exceptions = new FixedSizeQueue<Tuple<Exception, DateTime>>(20);
        private FixedSizeQueue<Tuple<TimeSpan, DateTime, string, string, string, string, string>> _responseTimes = new FixedSizeQueue<Tuple<TimeSpan, DateTime, string, string, string, string, string>>(20);
        private int _totalPageViews = 0;
        private double _averageResponseTime = 0;
        private TimeSpan _minimumResponseTime = TimeSpan.FromDays(365);
        private TimeSpan _maximumResponseTime = TimeSpan.FromMilliseconds(0);
        private int _totalExceptions = 0;
        private int _averageResponseSize = 0, _minimumResponseSize = int.MaxValue, _maximumResponseSize = int.MinValue;

        public string URL { get; protected set; }

        public DebugResponseNode GetDebugResponseNode() => _debugResponseNode;

        /// <summary>
        /// Creates a new ResponseCoreImplementation element.
        /// </summary>
        /// <param name="URL">The URL of the Response.</param>
        public DataResponse(string URL, bool register = true)
        {
            if (URL == null)
                throw new ArgumentNullException(nameof(URL));

            this.URL = URL;
            _debugResponseNode = new DebugContainerResponseNode($"[{GetType().Name}] '{URL}'", null, GetDebugViewResponse, null, false);

            if (register)
                Master.AddDataResponseToServer(URL, GetDataContents);
        }

        /// <summary>
        /// The method that is called for every request to this URL.
        /// </summary>
        /// <param name="sessionData">The current SessionData.</param>
        /// <param name="contentType">The ContentType of the retrieved Data.</param>
        /// <param name="encoding">The Encoding / CharSet of the retrieved data.</param>
        /// <returns>The requested data as byte[].</returns>
        protected abstract byte[] GetDataContents(HttpSessionData sessionData, out string contentType, ref Encoding encoding);

        /// <summary>
        /// This method is used to remove the current data response from the server (as URL identifyable object)
        /// </summary>
        protected virtual void RemoveFromServer()
        {
            Master.RemoveDataResponseFromServer(URL);
        }

        protected virtual HElement GetDebugViewResponse(SessionData sessionData)
        {
            HMultipleElements ret = new HMultipleElements();

            ret += new HText($"This data response has been called {_totalPageViews} times.");

            if (_totalPageViews > 0)
            {
                ret += new HText($"The average response time of this data response is {_averageResponseTime} milliseconds. (maximum: {_maximumResponseTime} | minimum {_minimumResponseTime})");

                ret += new HHeadline("Last Response Times", 2);
                ret += new HTable((from r in _responseTimes select r.ToEnumerable())) { TableHeader = new List<HElement>() { "Response Time", "Time", "IP Address", "HTTP Head Variables", "HTTP Post Variables", "ContentType", "Charset / Encoding" } };
                ret += new HNewLine();
            }

            if (_totalExceptions > 0)
            {
                ret += new HText($"This data response has thrown an exception {_totalExceptions} times.");

                ret += new HHeadline("Last Exceptions", 2);
                ret += new HTable((from e in _exceptions select e.ToEnumerable())) { TableHeader = new List<HElement>() { "Exception", "Time" } };
                ret += new HNewLine();
            }

            return ret;
        }

        public void UpdateDebugResponseData(Exception exception, TimeSpan timeSpan, HttpRequest request, HttpResponse response)
        {
            _totalPageViews++;

            if (exception != null)
            {
                _exceptions.Push(new Tuple<Exception, DateTime>(exception, DateTime.Now));

                _totalExceptions++;
            }

            if (request != null)
            {
                string ip = "";
                string head = "";
                string post = "";

                if (request.Stream != null && request.Stream is System.Net.Sockets.NetworkStream) // not a nice solution...
                    ip = ((System.Net.Sockets.Socket)(typeof(System.Net.Sockets.NetworkStream).GetProperty("Socket", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)).GetValue((System.Net.Sockets.NetworkStream)request.Stream, null)).RemoteEndPoint.ToString();

                foreach (var param in request.VariablesHttpHead)
                    head += $"&{param.Key}={param.Value}";

                head = head.TrimStart('&');

                foreach (var param in request.VariablesHttpPost)
                    post += $"&{param.Key}={param.Value}";

                post = post.TrimStart('&');

                _responseTimes.Push(new Tuple<TimeSpan, DateTime, string, string, string, string, string>(timeSpan, DateTime.Now, ip, head, post, response?.ContentType ?? "", response?.CharSet ?? "UTF-8"));
            }
            else
            {
                _responseTimes.Push(new Tuple<TimeSpan, DateTime, string, string, string, string, string>(timeSpan, DateTime.Now, "", "", "", response?.ContentType ?? "", response?.CharSet ?? "UTF-8"));
            }

            _averageResponseTime = (_averageResponseTime * (_totalPageViews - 1) + timeSpan.TotalMilliseconds) / _totalPageViews;

            if (_maximumResponseTime < timeSpan)
                _maximumResponseTime = timeSpan;

            if (_minimumResponseTime > timeSpan)
                _minimumResponseTime = timeSpan;


            if (response != null && response.BinaryData != null)
            {
                _averageResponseSize = (int)System.Math.Round((double)_averageResponseSize * (_totalPageViews - 1) + (double)response.BinaryData.Length) / _totalPageViews;

                if (_maximumResponseSize < response.BinaryData.Length)
                    _maximumResponseSize = response.BinaryData.Length;

                if (_minimumResponseSize > response.BinaryData.Length)
                    _minimumResponseSize = response.BinaryData.Length;
            }
            else
            {

            }
        }
    }

    /// <summary>
    /// Inherit from this class to allow for replying to request with objects which will be serialized to JSON. Please make sure your objects are serializable.
    /// </summary>
    public abstract class JsonResponse : IURLIdentifyable, IDebugRespondable, IDebugUpdateableResponse<Exception, TimeSpan, HttpRequest, HttpResponse>
    {
        private DebugContainerResponseNode _debugResponseNode;
        private FixedSizeQueue<Tuple<Exception, DateTime>> _exceptions = new FixedSizeQueue<Tuple<Exception, DateTime>>(20);
        private FixedSizeQueue<Tuple<TimeSpan, DateTime, string, string, string, string, string>> _responseTimes = new FixedSizeQueue<Tuple<TimeSpan, DateTime, string, string, string, string, string>>(20);
        private int _totalPageViews = 0;
        private double _averageResponseTime = 0;
        private TimeSpan _minimumResponseTime = TimeSpan.FromDays(365);
        private TimeSpan _maximumResponseTime = TimeSpan.FromMilliseconds(0);
        private int _totalExceptions = 0;
        private int _averageResponseSize = 0, _minimumResponseSize = int.MaxValue, _maximumResponseSize = int.MinValue;

        public string URL { get; protected set; }

        public DebugResponseNode GetDebugResponseNode() => _debugResponseNode;

        /// <summary>
        /// Creates a new ResponseCoreImplementation element.
        /// </summary>
        /// <param name="URL">The URL of the Response.</param>
        public JsonResponse(string URL, bool register = true)
        {
            if (URL == null)
                throw new ArgumentNullException(nameof(URL));

            this.URL = URL;
            _debugResponseNode = new DebugContainerResponseNode($"[{GetType().Name}] '{URL}'", null, GetDebugViewResponse, null, false);

            if (register)
                Master.AddDataResponseToServer(URL, GetResponseAsJson);
        }

        /// <summary>
        /// The internal method that is called for every request to this URL.
        /// </summary>
        /// <param name="sessionData">The current SessionData.</param>
        /// <param name="contentType">The ContentType of the retrieved Data.</param>
        /// <param name="encoding">The Encoding / CharSet of the retrieved data.</param>
        /// <returns>The requested data as byte[].</returns>
        private byte[] GetResponseAsJson(HttpSessionData sessionData, out string contentType, ref Encoding encoding)
        {
            contentType = "application/json";

            object response = GetResponse(sessionData);

            return encoding.GetBytes(Serialization.Serializer.WriteJsonDataInMemory(response));
        }

        /// <summary>
        /// The method called for every request to this URL.
        /// </summary>
        /// <param name="sessionData"></param>
        /// <returns>Returns the requested *serializable* object</returns>
        abstract protected object GetResponse(HttpSessionData sessionData);

        /// <summary>
        /// This method is used to remove the current data response from the server (as URL identifyable object)
        /// </summary>
        protected virtual void RemoveFromServer()
        {
            Master.RemoveDataResponseFromServer(URL);
        }

        protected virtual HElement GetDebugViewResponse(SessionData sessionData)
        {
            HMultipleElements ret = new HMultipleElements();

            ret += new HText($"This json response has been called {_totalPageViews} times.");

            if (_totalPageViews > 0)
            {
                ret += new HText($"The average response time of this data response is {_averageResponseTime} milliseconds. (maximum: {_maximumResponseTime} | minimum {_minimumResponseTime})");

                ret += new HHeadline("Last Response Times", 2);
                ret += new HTable((from r in _responseTimes select r.ToEnumerable())) { TableHeader = new List<HElement>() { "Response Time", "Time", "IP Address", "HTTP Head Variables", "HTTP Post Variables", "ContentType", "Charset / Encoding" } };
                ret += new HNewLine();
            }

            if (_totalExceptions > 0)
            {
                ret += new HText($"This data response has thrown an exception {_totalExceptions} times.");

                ret += new HHeadline("Last Exceptions", 2);
                ret += new HTable((from e in _exceptions select e.ToEnumerable())) { TableHeader = new List<HElement>() { "Exception", "Time" } };
                ret += new HNewLine();
            }

            return ret;
        }

        public void UpdateDebugResponseData(Exception exception, TimeSpan timeSpan, HttpRequest request, HttpResponse response)
        {
            _totalPageViews++;

            if (exception != null)
            {
                _exceptions.Push(new Tuple<Exception, DateTime>(exception, DateTime.Now));

                _totalExceptions++;
            }

            if (request != null)
            {
                string ip = "";
                string head = "";
                string post = "";

                if (request.Stream != null && request.Stream is System.Net.Sockets.NetworkStream) // not a nice solution...
                    ip = ((System.Net.Sockets.Socket)(typeof(System.Net.Sockets.NetworkStream).GetProperty("Socket", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)).GetValue((System.Net.Sockets.NetworkStream)request.Stream, null)).RemoteEndPoint.ToString();

                foreach (var param in request.VariablesHttpHead)
                    head += $"&{param.Key}={param.Value}";

                head = head.TrimStart('&');

                foreach (var param in request.VariablesHttpPost)
                    post += $"&{param.Key}={param.Value}";

                post = post.TrimStart('&');

                _responseTimes.Push(new Tuple<TimeSpan, DateTime, string, string, string, string, string>(timeSpan, DateTime.Now, ip, head, post, response?.ContentType ?? "", response?.CharSet ?? "UTF-8"));
            }
            else
            {
                _responseTimes.Push(new Tuple<TimeSpan, DateTime, string, string, string, string, string>(timeSpan, DateTime.Now, "", "", "", response?.ContentType ?? "", response?.CharSet ?? "UTF-8"));
            }

            _averageResponseTime = (_averageResponseTime * (_totalPageViews - 1) + timeSpan.TotalMilliseconds) / _totalPageViews;

            if (_maximumResponseTime < timeSpan)
                _maximumResponseTime = timeSpan;

            if (_minimumResponseTime > timeSpan)
                _minimumResponseTime = timeSpan;


            if (response != null && response.BinaryData != null)
            {
                _averageResponseSize = (int)System.Math.Round((double)_averageResponseSize * (_totalPageViews - 1) + (double)response.BinaryData.Length) / _totalPageViews;

                if (_maximumResponseSize < response.BinaryData.Length)
                    _maximumResponseSize = response.BinaryData.Length;

                if (_minimumResponseSize > response.BinaryData.Length)
                    _minimumResponseSize = response.BinaryData.Length;
            }
            else
            {

            }
        }
    }

    /// <summary>
    /// This Helper-Class is Used to quickly define new pages at the server
    /// </summary>
    public static class InstantPageResponse
    {
        /// <summary>
        /// adds a page to the server, that executes the given code
        /// </summary>
        public static void AddInstantPageResponse(string URL, Master.GetContents code)
        {
            Master.AddPageResponseToServer(URL, code);
        }

        /// <summary>
        /// adds a temporary page to the server, that executes the given code (only available for ONE request)
        /// </summary>
        /// <param name="code">the code to execute</param>
        /// <param name="instantlyRemove">runtime code should instantly remove these - constructors should not remove, since then they'll be gone the next compile</param>
        /// <returns>the name at which this temporary page will be available at.</returns>
        public static string AddOneTimeInstantPageResponse(Master.GetContents code, bool instantlyRemove)
        {
            string hash = SessionContainer.GenerateUnusedHash();

            if (instantlyRemove)
            {
                Master.AddOneTimePageResponseToServer(hash, code);
            }
            else
            {
                Master.AddPageResponseToServer(hash, code);

            }

            return "/" + hash;
        }

        /// <summary>
        /// adds a page to the server, that redirects to "destinationURL" in X milliseconds
        /// </summary>
        public static void AddTimedRedirect(string originURL, string message, int milliseconds, string destinationURL, bool copyPOST = false)
        {
            AddInstantPageResponse(originURL, sessionData => GenerateRedirectInMillisecondsCode(destinationURL, message, milliseconds, sessionData, copyPOST));
        }

        /// <summary>
        /// adds a page to the server, that redirects to "destinationURL"
        /// </summary>
        public static void AddRedirect(string originURL, string destinationURL, bool copyPOST = false)
        {
            AddInstantPageResponse(originURL, sessionData => GenerateRedirectCode(destinationURL, sessionData, copyPOST));
        }

        /// <summary>
        /// adds a page to the server, that redirects to "destinationURL" and executes the given code
        /// </summary>
        public static void AddRedirectWithCode(string originURL, string destinationURL, Action<HttpSessionData> action, bool copyPOST = false)
        {
            AddInstantPageResponse(originURL, sessionData =>
            {
                action(sessionData);
                return GenerateRedirectCode(destinationURL, sessionData, copyPOST);
            });
        }

        /// <summary>
        /// adds a page to the server, that redirects to "destinationURLifTRUE" if the conditional code returns true and redirects to "destinationURLifFALSE" if the conditional code returns false
        /// </summary>
        public static void AddConditionalRedirect(string originalURL, string destinationURLifTRUE, string destinationURLifFALSE, Func<HttpSessionData, bool> conditionalCode, bool copyPOST = false)
        {
            AddInstantPageResponse(originalURL, sessionData =>
            {
                if (conditionalCode(sessionData))
                    return GenerateRedirectCode(destinationURLifTRUE, sessionData, copyPOST);

                return GenerateRedirectCode(destinationURLifFALSE, sessionData, copyPOST);
            });
        }

        /// <summary>
        /// adds a page to the server, that redirects if the conditional code returns true and executes other code if the conditional code returns false
        /// </summary>
        public static void AddRedirectOrCode(string originalURL, string destinationURLifTRUE, Master.GetContents codeIfFALSE, Func<HttpSessionData, bool> conditionalCode, bool copyPOST = false)
        {
            AddInstantPageResponse(originalURL, sessionData =>
            {
                if (conditionalCode(sessionData))
                    return GenerateRedirectCode(destinationURLifTRUE, sessionData, copyPOST);

                return codeIfFALSE(sessionData);
            });
        }

        /// <summary>
        /// adds a temporary page to the server, that redirects to "destinationURL" (only available for ONE request)
        /// </summary>
        /// <param name="destinationURL">the desired URL to reach</param>
        /// <param name="instantlyRemove">runtime code should instantly remove these - constructors should not remove, since then they'll be gone the next compile</param>
        /// <param name="copyPOST">specifies whether all POST values given should be copied throughout the whole redirecting process</param>
        /// <returns>the name at which this temporary page will be available at.</returns>
        public static string AddOneTimeRedirect(string destinationURL, bool instantlyRemove, bool copyPOST = false)
        {
            return AddOneTimeInstantPageResponse(sessionData => GenerateRedirectCode(destinationURL, sessionData, copyPOST)
                , instantlyRemove);
        }

        /// <summary>
        /// adds a temporary page to the server, that redirects to "destinationURL" and executes the given code (only available for ONE request)
        /// </summary>
        /// <param name="destinationURL">the desired URL to reach</param>
        /// <param name="instantlyRemove">runtime code should instantly remove these - constructors should not remove, since then they'll be gone the next compile</param>
        /// <param name="action">the code to execute</param>
        /// <param name="copyPOST">specifies whether all POST values given should be copied throughout the whole redirecting process</param>
        public static string AddOneTimeRedirectWithCode(string destinationURL, bool instantlyRemove, Action<HttpSessionData> action, bool copyPOST = false)
        {
            return AddOneTimeInstantPageResponse(sessionData =>
            {
                action(sessionData);
                return GenerateRedirectCode(destinationURL, sessionData, copyPOST);
            }
            , instantlyRemove);
        }

        /// <summary>
        /// adds a temporary page to the server, that redirects to "destinationURL" in X milliseconds (only available for ONE request)
        /// </summary>
        /// <param name="destinationURL">the desired URL to reach</param>
        /// <param name="message">the message to display</param>
        /// <param name="milliseconds">the amount of milliseconds to wait before redirecting</param>
        /// <param name="instantlyRemove">runtime code should instantly remove these - constructors should not remove, since then they'll be gone the next compile</param>
        /// <param name="copyPOST">specifies whether all POST values given should be copied throughout the whole redirecting process</param>
        /// <returns>the name at which this temporary page will be available at.</returns>
        public static string AddOneTimeTimedRedirect(string destinationURL, string message, int milliseconds, bool instantlyRemove, bool copyPOST = false)
        {
            return AddOneTimeInstantPageResponse(sessionData =>
            {
                return GenerateRedirectInMillisecondsCode(destinationURL, message, milliseconds, sessionData, copyPOST);
            }
            , instantlyRemove);
        }

        /// <summary>
        /// adds a temporary page to the server, that redirects to "destinationURLifTRUE" if the conditional code returns true and redirects to "destinationURLifFALSE" if the conditional code returns false (only available for ONE request)
        /// </summary>
        /// <param name="destinationURLifTRUE">the desired URL to reach if the code returns true</param>
        /// <param name="destinationURLifFALSE">the desired URL to reach if the code returns false</param>
        /// <param name="instantlyRemove">runtime code should instantly remove these - constructors should not remove, since then they'll be gone the next compile</param>
        /// <param name="conditionalCode">the conditional code to execute</param>
        /// <param name="copyPOST">specifies whether all POST values given should be copied throughout the whole redirecting process</param>
        /// <returns>the name at which this temporary page will be available at.</returns>
        public static string AddOneTimeConditionalRedirect(string destinationURLifTRUE, string destinationURLifFALSE, bool instantlyRemove, Func<HttpSessionData, bool> conditionalCode, bool copyPOST = false)
        {
            return AddOneTimeInstantPageResponse(sessionData =>
            {
                if (conditionalCode(sessionData))
                    return GenerateRedirectCode(destinationURLifTRUE, sessionData, copyPOST);

                return GenerateRedirectCode(destinationURLifFALSE, sessionData, copyPOST);
            }
                , instantlyRemove);
        }

        /// <summary>
        /// adds a temporary page to the server, that redirects if the conditional code returns true and executes other code if the conditional code returns false (only available for ONE request)
        /// </summary>
        /// <param name="destinationURLifTRUE">the desired URL to reach if the conditional code returns true</param>
        /// <param name="codeIfFALSE">the code to be executed if the conditional code returns false</param>
        /// <param name="instantlyRemove">runtime code should instantly remove these - constructors should not remove, since then they'll be gone the next compile</param>
        /// <param name="conditionalCode">the conditional code to execute</param>
        /// <param name="copyPOST">specifies whether all POST values given should be copied throughout the whole redirecting process</param>
        /// <returns>the name at which this temporary page will be available at.</returns>
        public static string AddOneTimeRedirectOrCode(string destinationURLifTRUE, Master.GetContents codeIfFALSE, bool instantlyRemove, Func<HttpSessionData, bool> conditionalCode, bool copyPOST = false)
        {
            return AddOneTimeInstantPageResponse(sessionData =>
            {
                if (conditionalCode(sessionData))
                    return GenerateRedirectCode(destinationURLifTRUE, sessionData, copyPOST);

                return codeIfFALSE(sessionData);
            }
            , instantlyRemove);
        }

        /// <summary>
        /// quickly generates a redirecting html page
        /// </summary>
        /// <param name="destinationURL">the desired url to reach</param>
        /// <param name="sessionData">the current SessionData</param>
        /// <param name="copyPOST">shall the POST-Values be copied?</param>
        /// <returns>the page as string</returns>
        public static string GenerateRedirectCode(string destinationURL, SessionData sessionData = null, bool copyPOST = false)
        {
            if(!copyPOST)
            {
                if (sessionData == null)
                {
                    return "<head><meta http-equiv=\"refresh\" content=\"1; url = "
                                   + destinationURL + "\"><script type=\"text/javascript\">window.location.href = \""
                                   + destinationURL + "\"</script><title>Page Redirection</title><style type=\"text/css\">hr{border:solid;border-width:3;color:#efefef;} p {overflow:overlay;}</style></head><body style='background-color: #f1f1f1;margin: 0;'><div style='font-family: \"Segoe UI\" ,sans-serif;width: 70%;max-width: 1200px;margin: 0em auto;font-size: 16pt;background-color: #fdfdfd;padding: 4em 8em;color: #4e4e4e;'><h1 style='font-weight: lighter;font-size: 50pt;'>Page Redirection</h1><hr><p>If you are not redirected automatically, follow this <a href='"
                                   + destinationURL + "'>link.</a></p><p style='text-align:right'>- LamestWebserver (LameOS)</p></div></body>";
                }
                else
                {
                    return "<head><script type=\"text/javascript\">onload = function(){var f=document.createElement('form');f.setAttribute('method','POST');f.setAttribute('action','"
                                    + destinationURL +
                                    "');f.setAttribute('enctype','application/x-www-form-urlencoded');var i=document.createElement('input');i.setAttribute('type','hidden');i.setAttribute('name','ssid');i.setAttribute('value','"
                                    + sessionData.Ssid +
                                    "');f.appendChild(i);document.body.appendChild(f);f.submit();document.body.remove(f);}</script><title>Page Redirection</title><style type=\"text/css\">hr{border:solid;border-width:3;color:#efefef;} p {overflow:overlay;}</style></head><body style='background-color: #f1f1f1;margin: 0;'><div style='font-family: \"Segoe UI\" ,sans-serif;width: 70%;max-width: 1200px;margin: 0em auto;font-size: 16pt;background-color: #fdfdfd;padding: 4em 8em;color: #4e4e4e;'><h1 style='font-weight: lighter;font-size: 50pt;'>Page Redirection</h1><hr><p>If you are not redirected automatically, follow this <a href='#' onclick=\"var f=document.createElement('form');f.setAttribute('method','POST');f.setAttribute('action','"
                                    + destinationURL +
                                    "');f.setAttribute('enctype','application/x-www-form-urlencoded');var i=document.createElement('input');i.setAttribute('type','hidden');i.setAttribute('name','ssid');i.setAttribute('value','"
                                    + sessionData.Ssid +
                                    "');f.appendChild(i);document.body.appendChild(f);f.submit();document.body.remove(f);\">link.</a></p><p style='text-align:right'>- LamestWebserver (LameOS)</p></div></body>";
                }
            }
            else
            {
                if (sessionData == null)
                {
                    return "<head><meta http-equiv=\"refresh\" content=\"1; url = "
                                   + destinationURL + "\"><script type=\"text/javascript\">window.location.href = \""
                                   + destinationURL + "\"</script><title>Page Redirection</title><style type=\"text/css\">hr{border:solid;border-width:3;color:#efefef;} p {overflow:overlay;}</style></head><body style='background-color: #f1f1f1;margin: 0;'><div style='font-family: \"Segoe UI\" ,sans-serif;width: 70%;max-width: 1200px;margin: 0em auto;font-size: 16pt;background-color: #fdfdfd;padding: 4em 8em;color: #4e4e4e;'><h1 style='font-weight: lighter;font-size: 50pt;'>Page Redirection</h1><hr><p>If you are not redirected automatically, follow this <a href='"
                                   + destinationURL + "'>link.</a></p><p style='text-align:right'>- LamestWebserver (LameOS)</p></div></body>";
                }
                else
                {
                    return "<head><script type=\"text/javascript\">onload = "
                                    + ScriptCollection.GetPageReferalWithFullPostInMilliseconds(sessionData, new object[] { destinationURL, 0 }) + 
                                    "</script><title>Page Redirection</title><style type=\"text/css\">hr{border:solid;border-width:3;color:#efefef;} p {overflow:overlay;}</style></head><body style='background-color: #f1f1f1;margin: 0;'><div style='font-family: \"Segoe UI\" ,sans-serif;width: 70%;max-width: 1200px;margin: 0em auto;font-size: 16pt;background-color: #fdfdfd;padding: 4em 8em;color: #4e4e4e;'><h1 style='font-weight: lighter;font-size: 50pt;'>Page Redirection</h1><hr><p>If you are not redirected automatically, follow this <a href='#' onclick=\""
                                    + ScriptCollection.GetPageReferalWithFullPostInMilliseconds(sessionData, new object[] { destinationURL, 0 }) +
                                    "\">link.</a></p><p style='text-align:right'>- LamestWebserver (LameOS)</p></div></body>";
                }
            }
        }

        /// <summary>
        /// quickly generates a redirecting html page that waits a few milliseconds and displays a message
        /// </summary>
        /// <param name="destinationURL">the desired url to reach</param>
        /// <param name="message">the message to display</param>
        /// <param name="milliseconds">the amount of milliseconds to wait</param>
        /// <param name="sessionData">the current SessionData</param>
        /// <param name="copyPOST">shall the POST-Values be copied?</param>
        /// <returns>the page as string</returns>
        public static string GenerateRedirectInMillisecondsCode(string destinationURL, string message, int milliseconds, HttpSessionData sessionData = null, bool copyPOST = false)
        {
            if (!copyPOST)
            {
                if (sessionData == null)
                {
                    return "<head><meta http-equiv=\"refresh\" content=\"" + System.Math.Round(milliseconds / 1000f) + "; url = "
                                   + destinationURL + "\"><script type=\"text/javascript\">setTimeout(function() { window.location.href = \""
                                   + destinationURL + "\";}, "
                                   + milliseconds + ");</script><title>Page Redirection</title><style type=\"text/css\">hr{border:solid;border-width:3;color:#efefef;} p {overflow:overlay;}</style></head><body style='background-color: #f1f1f1;margin: 0;'><div style='font-family: \"Segoe UI\" ,sans-serif;width: 70%;max-width: 1200px;margin: 0em auto;font-size: 16pt;background-color: #fdfdfd;padding: 4em 8em;color: #4e4e4e;'><h2 style='font-weight: lighter;font-size: 40pt;'>"
                                   + message + "</h2><hr><p><i style='color:#404040;'>If you are not redirected automatically, follow this <a href='"
                                   + destinationURL + "'>link.</a></i></p></div></body>";
                }
                else
                {
                    return "<head><script type=\"text/javascript\">onload = function(){var f=document.createElement('form');f.setAttribute('method','POST');f.setAttribute('action','"
                                    + destinationURL +
                                    "');f.setAttribute('enctype','application/x-www-form-urlencoded');var i=document.createElement('input');i.setAttribute('type','hidden');i.setAttribute('name','ssid');i.setAttribute('value','"
                                    + sessionData.Ssid +
                                    "');f.appendChild(i);document.body.appendChild(f);setTimeout(function() {f.submit();document.body.remove(f);}," + milliseconds + ");}</script><title>Page Redirection</title><style type=\"text/css\">hr{border:solid;border-width:3;color:#efefef;} p {overflow:overlay;}</style></head><body style='background-color: #f1f1f1;margin: 0;'><div style='font-family: \"Segoe UI\" ,sans-serif;width: 70%;max-width: 1200px;margin: 0em auto;font-size: 16pt;background-color: #fdfdfd;padding: 4em 8em;color: #4e4e4e;'><p><h2 style='font-weight: lighter;font-size: 40pt;'>"
                                    + message + "</h2></p><hr><p><i style='color:#404040;'>If you are not redirected automatically, follow this <a href='#' onclick=\"var f=document.createElement('form');f.setAttribute('method','POST');f.setAttribute('action','"
                                    + destinationURL +
                                    "');f.setAttribute('enctype','application/x-www-form-urlencoded');var i=document.createElement('input');i.setAttribute('type','hidden');i.setAttribute('name','ssid');i.setAttribute('value','"
                                    + sessionData.Ssid +
                                    "');f.appendChild(i);document.body.appendChild(f);setTimeout(function() {f.submit();document.body.remove(f);}," + milliseconds + ");\">link.</a></i></p></div></body>";
                }
            }
            else
            {
                if (sessionData == null)
                {
                    return "<head><meta http-equiv=\"refresh\" content=\"" + System.Math.Round(milliseconds / 1000f) + "; url = "
                                   + destinationURL + "\"><script type=\"text/javascript\">setTimeout(function() { window.location.href = \""
                                   + destinationURL + "\";}, "
                                   + milliseconds + ");</script><title>Page Redirection</title><style type=\"text/css\">hr{border:solid;border-width:3;color:#efefef;} p {overflow:overlay;}</style></head><body style='background-color: #f1f1f1;margin: 0;'><div style='font-family: \"Segoe UI\" ,sans-serif;width: 70%;max-width: 1200px;margin: 0em auto;font-size: 16pt;background-color: #fdfdfd;padding: 4em 8em;color: #4e4e4e;'><h2 style='font-weight: lighter;font-size: 40pt;'>"
                                   + message + "</h2><hr><p><i style='color:#404040;'>If you are not redirected automatically, follow this <a href='"
                                   + destinationURL + "'>link.</a></i></p></div></body>";
                }
                else
                {
                    return "<head><script type=\"text/javascript\">onload = "
                                    + ScriptCollection.GetPageReferalWithFullPostInMilliseconds(sessionData, new object[] { destinationURL, milliseconds }) +
                                    "</script><title>Page Redirection</title><style type=\"text/css\">hr{border:solid;border-width:3;color:#efefef;} p {overflow:overlay;}</style></head><body style='background-color: #f1f1f1;margin: 0;'><div style='font-family: \"Segoe UI\" ,sans-serif;width: 70%;max-width: 1200px;margin: 0em auto;font-size: 16pt;background-color: #fdfdfd;padding: 4em 8em;color: #4e4e4e;'><p><h2 style='font-weight: lighter;font-size: 40pt;'>"
                                    + message + "</h2></p><hr><p><i style='color:#404040;'>If you are not redirected automatically, follow this <a href='#' onclick=\""
                                    + ScriptCollection.GetPageReferalWithFullPostInMilliseconds(sessionData, new object[] { destinationURL, milliseconds }) +
                                    "\">link.</a></i></p></div></body>";
                }
            }
        }
    }
}
