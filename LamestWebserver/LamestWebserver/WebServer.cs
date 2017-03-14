using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Threading;
using System.Net.Sockets;
using System.Drawing;
using System.IO.Compression;
using LamestWebserver.Collections;
using System.IO;
using System.Runtime.Serialization;
using LamestWebserver.Synchronization;
using ThreadState = System.Threading.ThreadState;
using System.Reflection;
using LamestWebserver.RequestHandlers;
using Newtonsoft.Json;

namespace LamestWebserver
{
    /// <summary>
    /// A Webserver. The central unit in LamestWebserver.
    /// </summary>
    public class WebServer : IDisposable
    {
        internal static List<WebServer> RunningServers = new List<WebServer>();
        internal static UsableMutexSlim RunningServerMutex = new UsableMutexSlim();

        /// <summary>
        /// The IP and Port of the currently Connected Client.
        /// </summary>
        [ThreadStatic] public static string CurrentClientRemoteEndpoint = null;

        private readonly TcpListener _tcpListener;
        private readonly List<Thread> _threads = new List<Thread>();
        private readonly Thread _mThread;
        
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
        private AVLTree<int, NetworkStream> networkStreams = new AVLTree<int, NetworkStream>();

        Mutex cleanMutex = new Mutex();
        
        private Task<TcpClient> tcpRcvTask;

        /// <summary>
        /// Starts a new Webserver and adds the folder and default components to the CurrentResponseHandler. If you are just adding a server listening on another port as well - just use the other constructor.
        /// </summary>
        /// <param name="port">the port to listen to</param>
        /// <param name="folder">one folder to view at (can be null)</param>
        public WebServer(int port, string folder) : this(port)
        {
            ResponseHandler.CurrentResponseHandler.InsertSecondaryRequestHandler(new ErrorRequestHandler());
            ResponseHandler.CurrentResponseHandler.AddRequestHandler(new WebSocketRequestHandler());
            ResponseHandler.CurrentResponseHandler.AddRequestHandler(new PageResponseRequestHandler());
            ResponseHandler.CurrentResponseHandler.AddRequestHandler(new OneTimePageResponseRequestHandler());

            if(folder != null)
                ResponseHandler.CurrentResponseHandler.AddRequestHandler(new CachedFileRequestHandler(folder));

            ResponseHandler.CurrentResponseHandler.AddRequestHandler(new DirectoryResponseRequestHandler());
        }

        /// <summary>
        /// Starts a new Webserver listening to all previously added Responses.
        /// </summary>
        /// <param name="port">the port to listen to</param>
        public WebServer(int port)
        {
            if (!TcpPortIsUnused(port))
            {
                throw new InvalidOperationException("The tcp port " + port + " is currently used by another application.");
            }

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
            catch
            {
            }

            try
            {
                Master.ForceQuitThread(_mThread);
            }
            catch
            {
            }

            try
            {
                tcpRcvTask.Dispose();
            }
            catch
            {
            }

            networkStreamsMutex.WaitOne();

            foreach (KeyValuePair<int, NetworkStream> networkStream in networkStreams)
            {
                networkStream.Value.Close();
            }

            networkStreamsMutex.ReleaseMutex();

            int i = _threads.Count;

            while (_threads.Count > 0)
            {
                try
                {
                    Master.ForceQuitThread(_threads[0]);
                }
                catch
                {
                }

                _threads.RemoveAt(0);
            }

            using (RunningServerMutex.Lock())
                RunningServers.Remove(this);
            
            if (RunningServers.Count == 0)
                Master.StopServers();
        }

        /// <summary>
        /// Retrieves the number of used threads by this Webserver.
        /// </summary>
        /// <returns>the number of used threads by this Webserver</returns>
        public int GetThreadCount()
        {
            int num = 1;

            CleanThreads();

            for (int i = 0; i < _threads.Count; i++)
            {
                if (_threads[i] != null && _threads[i].IsAlive)
                {
                    num++;
                }
            }

            return num;
        }

