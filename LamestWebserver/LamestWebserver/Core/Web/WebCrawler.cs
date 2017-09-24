﻿using LamestWebserver.Collections;
using LamestWebserver.Serialization;
using LamestWebserver.Synchronization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace LamestWebserver.Core.Web
{
    /// <summary>
    /// Provides an automated easy-to-use WebCrawler.
    /// </summary>
    public class WebCrawler : NullCheckable
    {
        /// <summary>
        /// The URL to begin with.
        /// </summary>
        public readonly string StartURL;

        /// <summary>
        /// The Prefixes of the valid URLs to Crawl into.
        /// </summary>
        public readonly string[] Prefixes;

        /// <summary>
        /// The delegate to execute whenever a new match was found.
        /// </summary>
        public readonly Func<string, WebCrawler, bool> OnNewPage;

        /// <summary>
        /// Has the WebCrawler processed every possible page?
        /// </summary>
        public SynchronizedValue<bool> IsDone { get; private set; } = new SynchronizedValue<bool>(false);

        /// <summary>
        /// If false: the last visited page before OnNewPage returns false is removed from VisitedPages. 
        /// This might be handy if the bot detection of a webpage is messing you up and you want to continue where you _successfully_ left of.
        /// </summary>
        public bool KeepLastEntry = true;

        private WebCrawlerState CurrentState;
        private SynchronizedValue<bool> Running = new SynchronizedValue<bool>(false);
        private Thread[] crawlerThreads;
        private Regex linkParser = new Regex(@"href=[""'](?<url>(http|https)://[^/]*?\.[^/]*?\)(/.*)?[""']", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private UsableMutexSlim WebCrawlerStateMutex = new UsableMutexSlim();

        /// <summary>
        /// Constructs a new WebCrawler instance. Doesn't start it yet.
        /// </summary>
        /// <param name="startURL">The URL to begin crawling at.</param>
        /// <param name="prefixes">The valid prefixes of an URL to load. (usually the page domain that you want to crawl through). ALL pages are valid if null.</param>
        /// <param name="onNewPage">The function to execute whenever a valid page is found.</param>
        /// <param name="webRequestFactory">A WebRequestFactory to construct the Requests with.</param>
        /// <param name="threadCount">The number of worker-threads to use.</param>
        public WebCrawler(string startURL, string[] prefixes, Func<string, WebCrawler, bool> onNewPage, WebRequestFactory webRequestFactory, int threadCount = 1)
        {
            if (startURL == null)
                throw new ArgumentNullException(nameof(startURL));

            if (prefixes == null)
                prefixes = new string[] { "" };
            else if (prefixes.Length < 1)
                throw new ArgumentOutOfRangeException(nameof(prefixes));

            for (int i = 0; i < prefixes.Length; i++)
                prefixes[i] = prefixes[i].Replace("http://", "").Replace("https://", "").Replace("www.", "");

            if (onNewPage == null)
                throw new ArgumentNullException(nameof(onNewPage));

            if (webRequestFactory == null)
                throw new ArgumentNullException(nameof(webRequestFactory));

            if (threadCount < 1)
                throw new ArgumentOutOfRangeException(nameof(threadCount));

            StartURL = startURL;
            Prefixes = prefixes;
            OnNewPage = onNewPage;

            using (WebCrawlerStateMutex.Lock())
            {
                CurrentState = new WebCrawlerState(webRequestFactory);
                CurrentState.ToGo.Add(StartURL);
            }

            crawlerThreads = new Thread[threadCount];
        }

        /// <summary>
        /// Loads a previous state of the crawler.
        /// </summary>
        /// <param name="fileName">The filename of the saved state.</param>
        public void LoadState(string fileName)
        {
            if (fileName == null)
                throw new ArgumentNullException(nameof(fileName));

            using (WebCrawlerStateMutex.Lock())
                CurrentState = Serializer.ReadJsonData<WebCrawlerState>(fileName);
        }

        /// <summary>
        /// Saves the current state of the crawler.
        /// </summary>
        /// <param name="fileName">The filename for the saved state.</param>
        public void SaveState(string fileName)
        {
            if (fileName == null)
                throw new ArgumentNullException(nameof(fileName));

            using (WebCrawlerStateMutex.Lock())
                Serializer.WriteJsonData(CurrentState, fileName);
        }
        
        /// <summary>
        /// Stops the crawler.
        /// </summary>
        public void Stop()
        {
            Running.Value = false;
        }

        /// <summary>
        /// Starts the WebCrawler.
        /// </summary>
        /// <returns>Returns the current WebCrawler so that you can type 'var crawler = new WebCrawler(...).Start();'.</returns>
        public WebCrawler Start()
        {
            if (IsDone)
                return this;

            Running.Value = true;

            for(int i = 0; i < crawlerThreads.Length; i++)
            {
                if (crawlerThreads[i] != null && crawlerThreads[i].ThreadState == ThreadState.Running)
                    return this;

                crawlerThreads[i] = new Thread(Crawl);
                crawlerThreads[i].Start();
            }

            return this;
        }

        private void Crawl()
        {
            while (Running)
            {
                string currentSite, response;

                using (WebCrawlerStateMutex.Lock())
                {
                    if (CurrentState.ToGo.Count == 0)
                    {
                        Running.Value = false;
                        return;
                    }
                
                    currentSite = CurrentState.ToGo[0];
                }

                response = CurrentState.WebrequestFactory.GetResponse(currentSite);

                var matches = linkParser.Matches(response);

                foreach (Match m in matches)
                {
                    string url = m.Value.Replace("href='", "").Replace("href=\"", "").Split('\'', '\"')[0];
                    string domainBasedUrl = url.Replace("http://", "").Replace("https://", "").Replace("www.", "");

                    bool alreadyVisited;

                    using (WebCrawlerStateMutex.Lock())
                        alreadyVisited = CurrentState.VisitedPages.ContainsKey(url);

                    if (!alreadyVisited && (from start in Prefixes where domainBasedUrl.StartsWith(start) select true).Any())
                    {
                        using (WebCrawlerStateMutex.Lock())
                        {
                            CurrentState.ToGo.Add(url);
                            CurrentState.VisitedPages.Add(currentSite, true);
                        }

                        if (Running && !OnNewPage(url, this))
                        {
                            Running.Value = false;
                            
                            if(!KeepLastEntry)
                                using (WebCrawlerStateMutex.Lock())
                                    CurrentState.VisitedPages.Remove(currentSite);

                            return;
                        }

                    }
                }

                CurrentState.ToGo.RemoveAt(0);
            }

            if (CurrentState.ToGo.Count == 0)
                IsDone.Value = true;
        }

        /// <summary>
        /// This is just public to be serializable.
        /// </summary>
        [Serializable]
        public class WebCrawlerState : NullCheckable
        {
            /// <summary>
            /// The visited pages so far.
            /// </summary>
            public AVLHashMap<string, bool> VisitedPages; // just because it's fast to look through it - this is not the best possible implementation.

            /// <summary>
            /// The discovered Pages that haven't been visited.
            /// </summary>
            public List<string> ToGo;

            /// <summary>
            /// The internal WebRequestFactory.
            /// </summary>
            public WebRequestFactory WebrequestFactory;

            /// <summary>
            /// Deserialization Constructor.
            /// </summary>
            public WebCrawlerState()
            {
            }

            internal WebCrawlerState(WebRequestFactory webRequestFactory)
            {
                if (webRequestFactory == null)
                    throw new ArgumentNullException(nameof(webRequestFactory));

                VisitedPages = new AVLHashMap<string, bool>();
                ToGo = new List<string>();

                WebrequestFactory = webRequestFactory;
            }
        }
    }
}
