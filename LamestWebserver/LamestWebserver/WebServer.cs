using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Threading;
using System.Net.Sockets;
using LamestWebserver.Collections;
using System.IO;
using System.Runtime.Serialization;
using LamestWebserver.Synchronization;
using System.Reflection;
using LamestWebserver.RequestHandlers;
using Newtonsoft.Json;
using System.Security.Cryptography.X509Certificates;
using System.Security.Authentication;
using LamestWebserver.Core;
using LamestWebserver.Core.Memory;

using ThreadState = System.Threading.ThreadState;
using LamestWebserver.Core;

namespace LamestWebserver
{
    /// <summary>
    /// A Webserver. The central unit in LamestWebserver.
    /// </summary>
    public class WebServer : IDisposable
    {
        internal readonly Singleton<ThreadedWorker> WorkerThreads = new Singleton<ThreadedWorker>(() => new ThreadedWorker()); 

        internal static List<WebServer> RunningServers = new List<WebServer>();
        internal static UsableMutexSlim RunningServerMutex = new UsableMutexSlim();

        /// <summary>
        /// The IP and Port of the currently Connected Client.
        /// </summary>
        [ThreadStatic] public static string CurrentClientRemoteEndpoint = null;

        private readonly TcpListener _tcpListener;
        private readonly Thread _mThread;
        private List<WebServer> _dependentWebservers = new List<WebServer>();

        /// <summary>
        /// The Port, the server is listening at.
        /// </summary>
        public readonly int Port;

