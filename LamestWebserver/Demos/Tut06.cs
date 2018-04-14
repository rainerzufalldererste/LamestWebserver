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
            TestWebService wst = WebServiceHandler.CurrentServiceHandler.Instance.GetRequesterService<TestWebService>();

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
                new HHeadline("LamestWebserver WebServices"),
                new HText("This response runs various methods of a Test WebService. Look at the Logger output for this file to get a propper understanding of the WebService functions, that were actually executed."),
                new HHeadline("Executed Functions", 2),
                new HTable(from field in wst.GetType().GetFields() where field.DeclaringType == typeof(TestWebService) && field.FieldType == typeof(bool) select new List<object> { field.Name, ((bool)field.GetValue(wst)) ? "✔️" : "❌" })
            }, nameof(Tut06) + ".cs");
        }
    }

    public class TestWebService : IWebService
    {
        [WebServiceIgnore]
        public bool Constructor, VoidMethod, VoidMethodWithParameters, ReturningMethod, ReturningMethodWithParameters, ExceptionMethodVoid, ReturningExceptionMethod;

        public TestWebService()
        {
            Logger.LogTrace($"Called Constructor of {nameof(TestWebService)}.");
            Constructor = true;
        }

        public virtual void CallSomethingVoid()
        {
            string hello = "world";
            Logger.LogInformation(nameof(hello));
            Logger.LogInformation(hello);
            VoidMethod = true;
        }

        public virtual string CallSomethingReturn()
        {
            string wello = "horld";
            Logger.LogInformation(nameof(wello));
            Logger.LogInformation(wello);

            ReturningMethod = true;
            return wello;
        }

        public virtual void CallSomethingParamsVoid(string value)
        {
            while (value.Length > 0)
            {
                Logger.LogInformation(value);
                value = value.Remove(0, 1);
            }

            VoidMethodWithParameters = true;
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

            ReturningMethodWithParameters = true;

            return ret;
        }

        public virtual void ExceptVoid()
        {
            ExceptionMethodVoid = true;
            throw new Exception("Test Exception");
        }

        public virtual string ExceptReturn()
        {
            ReturningExceptionMethod = true;
            throw new Exception("Test Exception Return.");
        }

        /// <summary>
        /// Templated Functions are currently not supported in WebServices.
        /// </summary>
        [WebServiceIgnore]
        public virtual T TemplatedFunction<T> (object obj)
        {
            return (T)obj;
        }
    }
}
