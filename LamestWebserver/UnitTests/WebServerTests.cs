using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LamestWebserver;
using LamestWebserver.Core.Web;
using LamestWebserver.UI;
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

        [TestMethod]
        public void TestWebServer()
        {
            using (var webserver = new WebServer(20202))
            {
                foreach (var element in plainTextElementResponses)
                    new TestElementResponse(element.Key, element.Value);

                foreach (var element in plainTextElementResponses)
                    Assert.AreEqual(WebRequestFactory.GetResponseSimple($"http://localhost:{webserver.Port}/{element.Key}", out System.Net.HttpStatusCode statusCode), element.Key);
            }
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
