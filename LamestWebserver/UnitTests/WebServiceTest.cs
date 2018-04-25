using LamestWebserver.Core;
using LamestWebserver.WebServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests
{
    [TestClass]
    public class WebServiceTest
    {
        [TestMethod]
        public void TestWebServices()
        {
            WebServiceHandler serverWebServiceHandler = new WebServiceHandler();
            TestWebService clientImplementation = WebServiceHandler.CurrentServiceHandler.Instance.GetRequesterService<TestWebService>();
            TestWebService serverImplementation = serverWebServiceHandler.GetLocalService<TestWebService>();

            using (WebServiceServer webServiceServer = new WebServiceServer(serverWebServiceHandler))
            {
                WebServiceHandler.CurrentServiceHandler.Instance.AssignRemoteEndpointToType(typeof(TestWebService), new IPEndPoint((from addr in Dns.GetHostEntry(Dns.GetHostName()).AddressList where addr.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork select addr).First(), webServiceServer.Port));

                clientImplementation.CallSomethingVoid();
                Assert.AreEqual(nameof(clientImplementation.CallSomethingReturn), clientImplementation.CallSomethingReturn());
                clientImplementation.CallSomethingParamsVoid(nameof(clientImplementation.CallSomethingParamsVoid));
                Assert.AreEqual(nameof(TestWebService), clientImplementation.CallSomethingParamsReturn(nameof(clientImplementation.CallSomethingParamsReturn)));
                clientImplementation.CallSomethingDefaultParamsVoid(nameof(clientImplementation.CallSomethingDefaultParamsVoid));
                Assert.AreEqual(nameof(TestWebService), clientImplementation.CallSomethingDefaultParamsReturn(nameof(clientImplementation.CallSomethingDefaultParamsReturn)));

                try
                {
                    clientImplementation.ExceptVoid();
                    Assert.Fail();
                }
                catch (RemoteException e)
                {
                    Assert.IsTrue(e.InnerException is RemoteException);
                }

                try
                {
                    clientImplementation.ExceptReturn();
                    Assert.Fail();
                }
                catch (RemoteException e)
                {
                    Assert.IsTrue(e.InnerException is RemoteException);
                }
            }

            Assert.IsTrue(serverImplementation.Constructor);
            Assert.IsTrue(clientImplementation.Constructor); // Yes, true. We have to inherit from the constructor.

            Assert.IsTrue(serverImplementation.VoidMethod);
            Assert.IsFalse(clientImplementation.VoidMethod);

            Assert.IsTrue(serverImplementation.VoidMethodWithParameters);
            Assert.IsFalse(clientImplementation.VoidMethodWithParameters);

            Assert.IsTrue(serverImplementation.ReturningMethod);
            Assert.IsFalse(clientImplementation.ReturningMethod);

            Assert.IsTrue(serverImplementation.ReturningMethodWithParameters);
            Assert.IsFalse(clientImplementation.ReturningMethodWithParameters);

            Assert.IsTrue(serverImplementation.ExceptionMethodVoid);
            Assert.IsFalse(clientImplementation.ExceptionMethodVoid);

            Assert.IsTrue(serverImplementation.ReturningExceptionMethod);
            Assert.IsFalse(clientImplementation.ReturningExceptionMethod);

            Assert.IsTrue(serverImplementation.VoidMethodWithDefaultParameters);
            Assert.IsFalse(clientImplementation.VoidMethodWithDefaultParameters);

            Assert.IsTrue(serverImplementation.ReturningMethodWithDefaultParameters);
            Assert.IsFalse(clientImplementation.ReturningMethodWithDefaultParameters);
        }
    }
    public class TestWebService : IWebService
    {
        [WebServiceIgnore]
        public bool Constructor, VoidMethod, VoidMethodWithParameters, ReturningMethod, ReturningMethodWithParameters, ExceptionMethodVoid, ReturningExceptionMethod, VoidMethodWithDefaultParameters, ReturningMethodWithDefaultParameters;

        public TestWebService()
        {
            Constructor = true;
        }

        public virtual void CallSomethingVoid()
        {
            VoidMethod = true;
        }

        public virtual string CallSomethingReturn()
        {
            ReturningMethod = true;
            return nameof(CallSomethingReturn);
        }

        public virtual void CallSomethingParamsVoid(string value)
        {
            Assert.AreEqual(value, nameof(CallSomethingParamsVoid));
            VoidMethodWithParameters = true;
        }

        public virtual string CallSomethingParamsReturn(string value)
        {
            Assert.AreEqual(value, nameof(CallSomethingParamsReturn));
            ReturningMethodWithParameters = true;

            return nameof(TestWebService);
        }

        public virtual void ExceptVoid()
        {
            ExceptionMethodVoid = true;
            throw new Exception(nameof(ExceptVoid));
        }

        public virtual string ExceptReturn()
        {
            ReturningExceptionMethod = true;
            throw new Exception(nameof(ExceptReturn));
        }

        public virtual void CallSomethingDefaultParamsVoid(string a, string b = "b", string c = "c")
        {
            Assert.AreEqual(nameof(CallSomethingDefaultParamsVoid), a);
            Assert.AreEqual(nameof(b), b);
            Assert.AreEqual(nameof(c), c);

            VoidMethodWithDefaultParameters = true;
            return;
        }

        public virtual string CallSomethingDefaultParamsReturn(string a, string b = "b", string c = "c")
        {
            Assert.AreEqual(nameof(CallSomethingDefaultParamsReturn), a);
            Assert.AreEqual(nameof(b), b);
            Assert.AreEqual(nameof(c), c);

            ReturningMethodWithDefaultParameters = true;
            return nameof(TestWebService);
        }

        /// <summary>
        /// Templated Functions are currently not supported in WebServices.
        /// </summary>
        [WebServiceIgnore]
        public virtual T TemplatedFunction<T>(object obj)
        {
            return (T)obj;
        }
    }
}
