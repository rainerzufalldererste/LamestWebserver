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
        public WebServiceHelperClient() : base("websvc/client")
        {
        }

        /// <inheritdoc />
        protected override HElement GetElement(SessionData sessionData)
        {
            var client0 = MethodBase.GetMethodFromHandle(typeof(WebServiceTestClient).GetMethod("CallSomethingVoid").MethodHandle).GetMethodBody();
            var client1 = MethodBase.GetMethodFromHandle(typeof(WebServiceTestClient).GetMethod("CallSomethingReturn").MethodHandle).GetMethodBody();
            var client2 = MethodBase.GetMethodFromHandle(typeof(WebServiceTestClient).GetMethod("CallSomethingParamsVoid").MethodHandle).GetMethodBody();
            var client3 = MethodBase.GetMethodFromHandle(typeof(WebServiceTestClient).GetMethod("CallSomethingParamsReturn").MethodHandle).GetMethodBody();

            return MainPage.GetPage(new List<HElement>()
            {
                new HText(client0.ToString()),
                new HText(client1.ToString()),
                new HText(client2.ToString()),
                new HText(client3.ToString()),
            }, nameof(WebServiceTest) + ".cs");
        }
    }
    public class WebServiceHelperServer : ElementResponse
    {
        /// <inheritdoc />
        public WebServiceHelperServer() : base("websvc/server")
        {
        }

        /// <inheritdoc />
        protected override HElement GetElement(SessionData sessionData)
        {
            var client0 = MethodBase.GetMethodFromHandle(typeof(WebServiceTestServer).GetMethod("CallSomethingVoidServer").MethodHandle).GetMethodBody();
            var client1 = MethodBase.GetMethodFromHandle(typeof(WebServiceTestServer).GetMethod("CallSomethingReturnServer").MethodHandle).GetMethodBody();
            var client2 = MethodBase.GetMethodFromHandle(typeof(WebServiceTestServer).GetMethod("CallSomethingParamsVoidServer").MethodHandle).GetMethodBody();
            var client3 = MethodBase.GetMethodFromHandle(typeof(WebServiceTestServer).GetMethod("CallSomethingParamsReturnServer").MethodHandle).GetMethodBody();

            return MainPage.GetPage(new List<HElement>()
            {
                new HText(client0.ToString()),
                new HText(client1.ToString()),
                new HText(client2.ToString()),
                new HText(client3.ToString()),
            }, nameof(WebServiceTest) + ".cs");
        }
    }

    public class WebServiceTest : LamestWebserver.WebServices.IWebService
    {
        /// <inheritdoc />
        public string URL => "testURL";

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

    public class WebServiceTestServer : WebServiceTest
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

    public class WebServiceTestClient : WebServiceTest
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