        internal void CleanThreads()
        {
            cleanMutex.WaitOne();

            int threadCount = _threads.Count;
            int i = 0;

            while (i < _threads.Count)
            {
                if (_threads[i] == null ||
                    _threads[i].ThreadState == ThreadState.Running ||
                    _threads[i].ThreadState == ThreadState.Unstarted ||
                    _threads[i].ThreadState == ThreadState.AbortRequested)
                {
                    i++;
                }
                else
                {
                    try
                    {
                        _threads[i].Abort();
                    }
                    catch (Exception)
                    {
                    }

                    networkStreamsMutex.WaitOne();

                    var networkStream = networkStreams[_threads[i].ManagedThreadId];

                    if (networkStream != null)
                    {
                        try
                        {
                            networkStream.Close();
                        }
                        catch
                        {
                        }

                        networkStreams.Remove(_threads[i].ManagedThreadId);
                    }

                    networkStreamsMutex.ReleaseMutex();

                    _threads.RemoveAt(i);
                }
            }

            int threadCountAfter = _threads.Count;

            cleanMutex.ReleaseMutex();

            ServerHandler.LogMessage("Cleaning up threads. Before: " + threadCount + ", After: " + threadCountAfter + ".");
        }

        private void HandleTcpListener()
        {
            try
            {
                _tcpListener.Start();
            }
            catch (Exception e)
            {
                ServerHandler.LogMessage("The TcpListener couldn't be started. The Port is probably blocked.\n" + e);

                return;
            }

            while (Running)
            {
                try
                {
                    tcpRcvTask = _tcpListener.AcceptTcpClientAsync();
                    tcpRcvTask.Wait();
                    TcpClient tcpClient = tcpRcvTask.Result;
                    Thread t = new Thread(HandleClient);
                    _threads.Add(t);
                    t.Start((object) tcpClient);
                    ServerHandler.LogMessage("Client Connected: " + tcpClient.Client.RemoteEndPoint.ToString());

                    if (_threads.Count%25 == 0)
                    {
                        _threads.Add(new Thread(new ThreadStart(CleanThreads)));
                        _threads[_threads.Count - 1].Start();
                    }
                }
                catch (ThreadAbortException)
                {
                    break;
                }
                catch (Exception e)
                {
                    ServerHandler.LogMessage("The TcpListener failed.\n" + e);
                }
            }
        }

