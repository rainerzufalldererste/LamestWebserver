using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using LamestWebserver.Collections;
using LamestWebserver.Core;
using LamestWebserver.Core.Memory;
using LamestWebserver.RequestHandlers;
using LamestWebserver.Synchronization;

namespace LamestWebserver
{
    /// <summary>
    /// A Webserver. The central unit in LamestWebserver.
    /// </summary>
    public class WebServer : ServerCore
    {
        internal static List<WebServer> RunningServers = new List<WebServer>();
        internal static UsableMutexSlim RunningServerMutex = new UsableMutexSlim();

        /// <summary>
        /// The IP and Port of the currently Connected Client.
        /// </summary>
        [ThreadStatic] public static string CurrentClientRemoteEndpoint = null;

        private List<WebServer> _dependentWebservers = new List<WebServer>();

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

        /// <summary>
        /// The size of the Page Response AVLTree-Hashmap. This is not the maximum amount this Hashmap can handle.
        /// </summary>
        public static int PageResponseStorageHashMapSize = 2048;
        
        /// <summary>
        /// The size of the Data Response AVLTree-Hashmap. This is not the maximum amount this Hashmap can handle.
        /// </summary>
        public static int DataResponseStorageHashMapSize = 512;

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
        public static int WebSocketResponsePageStorageHashMapSize = 512;

        /// <summary>
        /// The size of the Directory Response AVLTree-Hashmap. This is not the maximum amount this Hashmap can handle.
        /// </summary>
        public static int DirectoryResponseStorageHashMapSize = 512;

        /// <summary>
        /// The size that is read from the networkStream for each request.
        /// </summary>
        public static int RequestMaxPacketSize = 4096;
        
        /// <summary>
        /// The size that is set as starting StringBuilder capacity for a HttpResponse.
        /// </summary>
        public static int ResponseDefaultStringLength = 512;

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
        /// The RequestHandler used in this Webserver instance.
        /// </summary>
        public readonly RequestHandler RequestHandler;

        /// <summary>
        /// If enabled will provide callers with a fresh FlushableMemoryPool.
        /// </summary>
        public bool RequireUnsafeMemory = false;

        /// <summary>
        /// Starts a new Webserver and adds the folder and default components to the CurrentRequestHandler. If you are just adding a server listening on another port as well - just use a different constructor.
        /// </summary>
        /// <param name="port">The port to listen to</param>
        /// <param name="folder">a folder to read from (can be null)</param>
        /// <param name="certificate">The ssl certificate for https (if null: connection will be http; if set will only be https)
        /// <para /> If the webserver does not respond after including your certificate, it might not be loaded or set up correctly.
        /// Consider looking at the LamestWebserver Logger output.</param>
        /// <param name="enabledSslProtocols">The available ssl protocols if the connection is https.</param>
        public WebServer(int port, string folder = null, X509Certificate2 certificate = null, SslProtocols enabledSslProtocols = SslProtocols.Tls12) : this(port, certificate, enabledSslProtocols)
        {
            RequestHandler.InsertSecondaryRequestHandler(new ErrorRequestHandler());
            RequestHandler.AddRequestHandler(new WebSocketRequestHandler());
            RequestHandler.AddRequestHandler(new PageResponseRequestHandler());
            RequestHandler.AddRequestHandler(new DataResponseRequestHandler());
            RequestHandler.AddRequestHandler(new OneTimePageResponseRequestHandler());

            if(folder != null)
                RequestHandler.AddRequestHandler(new CachedFileRequestHandler(folder));

            RequestHandler.AddRequestHandler(new DirectoryResponseRequestHandler());
        }

        /// <summary>
        /// Starts a new Webserver on a specified RequestHandler.
        /// </summary>
        /// <param name="port">The port to listen to.</param>
        /// <param name="requestHandler">The RequestHandler to use. If null, RequestHandler.CurrentRequestHandler will be used.</param>
        /// <param name="certificate">The ssl certificate for https (if null: connection will be http; if set will only be https)
        /// <para /> If the webserver does not respond after including your certificate, it might not be loaded or set up correctly.
        /// Consider looking at the LamestWebserver Logger output.</param>
        /// <param name="enabledSslProtocols">The available ssl protocols if the connection is https.</param>
        public WebServer(int port, RequestHandler requestHandler, X509Certificate2 certificate = null, SslProtocols enabledSslProtocols = SslProtocols.Tls12) : this(port, certificate, enabledSslProtocols)
        {
            RequestHandler = requestHandler;
        }

