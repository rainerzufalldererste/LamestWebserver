using LamestWebserver.Collections;
using LamestWebserver.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace LamestWebserver.Core.Web
{
    public class WebCrawler : NullCheckable
    {
        public readonly string StartURL;
        public readonly string[] Prefixes;
        public readonly Func<string, WebCrawler, bool> OnNewPage;
        WebCrawlerState CurrentState;
        bool Running = true;
        Thread crawlerThread;

        public bool IsDone { get; private set; } = false;

        public WebCrawler(string startURL, string[] prefixes, Func<string, WebCrawler, bool> onNewPage, WebRequestFactory webRequestFactory)
        {
            StartURL = startURL;
            Prefixes = prefixes;
            OnNewPage = onNewPage;
            this.CurrentState = new WebCrawlerState(webRequestFactory);
            CurrentState.ToGo.Add(StartURL);
        }

        public void LoadState(string fileName)
        {
            CurrentState = Serializer.ReadJsonData<WebCrawlerState>(fileName);
        }

        public void SaveState(string fileName)
        {
            Serializer.WriteJsonData(CurrentState, fileName);
        }
        
        public void Stop()
        {
            Running = false;
        }

        public void Start()
        {
            if (IsDone)
                return;

            Running = true;

            if (crawlerThread.ThreadState == ThreadState.Running)
                return;

            crawlerThread = new Thread(Crawl);
            crawlerThread.Start();
        }

        private void Crawl()
        {
            while(Running && CurrentState.ToGo.Count > 0)
            {
                string currentSite = CurrentState.ToGo[0];

                string response = CurrentState.WebrequestFactory.GetResponse(currentSite);

                var linkParser = new Regex(@"href=[""'](?<url>(http|https)://[^/]*?\.[^/]*?\)(/.*)?[""']", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                var matches = linkParser.Matches(response);

                foreach (Match m in matches)
                {
                    string url = m.Value.Replace("href='", "").Replace("href=\"", "").Split('\'', '\"')[0];

                    Console.WriteLine(url);

                    string domainBasedUrl = url.Replace("http://", "").Replace("https://", "").Replace("www.", "");

                    if (!CurrentState.VisitedPages.ContainsKey(url) && (from start in Prefixes where domainBasedUrl.StartsWith(start) select true).Any())
                    {
                        CurrentState.ToGo.Add(url);

                        if (!OnNewPage(url, this))
                        {
                            Running = false;
                            return;
                        }

                        CurrentState.VisitedPages.Add(currentSite, true);

                    }
                }

                CurrentState.ToGo.RemoveAt(0);
            }

            if (CurrentState.ToGo.Count == 0)
                IsDone = true;
        }

        [Serializable]
        public class WebCrawlerState : NullCheckable
        {
            public AVLHashMap<string, bool> VisitedPages;
            public List<string> ToGo;
            public WebRequestFactory WebrequestFactory;

            public WebCrawlerState()
            {
                VisitedPages = new AVLHashMap<string, bool>();
                ToGo = new List<string>();
            }

            public WebCrawlerState(WebRequestFactory webrequestFactory) : this()
            {
                WebrequestFactory = webrequestFactory;
            }
        }
    }
}
