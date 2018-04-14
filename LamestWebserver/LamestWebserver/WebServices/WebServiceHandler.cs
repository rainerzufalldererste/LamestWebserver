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
using LamestWebserver.Core;

namespace LamestWebserver.WebServices
{
    public class WebServiceHandler
    {
        public static readonly Singleton<WebServiceHandler> CurrentServiceHandler = new Singleton<WebServiceHandler>(() => new WebServiceHandler());

        private UsableWriteLock _listLock = new UsableWriteLock();
        private Dictionary<Type, object> RequestertWebServiceVariants = new Dictionary<Type, object>();
        private Dictionary<Type, object> LocalWebServiceVariants = new Dictionary<Type, object>();
        private AVLHashMap<string, IPEndPoint> UrlToServerHashMap = new AVLHashMap<string, IPEndPoint>();

        public void RegisterTypeRemoteEndpoint(Type type, IPEndPoint remoteEndpoint)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            if (remoteEndpoint == null)
                throw new ArgumentNullException(nameof(remoteEndpoint));

            string typename = GetTypename(type);
            IPEndPoint original = UrlToServerHashMap[typename];

            if (original != null)
                Logger.LogWarning($"Type '{typename}' will be unmapped from '{original.Address}:{original.Port}' to '{remoteEndpoint.Address}:{remoteEndpoint.Port}'.");

            UrlToServerHashMap[typename] = remoteEndpoint;

            Logger.LogInformation($"Type '{typename}' has been mapped to '{remoteEndpoint.Address}:{remoteEndpoint.Port}'.");
        }

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

            bool contained = false;
            
            using (_listLock.LockRead())
                contained = LocalWebServiceVariants.ContainsKey(type);

            if (contained)
            {
                return LocalWebServiceVariants[type];
            }
            else
            {
                object ret = WebServiceImplementationGenerator.GetWebServiceLocalImplementation(type);

                using(_listLock.LockWrite())
                    LocalWebServiceVariants.Add(type, ret);

                return ret;
            }
        }
        
        public T GetRequesterService<T>() where T : IWebService, new()
        {
            return (T)GetRequesterService(typeof(T));
        }

        public object GetRequesterService(Type type)
        {
            if (!type.GetInterfaces().Contains(typeof(IWebService)))
                throw new IncompatibleTypeException($"Type '{type}' is not compatible with {nameof(WebServiceHandler)}: Does not implement '{nameof(IWebService)}'.");

            if (type.GetConstructor(new Type[0]) == null)
                throw new IncompatibleTypeException($"Type '{type}' is not compatible with {nameof(WebServiceHandler)}: No empty constructor available.");

            if (type.IsAbstract || type.IsInterface || !type.IsPublic || type.IsSealed)
                throw new IncompatibleTypeException("Only public non-abstract non-sealed Types of classes can be WebServices.");

            bool contained = false;

            using (_listLock.LockRead())
                contained = RequestertWebServiceVariants.ContainsKey(type);

            if (contained)
            {
                return RequestertWebServiceVariants[type];
            }
            else
            {
                object ret = WebServiceImplementationGenerator.GetWebServiceRequestImplementation(type);

                using (_listLock.LockWrite())
                    RequestertWebServiceVariants.Add(type, ret);

                return ret;
            }
        }

        public WebServiceResponse Request(WebServiceRequest webServiceRequest)
        {
            string typename = webServiceRequest.Namespace + "." + webServiceRequest.Type;
            IPEndPoint endPoint = UrlToServerHashMap[typename];

            if (endPoint == null)
            {
                Logger.LogWarning($"The type '{typename}' has not been added to the WebServiceHandler yet and therefore could not be resolved. Trying to use local equivalent.");

                try
                {
                    Type type = Type.GetType(typename);

                    if (type == null)
                    {
                        var assemblies = AppDomain.CurrentDomain.GetAssemblies();

                        foreach (var assembly in assemblies)
                        {
                            type = assembly.GetType(typename);

                            if (type != null)
                                break;
                        }
                    }

                    if (type == null)
                        throw new IncompatibleTypeException($"Type '{typename}' could not be found.");

                    object ws = GetLocalService(type);
                    var method = ws.GetType().GetMethod(webServiceRequest.Method, webServiceRequest._parameterTypes);

                    try
                    {
                        var ret = method.Invoke(ws, webServiceRequest.Parameters);

                        if (method.ReturnType == typeof(WebServiceResponse))
                            return (WebServiceResponse)ret;
                        else
                            return WebServiceResponse.Exception(new IncompatibleTypeException($"Return type was not '{nameof(WebServiceResponse)}'."));
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

        internal static string GetTypename(Type type) => type.Namespace + "." + type.Name;
    }
}