        /// <summary>
        /// Starts a new Webserver listening to all previously added Responses.
        /// </summary>
        /// <param name="port">the port to listen to</param>
        /// <param name="certificate">The ssl certificate for https (if null: connection will be http; if set will only be https)
        /// <para /> If the webserver does not respond after including your certificate, it might not be loaded or set up correctly.
        /// Consider looking at the LamestWebserver Logger output.</param>
        /// <param name="enabledSslProtocols">The available ssl protocols if the connection is https.</param>
        private WebServer(int port, X509Certificate2 certificate = null, SslProtocols enabledSslProtocols = SslProtocols.Tls12) : base(port)
        {
            EnabledSslProtocols = enabledSslProtocols;
            Certificate = certificate;

            RequestHandler = RequestHandler.CurrentRequestHandler;

            Start();

            using (RunningServerMutex.Lock())
                RunningServers.Add(this);
        }

        /// <inheritdoc />
        public override void Stop()
        {
            base.Stop();

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

        /// <inheritdoc />
        protected override void HandleClient(TcpClient client, NetworkStream networkStream)
        {
            if (RequireUnsafeMemory)
                FlushableMemoryPool.AquireOrFlush();

            Stream stream = networkStream;
            Encoding enc = Encoding.UTF8;
            string lastmsg = null;
            CurrentClientRemoteEndpoint = client.Client.RemoteEndPoint.ToString();

            if (Certificate != null)
            {
                try
                {
                    System.Net.Security.SslStream sslStream = new System.Net.Security.SslStream(networkStream, false);
                    sslStream.AuthenticateAsServer(Certificate, false, EnabledSslProtocols, true);
                    stream = sslStream;
                    CurrentThreadStream = stream;
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

                        networkStream.Write(response, 0, response.Length);
                        
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
                    if (Running)
                    {
                        if (e.InnerException != null && e.InnerException is SocketException)
                        {
                            if (((SocketException)e.InnerException).SocketErrorCode == SocketError.TimedOut)
                            {
                                try
                                {
                                    string remoteEndPoint = client.Client.RemoteEndPoint.ToString();

                                    client.Client.Shutdown(SocketShutdown.Both);
                                    client.Close();

                                    Logger.LogTrace($"The connection to {remoteEndPoint} has been closed ordinarily after the timeout has been reached.", stopwatch);
                                }
                                catch { };

                                break;
                            }
                            else
                            {
                                string remoteEndPoint = "<unknown remote endpoint>";

                                try
                                {
                                    remoteEndPoint = client.Client.RemoteEndPoint.ToString();

                                    client.Client.Shutdown(SocketShutdown.Both);
                                    client.Close();

                                    Logger.LogTrace($"The connection to {remoteEndPoint} has been closed ordinarily after a SocketException occured. ({((SocketException)e.InnerException).SocketErrorCode})", stopwatch);

                                    break;
                                }
                                catch { };
                                
                                Logger.LogTrace($"A SocketException occured with {remoteEndPoint}. ({((SocketException)e.InnerException).SocketErrorCode})", stopwatch);

                                break;
                            }
                        }

                        Logger.LogError("An exception occured in the client handler:  " + e.SafeToString(), stopwatch);

                        try
                        {
                            client.Client.Shutdown(SocketShutdown.Both);
                            client.Close();

                            Logger.LogTrace($"The connection to {client.Client.RemoteEndPoint} has been closed ordinarily.", stopwatch);
                        }
                        catch { };
                    }

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
                    htp.TcpClient = client;

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
                                response = RequestHandler.GetResponse(htp);

                                if (response == null)
                                    goto InvalidResponse;

                                buffer = response.GetPackage();
                                stream.Write(buffer, 0, buffer.Length);

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

                                Logger.LogWarning($"Client requested '{htp.RequestUrl}'. {e.GetType()} thrown.\n" + e.SafeToString(), stopwatch);

                                ServerHandler.LogMessage($"Client requested '{htp.RequestUrl}'. {e.GetType()} thrown.\n" + e.SafeToString(), stopwatch);

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
                        Logger.LogError("An error occurred in the client handler: " + e.SafeToString(), stopwatch);
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
                    Logger.LogError("An error occurred in the client handler: " + e.SafeToString(), stopwatch);
                }
            }
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