        internal bool Running
        {
            get
            {
                _runningLock.EnterReadLock();
                var ret = _running;
                _runningLock.ExitReadLock();
                return ret;
            }
            set
            {
                _runningLock.EnterWriteLock();
                _running = value;
                _runningLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// The number of currently running servers.
        /// </summary>
        public static int ServerCount
        {
            get
            {
                using (RunningServerMutex.Lock())
                    return RunningServers.Count;
            }
        }

        private bool _running = true;
        private ReaderWriterLockSlim _runningLock = new ReaderWriterLockSlim();

        /// <summary>
        /// The size of the Page Response AVLTree-Hashmap. This is not the maximum amount this Hashmap can handle.
        /// </summary>
        public static int PageResponseStorageHashMapSize = 256;

        /// <summary>
        /// Add a webserver to be closed (IDisposable.Dispose()) whenever this webserver is closed. 
        /// </summary>
        /// <param name="webserver">The webserver to close with this one.</param>
        public void AddDependentWebsever(WebServer webserver)
        {
            _dependentWebservers.Add(webserver);

            Logger.LogInformation($"A dependent Webserver on port {webserver.Port} has been added to a Webserver on port {Port}.");
        }

        /// <summary>
        /// The maximum amount of items in the One Time Page Response Queue (QueuedAVLTree).
        /// </summary>
        public static int OneTimePageResponsesStorageQueueSize = 4096;

        /// <summary>
        /// The size of the Websocket Response AVLTree-Hashmap. This is not the maximum amount this Hashmap can handle.
        /// </summary>
        public static int WebSocketResponsePageStorageHashMapSize = 64;

        /// <summary>
        /// The size of the Directory Response AVLTree-Hashmap. This is not the maximum amount this Hashmap can handle.
        /// </summary>
        public static int DirectoryResponseStorageHashMapSize = 128;

        /// <summary>
        /// The size that is read from the networkStream for each request.
        /// </summary>
        public static int RequestMaxPacketSize = 4096;

        private Mutex networkStreamsMutex = new Mutex();
        private AVLTree<int, Stream> streams = new AVLTree<int, Stream>();

        private Task<TcpClient> tcpRcvTask;

        /// <summary>
        /// SSL Certificate. server will use ssl if certificate not null.
        /// <para /> If the webserver does not respond after including your certificate, it might not be loaded or set up correctly.
        /// Consider the LamestWebserver log output `ServerHandler.StartHandler();`.
        /// </summary>
        public X509Certificate2 Certificate
        {
            get
            {
                return _certificate;
            }
            set
            {
                _certificate = value;

                if (value == null)
                    return;

                if (!value.Verify())
                    Logger.LogError("The certificate could not be verified.");

                // TODO: check & throw exception if sslStream.AuthenticateAsServer with this certificate will fail.
            }
        }

        private X509Certificate2 _certificate = null;

        /// <summary>
        /// Shall the server answer in plain HTTP if no certificate is provided or if the authentication fails?
        /// <para /> If this is set to true and your Certificate is not set up correctly, the webserver will not respond at all to the clients.
        /// </summary>
        public bool BlockInsecureConnections = false;

        /// <summary>
        /// Enabled SSL Protocols supported by the server.
        /// </summary>
        public SslProtocols EnabledSslProtocols { get; set; } = SslProtocols.Tls12;

        /// <summary>
        /// The ResponseHandler used in this Webserver instance.
        /// </summary>
        public readonly ResponseHandler ResponseHandler;

        /// <summary>
        /// Starts a new Webserver and adds the folder and default components to the CurrentResponseHandler. If you are just adding a server listening on another port as well - just use a different constructor.
        /// </summary>
        /// <param name="port">The port to listen to</param>
        /// <param name="folder">a folder to read from (can be null)</param>
        /// <param name="certificate">The ssl certificate for https (if null: connection will be http; if set will only be https)
        /// <para /> If the webserver does not respond after including your certificate, it might not be loaded or set up correctly.
        /// Consider looking at the LamestWebserver Logger output.</param>
        /// <param name="enabledSslProtocols">The available ssl protocols if the connection is https.</param>
        public WebServer(int port, string folder = null, X509Certificate2 certificate = null, SslProtocols enabledSslProtocols = SslProtocols.Tls12) : this(port, certificate, enabledSslProtocols)
        {
            ResponseHandler.InsertSecondaryRequestHandler(new ErrorRequestHandler());
            ResponseHandler.AddRequestHandler(new WebSocketRequestHandler());
            ResponseHandler.AddRequestHandler(new PageResponseRequestHandler());
            ResponseHandler.AddRequestHandler(new OneTimePageResponseRequestHandler());

            if(folder != null)
                ResponseHandler.AddRequestHandler(new CachedFileRequestHandler(folder));

            ResponseHandler.AddRequestHandler(new DirectoryResponseRequestHandler());
        }

        /// <summary>
        /// Starts a new Webserver on a specified ResponseHandler.
        /// </summary>
        /// <param name="port">The port to listen to.</param>
        /// <param name="responseHandler">The ResponseHandler to use. If null, ResponseHandler.CurrentResponseHandler will be used.</param>
        /// <param name="certificate">The ssl certificate for https (if null: connection will be http; if set will only be https)
        /// <para /> If the webserver does not respond after including your certificate, it might not be loaded or set up correctly.
        /// Consider looking at the LamestWebserver Logger output.</param>
        /// <param name="enabledSslProtocols">The available ssl protocols if the connection is https.</param>
        public WebServer(int port, ResponseHandler responseHandler, X509Certificate2 certificate = null, SslProtocols enabledSslProtocols = SslProtocols.Tls12) : this(port, certificate, enabledSslProtocols)
        {
            ResponseHandler = responseHandler;
        }

        /// <summary>
        /// Starts a new Webserver listening to all previously added Responses.
        /// </summary>
        /// <param name="port">the port to listen to</param>
        /// <param name="certificate">The ssl certificate for https (if null: connection will be http; if set will only be https)
        /// <para /> If the webserver does not respond after including your certificate, it might not be loaded or set up correctly.
        /// Consider looking at the LamestWebserver Logger output.</param>
        /// <param name="enabledSslProtocols">The available ssl protocols if the connection is https.</param>
        private WebServer(int port, X509Certificate2 certificate = null, SslProtocols enabledSslProtocols = SslProtocols.Tls12)
        {
            if (!TcpPortIsUnused(port))
            {
                throw new InvalidOperationException("The tcp port " + port + " is currently used by another application.");
            }

            EnabledSslProtocols = enabledSslProtocols;
            Certificate = certificate;

            ResponseHandler = ResponseHandler.CurrentResponseHandler;

            this.Port = port;
            this._tcpListener = new TcpListener(IPAddress.Any, port);

            _mThread = new Thread(new ThreadStart(HandleTcpListener));
            _mThread.Start();

            using (RunningServerMutex.Lock())
                RunningServers.Add(this);
        }

        /// <inheritdoc />
        ~WebServer()
        {
            try
            {
                Stop();
            }
            catch (Exception)
            {
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Stop();
        }

        /// <summary>
        /// Stops the Server from running.
        /// </summary>
        public void Stop()
        {
            Running = false;

            try
            {
                _tcpListener.Stop();
            }
            catch { }

            // Give the _mThread Thread time to finish.
            Thread.Sleep(1);

            if (_mThread.ThreadState != ThreadState.Stopped)
            {
                try
                {
                    Master.ForceQuitThread(_mThread);
                }
                catch { }
            }

            try
            {
                tcpRcvTask.Dispose();
            }
            catch { }

            networkStreamsMutex.WaitOne();

            foreach (KeyValuePair<int, Stream> stream in streams)
            {
                try
                {
                    stream.Value.Close();
                }
                catch { }
            }

            networkStreamsMutex.ReleaseMutex();

            WorkerThreads.Instance.Stop();

            using (RunningServerMutex.Lock())
                RunningServers.Remove(this);

            if (RunningServers.Count == 0)
                Master.StopServers();

            foreach (WebServer webserver in _dependentWebservers)
            {
                if (webserver != null)
                {
                    try
                    {
                        webserver.Stop();
                    }
                    catch { }
                }
            }
        }

        internal void ClearStreams()
        {
            networkStreamsMutex.WaitOne();

            foreach (Stream stream in streams.Values)
            {
                if (stream != null)
                {
                    try
                    {
                        stream.Close();
                    }
                    catch { }
                }
            }

            streams.Clear();

            networkStreamsMutex.ReleaseMutex();
        }

        private void HandleTcpListener()
        {
            try
            {
                _tcpListener.Start();
                Logger.LogInformation($"{nameof(WebServer)} {nameof(TcpListener)} successfully started on port {Port}.");
            }
            catch (Exception e)
            {
                Logger.LogDebugExcept("The TcpListener couldn't be started. The Port is probably blocked.", e);

                return;
            }

            while (Running)
            {
                try
                {
                    tcpRcvTask = _tcpListener.AcceptTcpClientAsync();
                    tcpRcvTask.Wait();
                    TcpClient tcpClient = tcpRcvTask.Result;

                    WorkerThreads.Instance.EnqueueJob((Action)(() => { FlushableMemoryPool.AquireOrFlush(); HandleClient(tcpClient); }));
                    Logger.LogTrace("Client Connected: " + tcpClient.Client.RemoteEndPoint.ToString());

                }
                catch (ThreadAbortException)
                {
                    break;
                }
                catch (Exception e)
                {
                    Logger.LogDebugExcept("The TcpListener failed.", e);
                }
            }
        }

        private void HandleClient(object obj)
        {
            TcpClient client = (TcpClient) obj;
            client.NoDelay = true;

            NetworkStream nws = client.GetStream();
            
            Stream stream = nws; 

            UTF8Encoding enc = new UTF8Encoding();
            string lastmsg = null;
            WebServer.CurrentClientRemoteEndpoint = client.Client.RemoteEndPoint.ToString();

            if (Certificate != null)
            {
                try
                {
                    System.Net.Security.SslStream sslStream = new System.Net.Security.SslStream(nws, false);
                    sslStream.AuthenticateAsServer(Certificate, false, EnabledSslProtocols, true);
                    stream = sslStream;
                }
                catch (ThreadAbortException)
                {
                    try
                    {
                        stream.Close();
                    }
                    catch { }

                    return;
                }
                catch (Exception e)
                {
                    Logger.LogError($"Failed to authenticate. ({e.Message} / Inner Exception: {e.InnerException?.Message})");

                    // With misconfigured Certificates every request might fail here.

                    if (BlockInsecureConnections)
                    {
                        try
                        {
                            stream.Close();
                        }
                        catch { }

                        return;
                    }

                    try
                    {
                        byte[] response = new HttpResponse(null, Master.GetErrorMsg(
                                        "Error 500: Internal Server Error",
                                        "<p>An Exception occured while trying to authenticate the connection.</p><br><br><div style='font-family:\"Consolas\",monospace;font-size: 13;color:#4C4C4C;'>"
                                        + GetErrorMsg(e, null, null).Replace("\r\n", "<br>").Replace(" ", "&nbsp;") + "</div><br>"
                                        + "</div></p>"))
                        {
                            Status = "500 Internal Server Error"
                        }.GetPackage();

                        nws.Write(response, 0, response.Length);
                        
                        Logger.LogInformation($"Replied authentication error to client.");
                    }
                    catch (Exception ex)
                    {
                        Logger.LogInformation($"Failed to reply securely to unauthenticated ssl Connection. ({ex.Message})");
                    }

                    try
                    {
                        stream.Close();
                    }
                    catch { }

                    return;
                }
            }
            else if (BlockInsecureConnections)
            {
                Logger.LogCrashAndBurn($"Failed to authenticate. (No Certificate given.) This will fail every single time. Crashing...");

                try
                {
                    stream.Close();
                }
                catch { }

                return;
            }

            byte[] msg;
            int bytes = 0;

            networkStreamsMutex.WaitOne();
            
            Stream lastStream = streams[Thread.CurrentThread.ManagedThreadId];

            if(lastStream != null)
            {
                try
                {
                    lastStream.Dispose();
                }
                catch { };
            }
            
            streams.Add(Thread.CurrentThread.ManagedThreadId, stream);
            networkStreamsMutex.ReleaseMutex();

            Stopwatch stopwatch = new Stopwatch();

            while (Running)
            {
                msg = new byte[RequestMaxPacketSize];

                try
                {
                    if(stopwatch.IsRunning)
                        stopwatch.Reset();

                    bytes = stream.Read(msg, 0, RequestMaxPacketSize);
                    stopwatch.Start();
                }
                catch (ThreadAbortException)
                {
                    break;
                }
                catch (Exception e)
                {
                    if(Running)
                        Logger.LogError("An exception occured in the client handler:  " + e, stopwatch);

                    break;
                }

                if (bytes == 0)
                {
                    break;
                }

                try
                {
                    string msg_ = enc.GetString(msg, 0, bytes);
                    HttpRequest htp = HttpRequest.Constructor(ref msg_, lastmsg, stream);

                    byte[] buffer;

                    try
                    {
                        if (htp.IsIncompleteRequest)
                        {
                            lastmsg = msg_;
                        }
                        else if (htp.RequestUrl == "")
                        {
                            lastmsg = null;

                            HttpResponse htp_ = new HttpResponse(null, Master.GetErrorMsg(
                                    "Error 501: Not Implemented",
                                    "<p>The Feature that you were trying to use is not yet implemented.</p>" +
#if DEBUG
                                    "<p>The Package you were sending:<br><div style='font-family:\"Consolas\",monospace;font-size: 13;color:#4C4C4C;'>" +
                                    msg_.Replace("\r\n", "<br>") +
#endif
                                    "</div></p>"))
                            {
                                Status = "501 Not Implemented"
                            };

                            buffer = htp_.GetPackage();
                            stream.Write(buffer, 0, buffer.Length);

                            Logger.LogInformation("Client requested an empty URL. We sent Error 501.", stopwatch);
                        }
                        else
                        {
                            lastmsg = null;
                            HttpResponse response = null;

                            try
                            {
                                response = ResponseHandler.GetResponse(htp);

                                if (response == null)
                                    goto InvalidResponse;

                                buffer = response.GetPackage();
                                stream.Write(buffer, 0, buffer.Length);

                                Logger.LogInformation($"Client requested '{htp.RequestUrl}'. Answer delivered from {nameof(ResponseHandler)}.", stopwatch);

                                continue;
                            }
                            catch (ThreadAbortException)
                            {
                                try
                                {
                                    stream.Close();
                                }
                                catch { }

                                return;
                            }
                            catch (WebSocketManagementOvertakeFlagException)
                            {
                                return;
                            }
                            catch (Exception e)
                            {
                                HttpResponse htp_ = new HttpResponse(null, Master.GetErrorMsg(
                                        "Error 500: Internal Server Error",
                                        "<p>An Exception occured while processing the response.</p><br><br><div style='font-family:\"Consolas\",monospace;font-size: 13;color:#4C4C4C;'>"
                                        + GetErrorMsg(e, SessionData.CurrentSession, msg_).Replace("\r\n", "<br>").Replace(" ", "&nbsp;") + "</div><br>"
                                        + "</div></p>"))
                                {
                                    Status = "500 Internal Server Error"
                                };

                                buffer = htp_.GetPackage();
                                stream.Write(buffer, 0, buffer.Length);

                                Logger.LogWarning($"Client requested '{htp.RequestUrl}'. {e.GetType()} thrown.\n" + e, stopwatch);

                                continue;
                            }


                            InvalidResponse:

                            if (htp.RequestUrl.EndsWith("/"))
                            {
                                buffer = new HttpResponse(null, Master.GetErrorMsg(
                                        "Error 403: Forbidden",
                                        "<p>The Requested URL cannot be delivered due to insufficient priveleges.</p>" +
#if DEBUG
                                        "<p>The Package you were sending:<br><div style='font-family:\"Consolas\",monospace;font-size: 13;color:#4C4C4C;'>" +
                                        msg_.Replace("\r\n", "<br>") +
#endif
                                        "</div></p>"))
                                {
                                    Status = "403 Forbidden"
                                }.GetPackage();
                            }
                            else
                            {
                                buffer = new HttpResponse(null, Master.GetErrorMsg(
                                        "Error 404: Page Not Found",
                                        "<p>The URL you requested did not match any page or file on the server.</p>" +
#if DEBUG
                                        "<p>The Package you were sending:<br><div style='font-family:\"Consolas\",monospace;font-size: 13;color:#4C4C4C;'>" +
                                        msg_.Replace("\r\n", "<br>") +
#endif
                                        "</div></p>"))
                                {
                                    Status = "404 File Not Found"
                                }.GetPackage();
                            }

                            stream.Write(buffer, 0, buffer.Length);

                            Logger.LogInformation("Client requested the URL '" + htp.RequestUrl + "' which couldn't be found on the server. Retrieved Error 403/404.", stopwatch);
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.LogError("An error occured in the client handler: " + e, stopwatch);
                    }
                }
                catch (ThreadAbortException)
                {
                    try
                    {
                        stream.Close();
                    }
                    catch { }

                    return;
                }
                catch (Exception e)
                {
                    Logger.LogError("An error occured in the client handler: " + e, stopwatch);
                }
            }
        }

        /// <summary>
        /// Source: http://stackoverflow.com/questions/570098/in-c-how-to-check-if-a-tcp-port-is-available
        /// </summary>
        /// <param name="port">The TCP-Port to check for</param>
        /// <returns>true if unused</returns>
        public static bool TcpPortIsUnused(int port)
        {
            // Evaluate current system tcp connections. This is the same information provided
            // by the netstat command line application, just in .Net strongly-typed object
            // form.  We will look through the list, and if our port we would like to use
            // in our TcpClient is occupied, we will set isAvailable to false.
            System.Net.NetworkInformation.IPGlobalProperties ipGlobalProperties = System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties();
            IPEndPoint[] tcpConnInfoArray = ipGlobalProperties.GetActiveTcpListeners();

            foreach (IPEndPoint endpoint in tcpConnInfoArray)
            {
                if (endpoint.Port == port)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Source: http://stackoverflow.com/questions/3825390/effective-way-to-find-any-files-encoding
        /// Determines a text file's encoding by analyzing its byte order mark (BOM).
        /// Defaults to ASCII when detection of the text file's endianness fails.
        /// </summary>
        /// <param name="filename">The text file to analyze.</param>
        /// <returns>The detected encoding.</returns>
        public static Encoding GetEncoding(string filename)
        {
            using (StreamReader reader = new StreamReader(filename, Encoding.ASCII, true))
            {
                reader.Peek(); // you need this!
                return reader.CurrentEncoding;
            }
        }

        /// <summary>
        /// Shall the ErrorMsg contain the current SessionData if possible?
        /// </summary>
        public static bool ErrorMsgContainSessionData = true;

        /// <summary>
        /// Shall exception-messages be encrypted?
        /// </summary>
        public static bool EncryptErrorMsgs = false;

        /// <summary>
        /// The Key for the exception-message encryption.
        /// </summary>
        public static byte[] ErrorMsgKey { set { _errorMsgKey = value; } }

        /// <summary>
        /// The IV for the exception-message encryption.
        /// </summary>
        public static byte[] ErrorMsgIV { set { _errorMsgIV = value; } }

        private static byte[] _errorMsgKey = Security.Encryption.GetKey();
        private static byte[] _errorMsgIV = Security.Encryption.GetIV();

        /// <summary>
        /// Retrieves an error message
        /// </summary>
        /// <param name="exception">the exception that happened</param>
        /// <param name="sessionData">the sessionData (can be null)</param>
        /// <param name="httpPacket">the http-request</param>
        /// <returns>a nice error message</returns>
        public static string GetErrorMsg(Exception exception, SessionData sessionData, string httpPacket)
        {
            string ret = exception.ToString() + "\n\n";

            ret += "The package you were sending:\n\n" + httpPacket;

            if (ErrorMsgContainSessionData && sessionData != null)
            {
                ret += "\n\n______________________________________________________________________________________________\n\n";

                if (sessionData is HttpSessionData)
                {
                    if (sessionData.HttpHeadVariables.Count > 0)
                    {
                        ret += "\n\nHTTP-Head:\n\n";

                        foreach (var variable in sessionData.HttpHeadVariables)
                            ret += "'" + variable.Key + "': " +variable.Value + "\n";
                    }

                    if (sessionData.HttpPostVariables.Count > 0)
                    {
                        ret += "\n\nHTTP-Post:\n\n";

                        foreach (var variable in sessionData.HttpPostVariables)
                            ret += "'" + variable.Key + "': " + variable.Value + "\n";
                    }
                }

                IDictionary<string, object> currentDictionary = null;

                if (sessionData.KnownUser)
                {
                    currentDictionary = sessionData._userInfo.UserGlobalVariables;

                    if (currentDictionary != null && currentDictionary.Count > 0)
                    {
                        ret += "\n\nUserGlobalVars:\n\n";

                        SerializeValues(currentDictionary, ref ret);
                    }

                    currentDictionary = sessionData.GetUserPerFileVariables();

                    if (currentDictionary != null && currentDictionary.Count > 0)
                    {
                        ret += "\n\nUserFileVars:\n\n";

                        SerializeValues(currentDictionary, ref ret);
                    }
                }

                currentDictionary = sessionData.GetGlobalVariables();

                if (currentDictionary != null && currentDictionary.Count > 0)
                {
                    ret += "\n\nGlobalVars:\n\n";

                    SerializeValues(currentDictionary, ref ret);
                }

                currentDictionary = sessionData.GetPerFileVariables();

                if (currentDictionary != null && currentDictionary.Count > 0)
                {
                    ret += "\n\nFileVars:\n\n";

                    SerializeValues(currentDictionary, ref ret);
                }
            }

            if (EncryptErrorMsgs)
            {
                ret = Security.Encryption.Encrypt(ret, _errorMsgKey, _errorMsgIV);

                for (int i = ret.Length - 1; i >= 0; i--)
                {
                    if (i % 128 == 0)
                        ret = ret.Insert(i, "\n");
                }

                ret = "The Error-Message has been encrypted for security reasons.\nIf this error occurs multiple times, please contact the developers and send them this piece of code.\n" + ret;
            }

            return ret;
        }

        private static void SerializeValues(IDictionary<string, object> data, ref string ret)
        {
            foreach (var variable in data)
            {
                try
                {
                    if (variable.Value.GetType().GetInterfaces().Contains(typeof(ISerializable)) ||
                        (from attrib in variable.Value.GetType().GetCustomAttributes() where attrib is SerializableAttribute select attrib).Any())
                    {
                        ret += "'" + variable.Key + "': \n" + Newtonsoft.Json.JsonConvert.SerializeObject(variable.Value, Formatting.Indented) + "\n\n";
                        continue;
                    }
                }
                catch
                {
                }

                ret += "'" + variable.Key + "': " + variable.Value + "\n";
            }
        }
    }
}
