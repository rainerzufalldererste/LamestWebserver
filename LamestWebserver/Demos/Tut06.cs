using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LamestWebserver.WebServices;
using LamestWebserver;
using LamestWebserver.UI;
using LamestWebserver.Core;

namespace Demos
{
    public class Tut06 : ElementResponse
    {
        /// <inheritdoc />
        public Tut06() : base(nameof(Tut06))
        {
            Logger.CurrentLogger.Instance.MinimumLoggingLevel = Logger.ELoggingLevel.Trace;
        }

        /// <inheritdoc />
        protected override HElement GetElement(SessionData sessionData)
        {
            TestWebService wst = WebServiceHandler.CurrentServiceHandler.Instance.GetLocalService<TestWebService>();

            wst.CallSomethingVoid();
            Logger.LogInformation(wst.CallSomethingReturn());
            wst.CallSomethingParamsVoid("hello");
            Logger.LogInformation(wst.CallSomethingParamsReturn("LamestWebserver"));

            try
            {
                wst.ExceptVoid();

                Logger.LogExcept("THIS SHOULD NOT BE THROWN.");
            }
            catch (RemoteException e)
            {
                Logger.LogError(e.SafeToString());
            }

            try
            {
                wst.ExceptReturn();

                Logger.LogExcept("THIS SHOULD NOT BE THROWN.");
            }
            catch (RemoteException e)
            {
                Logger.LogError(e.SafeToString());
            }

            return MainPage.GetPage(new List<HElement>()
            {
                new HHeadline("Success!"),
                new HText("This response runs various methods of a Test WebService. Look at the Logger output for this file to get a propper understanding of the WebService functions, that were actually executed."),
            }, nameof(Tut06) + ".cs");
        }
    }

    public class TestWebService : IWebService
    {
        public TestWebService()
        {
            Logger.LogTrace($"Called Constructor of {nameof(TestWebService)}.");
        }

        public virtual void CallSomethingVoid()
        {
            string hello = "world";
            Logger.LogInformation(nameof(hello));
            Logger.LogInformation(hello);
        }

        public virtual string CallSomethingReturn()
        {
            string wello = "horld";
            Logger.LogInformation(nameof(wello));
            Logger.LogInformation(wello);

            return wello;
        }

        public virtual void CallSomethingParamsVoid(string value)
        {
            while (value.Length > 0)
            {
                Logger.LogInformation(value);
                value = value.Remove(0, 1);
            }
        }

        public virtual string CallSomethingParamsReturn(string value)
        {
            string ret = "";

            while (value.Length > 0)
            {
                Logger.LogInformation(ret + " | " + value);
                ret += value[0];
                value = value.Remove(0, 1);
            }

            Logger.LogInformation(ret + " | " + value);

            return ret;
        }

        public virtual void ExceptVoid()
        {
            throw new Exception("Test Exception");
        }

        public virtual string ExceptReturn()
        {
            throw new Exception("Test Exception Return.");
        }
    }
}
