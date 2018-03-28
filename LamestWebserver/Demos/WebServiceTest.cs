using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LamestWebserver.WebServices;
using System.CodeDom;
using System.Reflection;
using System.Reflection.Emit;
using LamestWebserver;
using LamestWebserver.UI;
using LamestWebserver.Core;

namespace Demos
{
    public class WebServiceHelperClient : ElementResponse
    {
        /// <inheritdoc />
        public WebServiceHelperClient() : base("websvc/requester")
        {
            Logger.CurrentLogger.Instance.MinimumLoggingLevel = Logger.ELoggingLevel.Trace;
        }

        /// <inheritdoc />
        protected override HElement GetElement(SessionData sessionData)
        {
            WebServiceTest wst = WebServiceHandler.CurrentServiceHandler.GetService<WebServiceTest>();

            wst.CallSomethingVoid();
            Logger.LogInformation(wst.CallSomethingReturn());
            wst.CallSomethingParamsVoid("hello");
            Logger.LogInformation(wst.CallSomethingParamsReturn("LamestWebserver"));

            return MainPage.GetPage(new List<HElement>()
            {
                new HHeadline("Success!"),
            }, nameof(WebServiceTest) + ".cs");
        }
    }

    public class WebServiceTest : LamestWebserver.WebServices.IWebService
    {
        public virtual void CallSomethingVoid()
        {
            string hello = "world";
            Console.WriteLine(nameof(hello));
            Console.WriteLine(hello);
        }

        public virtual string CallSomethingReturn()
        {
            string wello = "horld";
            Console.WriteLine(nameof(wello));
            Console.WriteLine(wello);

            return wello;
        }

        public virtual void CallSomethingParamsVoid(string value)
        {
            while (value.Length > 0)
            {
                Console.WriteLine(value);
                value = value.Remove(0, 1);
            }
        }

        public virtual string CallSomethingParamsReturn(string value)
        {
            string ret = "";

            while (value.Length > 0)
            {
                Console.WriteLine(ret + " | " + value);
                ret += value[0];
                value = value.Remove(0, 1);
            }

            Console.WriteLine(ret + " | " + value);

            return ret;
        }
    }
}
