using System;
using System.Net;
using LamestWebserver;
using LamestWebserver.Core.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LamestWebserver.Collections;
using System.Collections.Generic;
using System.Threading;

namespace UnitTests
{
    [TestClass]
    public class WebCrawlerTest
    {
        class TestWebRequestFactory : WebRequestFactory
        {
            public AVLHashMap<string, string> PremadeResponses;

            public TestWebRequestFactory() : base(false)
            {
                PremadeResponses = new AVLHashMap<string, string>()
                {
                    {
                        "http://www.bla.com/",
                        "<html><head></head><body><a href='http://www.bla.com/blob'>hello</a><b>Test</b><a href=\"http://www.bla.com/xyz\">abcd</a><a href='http://www.xyz.com/123'>hello</a></body></html>"
                    },
                    {
                        "http://www.bla.com/blob",
                        "<html><head></head><body><a href='http://www.bla.com/'>back</a><b>Test</b><a href=\"http://www.bla.com/xyz\">abcd</a><a href='http://www.bla.com/secure/'>hello</a></body></html>"
                    },
                    {
                        "http://www.bla.com/xyz",
                        "<html><head></head><body><a href='http://www.bla.com/blob'>hello</a><b>Test</b><a href=\"http://www.bla.com/xyz\">abcd</a><a href='http://www.xyz.com/123'>hello</a></body></html>"
                    },
                    {
                        "http://www.bla.com/secure/",
                        "<html><head></head><body><a href='http://www.xyz.com/123'>hello</a></body></html>"
                    },
                };
            }

            public override string GetResponse(string URL, out HttpStatusCode statusCode, int maxRedirects = 10)
            {
                string ret = PremadeResponses[URL];

                if (ret == null)
                    statusCode = HttpStatusCode.NotFound;
                else
                    statusCode = HttpStatusCode.OK;

                return ret;
            }
        }

        [TestMethod]
        public void TestWebCrawler()
        {
            List<string> urls = new List<string>();
            WebCrawler wc = new WebCrawler("http://www.bla.com/", "bla.com",
                (string foundUrl, WebCrawler w) =>
                {
                    urls.Add(foundUrl);
                    return true;
                },
                e =>
                {
                    Assert.Fail(e.Message); return false;
                },
                new TestWebRequestFactory()).Start();

            while (!wc.IsDone)
                Thread.Sleep(25);

            Assert.AreEqual(3, urls.Count);
            Assert.AreEqual("http://www.bla.com/blob", urls[0]);
            Assert.AreEqual("http://www.bla.com/xyz", urls[1]);
            Assert.AreEqual("http://www.bla.com/secure/", urls[2]);

            int fails = 0;

            WebCrawler wc0 = new WebCrawler("http://blob.com/", (string)null, (e,f) => { Assert.Fail(); return false; }, e => { fails++; return true; }, new TestWebRequestFactory(), 8).Start();

            while (!wc0.IsDone)
                Thread.Sleep(25);

            Assert.AreEqual(1, fails);
        }
    }
}
