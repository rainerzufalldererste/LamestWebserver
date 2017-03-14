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

namespace Demos
{
    public class WebServiceHelperClient : ElementResponse
    {
        /// <inheritdoc />
        public WebServiceHelperClient() : base("websvc/requester")
        {
            SDILReader.Globals.LoadOpCodes();
        }

        /// <inheritdoc />
        protected override HElement GetElement(SessionData sessionData)
        {
            var client0 = new SDILReader.MethodBodyReader(typeof(WebServiceTestRequester).GetMethod("CallSomethingVoid")).GetBodyCode();
            var client1 = new SDILReader.MethodBodyReader(typeof(WebServiceTestRequester).GetMethod("CallSomethingReturn")).GetBodyCode();
            var client2 = new SDILReader.MethodBodyReader(typeof(WebServiceTestRequester).GetMethod("CallSomethingParamsVoid")).GetBodyCode();
            var client3 = new SDILReader.MethodBodyReader(typeof(WebServiceTestRequester).GetMethod("CallSomethingParamsReturn")).GetBodyCode();

            var url = new WebServiceTestRequester().URL;

            var product = WebServiceHandler.CurrentServiceHandler.GetService<WebServiceTest>();

            return MainPage.GetPage(new List<HElement>()
            {
                new HHeadline(url),
                new HHeadline(nameof(WebServiceTestRequester.CallSomethingVoid), 2),
                new HText(client0) {Class = "code"},
                new HHeadline(nameof(WebServiceTestRequester.CallSomethingReturn), 2),
                new HText(client1) {Class = "code"},
                new HHeadline(nameof(WebServiceTestRequester.CallSomethingParamsVoid), 2),
                new HText(client2) {Class = "code"},
                new HHeadline(nameof(WebServiceTestRequester.CallSomethingParamsReturn), 2),
                new HText(client3) {Class = "code"},
            }, nameof(WebServiceTest) + ".cs");
        }
    }
    public class WebServiceHelperServer : ElementResponse
    {
        /// <inheritdoc />
        public WebServiceHelperServer() : base("websvc/responder")
        {
        }

        /// <inheritdoc />
        protected override HElement GetElement(SessionData sessionData)
        {
            var server0 = new SDILReader.MethodBodyReader(typeof(WebServiceTestResponder).GetMethod("CallSomethingVoidServer")).GetBodyCode();
            var server1 = new SDILReader.MethodBodyReader(typeof(WebServiceTestResponder).GetMethod("CallSomethingReturnServer")).GetBodyCode();
            var server2 = new SDILReader.MethodBodyReader(typeof(WebServiceTestResponder).GetMethod("CallSomethingParamsVoidServer")).GetBodyCode();
            var server3 = new SDILReader.MethodBodyReader(typeof(WebServiceTestResponder).GetMethod("CallSomethingParamsReturnServer")).GetBodyCode();

            var url = new WebServiceTestResponder().URL;

            return MainPage.GetPage(new List<HElement>()
            {
                new HHeadline(url),
                new HHeadline(nameof(WebServiceTestResponder.CallSomethingVoidServer), 2),
                new HText(server0) {Class = "code"},
                new HHeadline(nameof(WebServiceTestResponder.CallSomethingReturnServer), 2),
                new HText(server1) {Class = "code"},
                new HHeadline(nameof(WebServiceTestResponder.CallSomethingParamsVoidServer), 2),
                new HText(server2) {Class = "code"},
                new HHeadline(nameof(WebServiceTestResponder.CallSomethingParamsReturnServer), 2),
                new HText(server3) {Class = "code"},
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

    public class WebServiceTestResponder : WebServiceTest
    {
        public WebServiceResponse CallSomethingVoidServer(WebServiceRequest request)
        {
            try
            {
                base.CallSomethingVoid();
                return WebServiceResponse.Return();
            }
            catch (Exception e)
            {
                return WebServiceResponse.Exception(e);
            }
        }

        public WebServiceResponse CallSomethingReturnServer(WebServiceRequest request)
        {
            try
            {
                var ret = base.CallSomethingReturn();
                return WebServiceResponse.Return(ret);
            }
            catch (Exception e)
            {
                return WebServiceResponse.Exception(e);
            }
        }

        public WebServiceResponse CallSomethingParamsVoidServer(WebServiceRequest request)
        {
            try
            {
                Action<string> action = base.CallSomethingParamsVoid;
                action.DynamicInvoke(request.Parameters);

                return WebServiceResponse.Return();
            }
            catch (Exception e)
            {
                return WebServiceResponse.Exception(e);
            }
        }

        public WebServiceResponse CallSomethingParamsReturnServer(WebServiceRequest request)
        {
            try
            {
                Func<string, string> func = base.CallSomethingParamsReturn;
                var ret = func.DynamicInvoke(request.Parameters);

                return WebServiceResponse.Return(ret);
            }
            catch (Exception e)
            {
                return WebServiceResponse.Exception(e);
            }
        }
    }

    public class WebServiceTestRequester : WebServiceTest
    {
        public override void CallSomethingVoid()
        {
            WebServiceHandler.CurrentServiceHandler.Request(
                new WebServiceRequest()
                {
                    URL = base.URL,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Parameters = null
                });
        }
        
        public override string CallSomethingReturn()
        {
            WebServiceResponse response = WebServiceHandler.CurrentServiceHandler.Request(
                new WebServiceRequest()
            {
                URL = base.URL,
                Method = MethodBase.GetCurrentMethod().Name,
                Parameters = null
            });

            switch (response.ReturnType)
            {
                case WebServiceReturnType.ReturnValue:
                    return (string) response.ReturnValue;

                case WebServiceReturnType.ReturnVoid:
                    throw new WebServiceIncompatibleException("The called method returned void - but a return value was expected.");

                case WebServiceReturnType.ExceptionThrown:
                    throw response.ExceptionThrown;

                default:
                    throw new WebServiceIncompatibleException($"The used WebServiceReturnType {response.ReturnType} hasn't been handled correctly.");
            }
        }
        
        public override void CallSomethingParamsVoid(string value)
        {
            WebServiceResponse response = WebServiceHandler.CurrentServiceHandler.Request(
                new WebServiceRequest()
                {
                    URL = base.URL,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Parameters = new object[] {value}
                });

            switch (response.ReturnType)
            {
                case WebServiceReturnType.ReturnVoid:
                    return;

                case WebServiceReturnType.ReturnValue:
                    throw new WebServiceIncompatibleException("The called method returned a value - but no return value was expected.");

                case WebServiceReturnType.ExceptionThrown:
                    throw response.ExceptionThrown;

                default:
                    throw new WebServiceIncompatibleException($"The used WebServiceReturnType {response.ReturnType} hasn't been handled correctly.");
            }
        }
        
        public override string CallSomethingParamsReturn(string value)
        {
            WebServiceResponse response = WebServiceHandler.CurrentServiceHandler.Request(
                new WebServiceRequest()
                {
                    URL = base.URL,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Parameters = new object[] {value}
                });

            switch (response.ReturnType)
            {
                case WebServiceReturnType.ReturnValue:
                    return (string)response.ReturnValue;

                case WebServiceReturnType.ReturnVoid:
                    throw new WebServiceIncompatibleException("The called method returned void - but a return value was expected.");

                case WebServiceReturnType.ExceptionThrown:
                    throw response.ExceptionThrown;

                default:
                    throw new WebServiceIncompatibleException($"The used WebServiceReturnType {response.ReturnType} hasn't been handled correctly.");
            }
        }
    }
}
