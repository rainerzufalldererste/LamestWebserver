using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.CodeDom;
using System.Net;
using System.Reflection;
using LamestWebserver.Collections;
using LamestWebserver.Synchronization;

namespace LamestWebserver.WebServices
{
    public interface IWebService : IURLIdentifyable
    {
    }

    public abstract class WebServiceException : Exception
    {
        protected WebServiceException(string description) : base(description) { }
    }

    public class ServiceNotAvailableException : WebServiceException
    {
        /// <inheritdoc />
        public ServiceNotAvailableException(string description) : base(description)
        {
        }
    }

    public class IncompatibleTypeException : WebServiceException
    {
        /// <inheritdoc />
        public IncompatibleTypeException(string description) : base(description)
        {
        }
    }

    public class WebServiceIncompatibleException : WebServiceException
    {
        /// <inheritdoc />
        public WebServiceIncompatibleException(string description) : base(description)
        {
        }
    }

    [Serializable]
    public class WebServiceResponse
    {
        public object ReturnValue = null;
        public Exception ExceptionThrown = null;
        public WebServiceReturnType ReturnType = WebServiceReturnType.ReturnVoid;

        public static WebServiceResponse Return() => new WebServiceResponse();

        public static WebServiceResponse Return(object value)
            => new WebServiceResponse()
            {
                ReturnValue = value,
                ReturnType = WebServiceReturnType.ReturnValue
            };

        public static WebServiceResponse Exception(Exception exception)
            => new WebServiceResponse()
            {
                ExceptionThrown = exception,
                    ReturnType = WebServiceReturnType.ExceptionThrown
            };

    }

    public class WebServiceRequest
    {
        public string URL;
        public string Method;
        public object[] Parameters;

        public static WebServiceRequest Request(string URL, string Method, params object[] Parameters)
            => new WebServiceRequest()
            {
                Method = Method,
                URL = URL,
                Parameters = Parameters
            };
    }

    public enum WebServiceReturnType : byte
    {
        ReturnValue, ReturnVoid, ExceptionThrown
    }

    public class WebServiceHandler
    {
        private static WebServiceHandler _currentServiceHandler;
        private static Mutex _mutex = new Mutex();

        public static WebServiceHandler CurrentServiceHandler
        {
            get
            {
                _mutex.WaitOne();

                if (_currentServiceHandler == null)
                    _currentServiceHandler = new WebServiceHandler();

                _mutex.ReleaseMutex();

                return _currentServiceHandler;
            }
        }

        private UsableMutexSlim _listMutex = new UsableMutexSlim();
        private Dictionary<Type, object> ServerWebServiceVariants = new Dictionary<Type, object>();
        private Dictionary<Type, object> ClientWebServiceVariants = new Dictionary<Type, object>();

        private AVLHashMap<string, IPEndPoint> UrlToServerHashMap = new AVLHashMap<string, IPEndPoint>();

        public T GetService<T>() where T : IWebService, new()
        {
            if (typeof(T).IsAbstract || typeof(T).IsInterface || !typeof(T).IsPublic)
                throw new IncompatibleTypeException("Only public non-abstract Types of non-interfaces can be WebServices.");

            AssemblyBuilder asmBuilder = Thread.GetDomain()
                .DefineDynamicAssembly(new AssemblyName(typeof(IWebService).Namespace + "." + typeof(IWebService).Name + "." + typeof(T).Namespace + "." + typeof(T).Name),
                    AssemblyBuilderAccess.RunAndCollect | AssemblyBuilderAccess.RunAndSave);

            ModuleBuilder moduleBuilder =
                asmBuilder.DefineDynamicModule(typeof(IWebService).Namespace + "." + typeof(IWebService).Name + "." + typeof(T).Namespace + "." + typeof(T).Name);

            TypeBuilder typeBuilder = moduleBuilder.DefineType("GENERATED_WEBSVC__" + typeof(T).Namespace + "_" + typeof(T).Name, TypeAttributes.Public | TypeAttributes.Class,
                typeof(T));

            var constructorBuilder = typeBuilder.DefineDefaultConstructor(MethodAttributes.Public);
            var ilgen = constructorBuilder.GetILGenerator();
            ilgen.Emit(OpCodes.Call, typeof(T).GetConstructor(new Type[0]));
            ilgen.Emit(OpCodes.Ret);

            foreach (var method in typeof(T).GetMethods(BindingFlags.Public))
            {
                if (method.IsVirtual && method.IsPublic && !method.IsAbstract && !method.IsStatic && !method.IsFinal && !method.IsGenericMethod && !method.IsGenericMethodDefinition)
                {
                    MethodBuilder methBuilder = typeBuilder.DefineMethod(method.Name, MethodAttributes.Public | MethodAttributes.NewSlot | MethodAttributes.Virtual |
                                                                                      MethodAttributes.Final, method.ReturnType,
                        (from param in method.GetParameters() select param.GetType()).ToArray());

                    ILGenerator il = methBuilder.GetILGenerator();
                    il.Emit(OpCodes.Call, method);
                    il.Emit(OpCodes.Ret);

                    typeBuilder.DefineMethodOverride(methBuilder, method);
                }
            }

            Type resultType = typeBuilder.CreateType();

            var ret = resultType.GetConstructor(new Type[0]).Invoke(new object[0]);

            return (T)ret;
        }

        public T GetServerService<T>() where T : IWebService, new()
        {
            return default(T);
        }

        public WebServiceResponse Request(WebServiceRequest webServiceRequest)
        {
            IPEndPoint endPoint = UrlToServerHashMap[webServiceRequest.URL];

            if (endPoint == null)
            {

            }
            else
            {
                
            }

            return WebServiceResponse.Exception(new ServiceNotAvailableException("test test test 123"));
        }
    }
}
