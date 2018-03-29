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
using LamestWebserver.WebServices.Generators;

namespace LamestWebserver.WebServices
{
    public class WebServiceHandler
    {
        private static WebServiceHandler _currentServiceHandler;
        private static UsableMutexSlim _mutex = new UsableMutexSlim();

        public static WebServiceHandler CurrentServiceHandler
        {
            get
            {
                using (_mutex.Lock())
                    if (_currentServiceHandler == null)
                        _currentServiceHandler = new WebServiceHandler();

                return _currentServiceHandler;
            }
        }

        private UsableMutexSlim _listMutex = new UsableMutexSlim();
        private Dictionary<Type, object> RequestWebServiceVariants = new Dictionary<Type, object>();
        private Dictionary<Type, object> ServerWebServiceVariants = new Dictionary<Type, object>();
        private Dictionary<Type, object> LocalWebServiceVariants = new Dictionary<Type, object>();

        private AVLHashMap<string, IPEndPoint> UrlToServerHashMap = new AVLHashMap<string, IPEndPoint>();

        public T GetLocalService<T>() where T : IWebService, new()
        {
            return (T)GetLocalService(typeof(T));
        }

        public object GetLocalService(Type type)
        {
            if (!type.GetInterfaces().Contains(typeof(IWebService)))
                throw new IncompatibleTypeException($"Type '{type}' is not compatible with {nameof(WebServiceHandler)}: Does not implement '{nameof(IWebService)}'.");

            if (type.GetConstructor(new Type[0]) == null)
                throw new IncompatibleTypeException($"Type '{type}' is not compatible with {nameof(WebServiceHandler)}: No empty constructor available.");

            if (type.IsAbstract || type.IsInterface || !type.IsPublic || type.IsSealed)
                throw new IncompatibleTypeException("Only public non-abstract non-sealed Types of classes can be WebServices.");

            if (LocalWebServiceVariants.ContainsKey(type))
            {
                return LocalWebServiceVariants[type];
            }
            else
            {
                object ret = WebServiceImplGenerator.GetWebServiceLocalImpl(type);

                LocalWebServiceVariants.Add(type, ret);

                return ret;
            }
        }

        public T GetServerService<T>() where T : IWebService, new()
        {
            return default(T);
        }
        public T GetRequestService<T>() where T : IWebService, new()
        {
            return default(T);
        }

        public WebServiceResponse Request(WebServiceRequest webServiceRequest)
        {
            IPEndPoint endPoint = UrlToServerHashMap[webServiceRequest.Namespace + "." + webServiceRequest.Type];

            if (endPoint == null)
            {
                try
                {
                    Type type = Type.GetType(webServiceRequest.Namespace + "." + webServiceRequest.Type);
                    object ws = GetLocalService(type);
                    var method = type.GetMethod(webServiceRequest.Method, webServiceRequest._parameterTypes);

                    try
                    {
                        method.Invoke(ws, webServiceRequest.Parameters);
                    }
                    catch(WebServiceException)
                    {
                        throw;
                    }
                    catch(Exception e)
                    {
                        throw new RemoteException($"Failed to execute method '{webServiceRequest.Namespace}.{webServiceRequest.Type}.{webServiceRequest.Method}'.", e);
                    }
                }
                catch (WebServiceException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    throw new IncompatibleTypeException($"Type '{webServiceRequest.Namespace}.{webServiceRequest.Type}' could not be created.", e);
                }
            }
            else
            {
                
            }

            return WebServiceResponse.Exception(new ServiceNotAvailableException("test test test 123"));
        }
    }
}
