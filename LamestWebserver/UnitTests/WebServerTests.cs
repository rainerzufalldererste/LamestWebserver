using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LamestWebserver;
using LamestWebserver.Core.Web;
using LamestWebserver.UI;
using LamestWebserver.UI.CachedByDefault;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
    [TestClass]
    public class WebServerTests
    {
        private static readonly Dictionary<string, HElement> plainTextElementResponses = new Dictionary<string, HElement>
        {
            { "plaintext" , new HPlainText("plaintext") },
            { "string" , new HString("string") },
            { "special&lt;character&gt;string" , new HString("special<character>string") },
            { "subfolder/page" , new HString("subfolder/page") }
        };

        private static readonly Dictionary<HElement, IEnumerable<string>> complexRelementResponsesContain = new Dictionary<HElement, IEnumerable<string>>
        {
            { new CLink("link text", "link-to-page.com/hello?world"), new [] { ">link text<", "href='link-to-page.com/hello?world'", "<a ", "</a>" } },
            { new HLink("link text", "link-to-page.com/hello?world"), new [] { ">link text<", "href='link-to-page.com/hello?world'", "<a ", "</a>" } },
            { new CImage("link-to-page.com/hello.png"), new [] { "src='link-to-page.com/hello.png'", "<img ", ">" } },
            { new HImage("link-to-page.com/hello.png"), new [] { "src='link-to-page.com/hello.png'", "<img ", ">" } },
            { new CForm("/nextpage") { Elements = { new CButton("Enter", HButton.EButtonType.submit) } }, new [] { "action='/nextpage", "<form ", "</form>", "<button ", "</button>", ">Enter<", " type='submit'" } },
            { new HForm("/nextpage") { Elements = { new HButton("Enter", HButton.EButtonType.submit) } }, new [] { "action='/nextpage", "<form ", "</form>", "<button ", "</button>", ">Enter<", " type='submit'" } },
            { new CContainer(new CHeadline("head line!", 2)), new [] { "<div ", "</div>", "<h2 ", "</h2>", ">head line!<" } },
            { new HContainer(new HHeadline("head line!", 2)), new [] { "<div ", "</div>", "<h2 ", "</h2>", ">head line!<" } },
        };

        [TestMethod]
        public void TestWebServer()
        {
            const int webserverPort = 20202;

            using (var webserver = new WebServer(webserverPort))
            {
                foreach (var element in plainTextElementResponses)
                    new TestElementResponse(element.Key, element.Value);

                foreach (var element in plainTextElementResponses)
                    Assert.AreEqual(WebRequestFactory.GetResponseSimple($"http://localhost:{webserver.Port}/{element.Key}", out System.Net.HttpStatusCode statusCode), element.Key);

                int count = 0;

                foreach (var element in complexRelementResponsesContain)
                    new TestElementResponse($"{nameof(complexRelementResponsesContain)}.{count++}.{element.Key.GetType().Name}", element.Key);

                count = 0;

                foreach (var element in complexRelementResponsesContain)
                {
                    foreach (var value in element.Value)
                        Assert.IsTrue(WebRequestFactory.GetResponseSimple($"http://localhost:{webserver.Port}/{nameof(complexRelementResponsesContain)}.{count}.{element.Key.GetType().Name}", out System.Net.HttpStatusCode statusCode).Contains(value));

                    count++;
                }
            }

            Thread.Sleep(500);

            Assert.IsTrue(ServerCore.TcpPortIsUnused(webserverPort));
        }

        public class TestElementResponse : ElementResponse
        {
            private readonly HElement _response;

            public TestElementResponse(string URL, HElement response) : base(URL)
            {
                _response = response;
            }

            protected override HElement GetElement(SessionData sessionData) => _response;
        }
    }
}