        private void HandleClient(object obj)
        {
            TcpClient client = (TcpClient) obj;
            client.NoDelay = true;

            NetworkStream nws = client.GetStream();
            UTF8Encoding enc = new UTF8Encoding();
            string lastmsg = null;
            WebServer.CurrentClientRemoteEndpoint = client.Client.RemoteEndPoint.ToString();

            byte[] msg;
            int bytes = 0;
            
            networkStreamsMutex.WaitOne();
            networkStreams.Add(Thread.CurrentThread.ManagedThreadId, nws);
            networkStreamsMutex.ReleaseMutex();

            Stopwatch stopwatch = new Stopwatch();

            while (Running)
            {
                msg = new byte[RequestMaxPacketSize];

                try
                {
                    if(stopwatch.IsRunning)
                        stopwatch.Reset();

                    bytes = nws.Read(msg, 0, RequestMaxPacketSize);
                    stopwatch.Start();
                }
                catch (ThreadAbortException)
                {
                    break;
                }
                catch (Exception e)
                {
                    ServerHandler.LogMessage("An error occured in the client handler:  " + e, stopwatch);
                    break;
                }

                if (bytes == 0)
                {
                    break;
                }

                try
                {
                    string msg_ = enc.GetString(msg, 0, bytes);
                    HttpPacket htp = HttpPacket.Constructor(ref msg_, client.Client.RemoteEndPoint, lastmsg, nws);

                    byte[] buffer;

                    try
                    {
                        if (htp.Version == null)
                        {
                            lastmsg = msg_;
                        }
                        else if (htp.RequestUrl == "")
                        {
                            lastmsg = null;

                            HttpPacket htp_ = new HttpPacket()
                            {
                                Status = "501 Not Implemented",
                                BinaryData = enc.GetBytes(Master.GetErrorMsg(
                                    "Error 501: Not Implemented",
                                    "<p>The Feature that you were trying to use is not yet implemented.</p>" +
#if DEBUG
                                    "<p>The Package you were sending:<br><div style='font-family:\"Consolas\",monospace;font-size: 13;color:#4C4C4C;'>" +
                                    msg_.Replace("\r\n", "<br>") +
#endif
                                    "</div></p>"))
                            };

                            buffer = htp_.GetPackage(enc);
                            nws.Write(buffer, 0, buffer.Length);

                            ServerHandler.LogMessage("Client requested an empty URL. We sent Error 501.", stopwatch);
                        }
                        else
                        {
                            lastmsg = null;
                            HttpPacket response = null;

                            try
                            {
                                response = ResponseHandler.CurrentResponseHandler.GetResponse(htp);

                                if (response == null)
                                    goto InvalidResponse;

                                buffer = response.GetPackage(enc);
                                nws.Write(buffer, 0, buffer.Length);

                                ServerHandler.LogMessage($"Client requested '{htp.RequestUrl}'. Answer delivered from {nameof(ResponseHandler)}.", stopwatch);

                                continue;
                            }
                            catch (ThreadAbortException)
                            {
                                return;
                            }
                            catch (WebSocketManagementOvertakeFlagException)
                            {
                                return;
                            }
                            catch (Exception e)
                            {
                                HttpPacket htp_ = new HttpPacket()
                                {
                                    Status = "500 Internal Server Error",
                                    BinaryData = enc.GetBytes(Master.GetErrorMsg(
                                        "Error 500: Internal Server Error",
                                        "<p>An Exception occured while processing the response.</p><br><br><div style='font-family:\"Consolas\",monospace;font-size: 13;color:#4C4C4C;'>"
                                        + GetErrorMsg(e, AbstractSessionIdentificator.CurrentSession, msg_).Replace("\r\n", "<br>").Replace(" ", "&nbsp;") + "</div><br>"
                                        + "</div></p>"))
                                };

                                buffer = htp_.GetPackage(enc);
                                nws.Write(buffer, 0, buffer.Length);

                                ServerHandler.LogMessage($"Client requested '{htp.RequestUrl}'. {e.GetType()} thrown.\n" + e, stopwatch);

                                continue;
                            }


                            InvalidResponse:

                            if (htp.RequestUrl.EndsWith("/"))
                            {
                                buffer = new HttpPacket()
                                {
                                    Status = "403 Forbidden",
                                    BinaryData = enc.GetBytes(Master.GetErrorMsg(
                                        "Error 403: Forbidden",
                                        "<p>The Requested URL cannot be delivered due to insufficient priveleges.</p>" +
#if DEBUG
                                        "<p>The Package you were sending:<br><div style='font-family:\"Consolas\",monospace;font-size: 13;color:#4C4C4C;'>" +
                                        msg_.Replace("\r\n", "<br>") +
#endif
                                        "</div></p>"))
                                }.GetPackage(enc);
                            }
                            else
                            {
                                buffer = new HttpPacket()
                                {
                                    Status = "404 File Not Found",
                                    BinaryData = enc.GetBytes(Master.GetErrorMsg(
                                        "Error 404: Page Not Found",
                                        "<p>The URL you requested did not match any page or file on the server.</p>" +
#if DEBUG
                                        "<p>The Package you were sending:<br><div style='font-family:\"Consolas\",monospace;font-size: 13;color:#4C4C4C;'>" +
                                        msg_.Replace("\r\n", "<br>") +
#endif
                                        "</div></p>"))
                                }.GetPackage(enc);
                            }

                            nws.Write(buffer, 0, buffer.Length);

                            ServerHandler.LogMessage(
                                "Client requested the URL '" + htp.RequestUrl + "' which couldn't be found on the server. Retrieved Error 403/404.", stopwatch);
                        }
                    }
                    catch (Exception e)
                    {
                        ServerHandler.LogMessage("An error occured in the client handler: " + e, stopwatch);
                    }
                }
                catch (ThreadAbortException)
                {
                    return;
                }
                catch (Exception e)
                {
                    ServerHandler.LogMessage("An error occured in the client handler: " + e, stopwatch);
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
        public static string GetErrorMsg(Exception exception, AbstractSessionIdentificator sessionData, string httpPacket)
        {
            string ret = exception.ToString() + "\n\n";

            ret += "The package you were sending:\n\n" + httpPacket;

            if (ErrorMsgContainSessionData && sessionData != null)
            {
                ret += "\n\n______________________________________________________________________________________________\n\n";

                if (sessionData is SessionData)
                {
                    SessionData _sessionData = (SessionData)sessionData;

                    if (_sessionData.HttpHeadParameters.Count > 0)
                    {
                        ret += "\n\nHTTP-Head:\n\n";

                        for (int i = 0; i < _sessionData.HttpHeadParameters.Count; i++)
                            ret += "'" + _sessionData.HttpHeadParameters[i] + "': " + _sessionData.HttpHeadValues[i] + "\n";
                    }

                    if (_sessionData.HttpPostParameters.Count > 0)
                    {
                        ret += "\n\nHTTP-Post:\n\n";

                        for (int i = 0; i < _sessionData.HttpPostParameters.Count; i++)
                            ret += "'" + _sessionData.HttpPostParameters[i] + "': " + _sessionData.HttpPostValues[i] + "\n";
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
