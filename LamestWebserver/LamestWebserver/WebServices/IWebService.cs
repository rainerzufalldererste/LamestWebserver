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
    public abstract class IWebService : IURLIdentifyable
    {
        public string URL { get; }

        protected IWebService()
        {
            URL = this.GetType().FullName;
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
        private Dictionary<Type, object> RequesterWebServiceVariants = new Dictionary<Type, object>();
        private Dictionary<Type, object> ResponderWebServiceVariants = new Dictionary<Type, object>();

        private AVLHashMap<string, IPEndPoint> UrlToServerHashMap = new AVLHashMap<string, IPEndPoint>();

        public T GetService<T>() where T : IWebService, new()
        {
            if (typeof(T).IsAbstract || typeof(T).IsInterface || !typeof(T).IsPublic || typeof(T).IsSealed)
                throw new IncompatibleTypeException("Only public non-abstract non-sealed Types of classes can be WebServices.");

            AssemblyBuilder asmBuilder = Thread.GetDomain()
                .DefineDynamicAssembly(new AssemblyName(typeof(IWebService).Namespace + "." + typeof(IWebService).Name + "." + typeof(T).Namespace + "." + typeof(T).Name),
                    AssemblyBuilderAccess.RunAndCollect);

            ModuleBuilder moduleBuilder =
                asmBuilder.DefineDynamicModule(typeof(IWebService).Namespace + "." + typeof(IWebService).Name + "." + typeof(T).Namespace + "." + typeof(T).Name);

            TypeBuilder typeBuilder = moduleBuilder.DefineType(typeof(IWebService).Namespace + "." + typeof(IWebService).Name + "." + typeof(T).Namespace + "." + typeof(T).Name, TypeAttributes.Public | TypeAttributes.Class,
                typeof(T));

            if (typeof(T).GetConstructor(new Type[0]) == null)
            {
                var constructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Any, new Type[0]);
                var ilgen = constructorBuilder.GetILGenerator();
                ilgen.Emit(OpCodes.Call, typeof(T).GetConstructor(new Type[0]));
                ilgen.Emit(OpCodes.Ret);
            }
            else
            {
                typeBuilder.DefineDefaultConstructor(MethodAttributes.Public);
            }
            
            var methods = typeof(T).GetMethods();

            foreach (var method in methods)
            {
                if (method.IsVirtual && method.IsPublic && !method.IsAbstract && !method.IsStatic && !method.IsFinal && !method.IsGenericMethod && !method.IsGenericMethodDefinition)
                {
                    try
                    {
                        GetRequesterMethod(typeBuilder, method);
                    }
                    catch (Exception e)
                    {
                        ServerHandler.LogMessage($"Failed to get Service for '{method.Name}'.\n" + e);
                    }
                }
            }

            Type resultType = typeBuilder.CreateType();

            var ret = resultType.GetConstructor(new Type[0]).Invoke(new object[0]);

            return (T)ret;
        }

        public void GetRequesterMethod(TypeBuilder typeBuilder, MethodInfo method)
        {
            MethodBuilder methBuilder = typeBuilder.DefineMethod(method.Name, MethodAttributes.Public | MethodAttributes.HideBySig | 
                                                                              MethodAttributes.Final, method.ReturnType,
                (from param in method.GetParameters() select param.GetType()).ToArray());

            ILGenerator il = methBuilder.GetILGenerator();
            il.Emit(OpCodes.Nop);
            il.Emit(OpCodes.Call, method);
            il.Emit(OpCodes.Newobj, typeof(WebServiceRequest).GetConstructors().First());
            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, typeof(IWebService).GetMethod("get_" + nameof(IWebService.URL)));
            il.Emit(OpCodes.Stfld, typeof(WebServiceRequest).GetField(nameof(IWebService.URL)));
            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Call, typeof(MethodBase).GetMethod(nameof(MethodBase.GetCurrentMethod)));
            il.Emit(OpCodes.Callvirt, typeof(MemberInfo).GetMethod("get_" + nameof(MemberInfo.Name)));
            il.Emit(OpCodes.Stfld, typeof(WebServiceRequest).GetField(nameof(WebServiceRequest.Method)));
            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Stfld, typeof(WebServiceRequest).GetField(nameof(WebServiceRequest.Parameters)));
            il.Emit(OpCodes.Callvirt, typeof(WebServiceHandler).GetMethod(nameof(WebServiceHandler.Request)));
            il.Emit(OpCodes.Pop);
            il.Emit(OpCodes.Ret);

            typeBuilder.DefineMethodOverride(methBuilder, method);
        }

        public T GetResponderService<T>() where T : IWebService, new()
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
