using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using LamestWebserver.Collections;
using LamestWebserver.Synchronization;
using LamestWebserver.WebServices.Generators;
using LamestWebserver.Core;

namespace LamestWebserver.WebServices
{
    /// <summary>
    /// A WebServiceHandler communicates with Local &amp; Remote WebServices.
    /// </summary>
    public class WebServiceHandler : NullCheckable
    {
        /// <summary>
        /// The WebServiceHandler Singleton.
        /// </summary>
        public static readonly Singleton<WebServiceHandler> CurrentServiceHandler = new Singleton<WebServiceHandler>(() => new WebServiceHandler());

        private UsableWriteLock _listLock = new UsableWriteLock();
        private Dictionary<Type, object> RequestertWebServiceVariants = new Dictionary<Type, object>();
        private Dictionary<Type, object> LocalWebServiceVariants = new Dictionary<Type, object>();
        private AVLHashMap<string, IPEndPoint> UrlToServerHashMap = new AVLHashMap<string, IPEndPoint>();

        /// <summary>
        /// Creates a WebServiceHandler instance.
        /// </summary>
        public WebServiceHandler()
        {
            Logger.LogTrace("A WebServiceHandler has been created.");
        }

        /// <summary>
        /// Assigns an IPEndPoint to a specific type to be found on Remote Machine as WebServices.
        /// </summary>
        /// <param name="type">The type to assign to.</param>
        /// <param name="remoteEndpoint">The IPEndpoint of the Remote Machine.</param>
        public void AssignRemoteEndpointToType(Type type, IPEndPoint remoteEndpoint)
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

        /// <summary>
        /// Retrieves a local WebService of a specified Type.
        /// </summary>
        /// <typeparam name="T">The type to generate a local Object of.</typeparam>
        /// <returns>Returns an instance of the local WebService implementation.</returns>
        public T GetLocalService<T>() where T : IWebService, new()
        {
            return (T)GetLocalService(typeof(T));
        }

        /// <summary>
        /// Retrieves a local WebService of a specified Type.
        /// </summary>
        /// <param name="type">The type to generate a local Object of.</param>
        /// <returns>Returns an instance of the local WebService implementation.</returns>
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
                object ret = WebServiceImplementationGenerator.GetWebServiceLocalImplementation(type, this);

                using(_listLock.LockWrite())
                    LocalWebServiceVariants.Add(type, ret);

                return ret;
            }
        }

        /// <summary>
        /// Retrieves a requesting WebService of a specified Type, that will contact the remote WebService whenever a method is executed or a property is being set or retrieved.
        /// </summary>
        /// <typeparam name="T">The type to generate a requesting Object of.</typeparam>
        /// <returns>Returns an instance of the requesting WebService implementation.</returns>
        public T GetRequesterService<T>() where T : IWebService, new()
        {
            return (T)GetRequesterService(typeof(T));
        }

        /// <summary>
        /// Retrieves a requesting WebService of a specified Type, that will contact the remote WebService whenever a method is executed or a property is being set or retrieved.
        /// </summary>
        /// <param name="type">The type to generate a requesting Object of.</param>
        /// <returns>Returns an instance of the requesting WebService implementation.</returns>
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
                object ret = WebServiceImplementationGenerator.GetWebServiceRequestImplementation(type, this);

                using (_listLock.LockWrite())
                    RequestertWebServiceVariants.Add(type, ret);

                return ret;
            }
        }
        
        /// <summary>
        /// Requests a certain WebServiceRequest at the local WebServiceHandler.
        /// </summary>
        /// <param name="webServiceRequest">The WebServiceRequest to reply to.</param>
        /// <returns>Returns the response as WebServiceResponse.</returns>
        public WebServiceResponse Request(WebServiceRequest webServiceRequest)
        {
            string typename = webServiceRequest.Namespace + "." + webServiceRequest.Type;
            IPEndPoint endPoint = UrlToServerHashMap[typename];

            if (!webServiceRequest.WebServiceHandler)
                webServiceRequest.WebServiceHandler = this;

            if (webServiceRequest.IsRemoteRequest && endPoint != null)
                return WebServiceResponse.Exception(new ServiceNotAvailableException($"The requested Type '{typename}' is not locally available on this {nameof(WebServiceHandler)}, but was requested from a remote {nameof(WebServiceRequest)}."));

            if (endPoint == null)
            {
                Logger.LogWarning($"The type '{typename}' has not been added to the WebServiceHandler yet and therefore could not be resolved. Trying to generate local equivalent.");

                try
                {
                    Type type = null;
                    var assemblies = AppDomain.CurrentDomain.GetAssemblies();

                    foreach (var assembly in assemblies)
                    {
                        type = assembly.GetType(typename);

                        if (type != null)
                        {
                            Logger.LogInformation($"Auto generating Local WebService Implementation of Type '{typename}' from Assembly '{assembly.GetName()}'.");
                            break;
                        }
                    }

                    if (type == null)
                        throw new IncompatibleTypeException($"Type '{typename}' could not be found in the current {nameof(AppDomain)}.");

                    object ws = GetLocalService(type);
                    var method = ws.GetType().GetMethod(webServiceRequest.Method, webServiceRequest._parameterTypes);

                    try
                    {
                        if (method.ReturnType == typeof(WebServiceResponse))
                            return (WebServiceResponse)method.Invoke(ws, webServiceRequest.Parameters);
                        else
                            throw new IncompatibleTypeException($"Return type of method '{typename}.{method.Name}' was not '{nameof(WebServiceResponse)}'.");
                    }
                    catch (WebServiceException)
                    {
                        throw;
                    }
                    catch (Exception e)
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
            else // Request from Remote WebServiceServer.
            {
                try
                {
                    return WebServiceServerRequest.Request(webServiceRequest, endPoint);
                }
                catch (Exception e)
                {
                    throw new ServiceNotAvailableException($"Exception in requesting from a remote WebServiceServer at '{endPoint}'.", e);
                }
            }
        }

        internal static string GetTypename(Type type) => type.Namespace + "." + type.Name;
    }
}
