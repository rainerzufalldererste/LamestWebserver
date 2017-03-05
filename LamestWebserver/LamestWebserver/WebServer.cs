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
        [ThreadStatic] public static string CurrentClientRemoteEndpoint = "<?>";

        private readonly TcpListener _tcpListener;
        private readonly List<Thread> _threads = new List<Thread>();
        private readonly Thread _mThread;
        
        /// <summary>
        /// The Port, the server is listening at.
        /// </summary>
        public readonly int Port;

        /// <summary>
        /// The Folder, the server should look for files at.
        /// </summary>
        public string Folder = "./web";

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

        internal AVLTree<string, PreloadedFile> Cache = new AVLTree<string, PreloadedFile>();

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

        private ReaderWriterLockSlim pageResponseWriteLock = new ReaderWriterLockSlim();

        private AVLHashMap<string, Master.GetContents> pageResponses = new AVLHashMap<string, Master.GetContents>(PageResponseStorageHashMapSize);
        private QueuedAVLTree<string, Master.GetContents> oneTimePageResponses = new QueuedAVLTree<string, Master.GetContents>(OneTimePageResponsesStorageQueueSize);
        private AVLHashMap<string, WebSocketCommunicationHandler> webSocketResponses = new AVLHashMap<string, WebSocketCommunicationHandler>(WebSocketResponsePageStorageHashMapSize);
        private AVLHashMap<string, Master.GetDirectoryContents> directoryResponses = new AVLHashMap<string, Master.GetDirectoryContents>(DirectoryResponseStorageHashMapSize);

        private Mutex networkStreamsMutex = new Mutex();
        private AVLTree<int, NetworkStream> networkStreams = new AVLTree<int, NetworkStream>();

        private bool acceptPages = true;
        internal bool useCache = true;

        Mutex cleanMutex = new Mutex();
        private bool silent;

        private FileSystemWatcher fileSystemWatcher = null;
        internal UsableMutex CacheMutex = new UsableMutex();

        private readonly byte[] crlf = Encoding.UTF8.GetBytes("\r\n");
        private Task<TcpClient> tcpRcvTask;

        private Random random = new Random();

        /// <summary>
        /// Starts a new Webserver.
        /// </summary>
        /// <param name="port">the port listen to</param>
        /// <param name="folder">the folder to listen to</param>
        public WebServer(int port, string folder) : this(port, folder, true, true)
        {
        }

        internal WebServer(int port, string folder, bool acceptPages, bool silent = false)
        {
            if (!TcpPortIsUnused(port))
            {
                throw new InvalidOperationException("The tcp port " + port + " is currently used by another application.");
            }

            this.acceptPages = acceptPages;

            if (acceptPages)
            {
                Master.AddFunctionEvent += AddFunction;
                Master.RemoveFunctionEvent += RemoveFunction;
                Master.AddOneTimeFunctionEvent += AddOneTimeFunction;
                Master.AddDirectoryFunctionEvent += AddDirectoryFunction;
                Master.RemoveDirectoryFunctionEvent += RemoveDirectoryFunction;

                // Websockets
                Master.AddWebsocketHandlerEvent += AddWebsocketHandler;
                Master.RemoveWebsocketHandlerEvent += RemoveWebsocketHandler;
            }

            this.Port = port;
            this._tcpListener = new TcpListener(IPAddress.Any, port);
            _mThread = new Thread(new ThreadStart(HandleTcpListener));
            _mThread.Start();
            this.silent = silent;
            this.Folder = folder;

            if (useCache)
                SetupFileSystemWatcher();

            using (RunningServerMutex.Lock())
                RunningServers.Add(this);
        }

        /// <inheritdoc />
        ~WebServer()
        {
            if (acceptPages)
            {
                Master.AddFunctionEvent -= AddFunction;
                Master.RemoveFunctionEvent -= RemoveFunction;
                Master.AddOneTimeFunctionEvent -= AddOneTimeFunction;
                Master.AddWebsocketHandlerEvent -= AddWebsocketHandler;
                Master.RemoveWebsocketHandlerEvent -= RemoveWebsocketHandler;
                Master.AddDirectoryFunctionEvent -= AddDirectoryFunction;
                Master.RemoveDirectoryFunctionEvent -= RemoveDirectoryFunction;
            }

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

        private void AddFunction(string URL, Master.GetContents getc)
        {
            pageResponseWriteLock.EnterWriteLock();
            pageResponses.Add(URL, getc);
            pageResponseWriteLock.ExitWriteLock();

            ServerHandler.LogMessage("The URL '" + URL + "' is now assigned to a Page. (WebserverApi)");
        }

        private void AddOneTimeFunction(string URL, Master.GetContents getc)
        {
            pageResponseWriteLock.EnterWriteLock();
            oneTimePageResponses.Add(URL, getc);
            pageResponseWriteLock.ExitWriteLock();

            ServerHandler.LogMessage("The URL '" + URL + "' is now assigned to a Page. (WebserverApi/OneTimeFunction)");
        }

        private void RemoveFunction(string URL)
        {
            pageResponseWriteLock.EnterWriteLock();
            pageResponses.Remove(URL);
            pageResponseWriteLock.ExitWriteLock();

            ServerHandler.LogMessage("The URL '" + URL + "' is not assigned to a Page anymore. (WebserverApi)");
        }

        private void AddDirectoryFunction(string URL, Master.GetDirectoryContents function)
        {
            pageResponseWriteLock.EnterWriteLock();
            directoryResponses.Add(URL, function);
            pageResponseWriteLock.ExitWriteLock();

            ServerHandler.LogMessage("The Directory with the URL '" + URL + "' is now available at the Webserver. (WebserverApi)");
        }

        private void RemoveDirectoryFunction(string URL)
        {
            pageResponseWriteLock.EnterWriteLock();
            directoryResponses.Remove(URL);
            pageResponseWriteLock.ExitWriteLock();

            ServerHandler.LogMessage("The Directory with the URL '" + URL + "' is not available at the Webserver anymore. (WebserverApi)");
        }

        private void AddWebsocketHandler(WebSocketCommunicationHandler handler)
        {
            pageResponseWriteLock.EnterWriteLock();
            webSocketResponses.Add(handler.URL, handler);
            pageResponseWriteLock.ExitWriteLock();

            ServerHandler.LogMessage("The URL '" + handler.URL + "' is now assigned to a Page. (Websocket)");
        }

        private void RemoveWebsocketHandler(string URL)
        {
            pageResponseWriteLock.EnterWriteLock();
            webSocketResponses.Remove(URL);
            pageResponseWriteLock.ExitWriteLock();

            ServerHandler.LogMessage("The URL '" + URL + "' is not assigned to a Page anymore. (Websocket)");
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

                if (!silent)
                    Console.WriteLine("Failed to start TcpListener.\n" + e);
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
                    if (!silent)
                        ServerHandler.LogMessage("The TcpListener failed.\n" + e);
                }
            }
        }

        private void HandleClient(object obj)
        {
            TcpClient client = (TcpClient) obj;
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

                            while (htp.RequestUrl.Length >= 2 && (htp.RequestUrl[0] == ' ' || htp.RequestUrl[0] == '/'))
                            {
                                htp.RequestUrl = htp.RequestUrl.Remove(0, 1);
                            }

                            Master.GetContents currentRequest;

                            pageResponseWriteLock.EnterReadLock();
                            currentRequest = pageResponses[htp.RequestUrl];
                            pageResponseWriteLock.ExitReadLock();

                            WebSocketCommunicationHandler currentWebSocketHandler;

                            pageResponseWriteLock.EnterReadLock();

                            if (htp.IsWebsocketUpgradeRequest && webSocketResponses.TryGetValue(htp.RequestUrl, out currentWebSocketHandler))
                            {
                                pageResponseWriteLock.ExitReadLock();
                                var handler =
                                    (Fleck.Handlers.ComposableHandler)
                                    Fleck.HandlerFactory.BuildHandler(Fleck.RequestParser.Parse(msg), currentWebSocketHandler._OnMessage, currentWebSocketHandler._OnClose,
                                        currentWebSocketHandler._OnBinary, currentWebSocketHandler._OnPing, currentWebSocketHandler._OnPong);
                                msg = handler.CreateHandshake();
                                nws.Write(msg, 0, msg.Length);

                                ServerHandler.LogMessage("Client requested the URL '" + htp.RequestUrl + "'. (WebSocket Upgrade Request)", stopwatch);

                                var proxy = new WebSocketHandlerProxy(nws, currentWebSocketHandler, handler);

                                return;
                            }
                            else
                            {
                                pageResponseWriteLock.ExitReadLock();
                            }

                            if (currentRequest == null)
                            {
                                pageResponseWriteLock.EnterReadLock();
                                currentRequest = oneTimePageResponses[htp.RequestUrl];
                                pageResponseWriteLock.ExitReadLock();

                                if (currentRequest != null)
                                {
                                    pageResponseWriteLock.EnterWriteLock();
                                    oneTimePageResponses.Remove(htp.RequestUrl);
                                    pageResponseWriteLock.ExitWriteLock();
                                }
                            }

                            if (currentRequest != null)
                            {
                                HttpPacket htp_ = new HttpPacket();

                                Exception error = null;

                                int tries = 0;

                                SessionData sessionData = new SessionData(htp);

                                RetryGetData:

                                try
                                {
                                    htp_.BinaryData = enc.GetBytes(currentRequest.Invoke(sessionData));

                                    if (sessionData.SetCookies.Count > 0)
                                    {
                                        htp_.Cookies = sessionData.SetCookies;
                                    }
                                }
                                catch (MutexRetryException e)
                                {
                                    ServerHandler.LogMessage("MutexRetryException. Retrying...", stopwatch);

                                    if (tries >= 10)
                                    {
                                        htp_.BinaryData = enc.GetBytes(Master.GetErrorMsg("Exception in Page Response for '"
                                                                                          + htp.RequestUrl + "'",
                                            $"<b>An Error occured while processing the output ({tries} Retries)</b><br><br><div style='font-family:\"Consolas\",monospace;font-size: 13;color:#4C4C4C;'>"
                                            + GetErrorMsg(e, sessionData, msg_).Replace("\r\n", "<br>").Replace(" ", "&nbsp;") + "</div><br>"
                                            + "</div></p>"));

                                        error = e;
                                        goto SendPackage;
                                    }

                                    tries++;
                                    Thread.Sleep(random.Next((25)*tries));
                                    goto RetryGetData;
                                }
                                catch (Exception e)
                                {
                                    htp_.BinaryData = enc.GetBytes(Master.GetErrorMsg("Exception in Page Response for '"
                                                                                      + htp.RequestUrl + "'", "<b>An Error occured while processing the output</b><br><br><div style='font-family:\"Consolas\",monospace;font-size: 13;color:#4C4C4C;'>"
                                                                                      + GetErrorMsg(e, sessionData, msg_).Replace("\r\n", "<br>").Replace(" ", "&nbsp;") + "</div><br>"
                                                                                      + "</div></p>"));

                                    error = e;
                                }

                                SendPackage:

                                buffer = htp_.GetPackage(enc);
                                nws.Write(buffer, 0, buffer.Length);

                                if (error == null)
                                    ServerHandler.LogMessage("Client requested the URL '" + htp.RequestUrl + "'. (C# WebserverApi)", stopwatch);
                                else
                                    ServerHandler.LogMessage("Client requested the URL '" + htp.RequestUrl +
                                                             "'. (C# WebserverApi)\nThe URL crashed with the following Exception:\n" + error, stopwatch);
                            }
                            else
                            {
                                var response = GetFile(htp.RequestUrl, htp, enc, msg_);

                                if (response != null)
                                {
                                    buffer = response.GetPackage(enc);
                                    nws.Write(buffer, 0, buffer.Length);

                                    buffer = null;

                                    ServerHandler.LogMessage("Client requested the URL '" + htp.RequestUrl + "'.", stopwatch);
                                }
                                else
                                {
                                    Master.GetDirectoryContents directory = null;
                                    string bestUrlMatch = htp.RequestUrl;

                                    if (bestUrlMatch.StartsWith("/"))
                                        bestUrlMatch = bestUrlMatch.Remove(0);

                                    while (true)
                                    {
                                        for (int i = bestUrlMatch.Length - 2; i >= 0; i--)
                                        {
                                            if (bestUrlMatch[i] == '/')
                                            {
                                                bestUrlMatch = bestUrlMatch.Substring(0, i + 1);
                                                break;
                                            }

                                            if (i == 0)
                                            {
                                                bestUrlMatch = "";
                                                break;
                                            }
                                        }

                                        pageResponseWriteLock.EnterReadLock();
                                        directory = directoryResponses[bestUrlMatch];
                                        pageResponseWriteLock.ExitReadLock();

                                        if (directory != null || bestUrlMatch.Length == 0)
                                        {
                                            break;
                                        }
                                    }

                                    if (directory != null)
                                    {
                                        HttpPacket htp_ = new HttpPacket();
                                        string subDir = htp.RequestUrl.Substring(bestUrlMatch.Length).TrimStart('/');

                                        Exception error = null;

                                        int tries = 0;

                                        SessionData sessionData = new SessionData(htp);

                                        RetryGetData:

                                        try
                                        {
                                            htp_.BinaryData = enc.GetBytes(directory.Invoke(sessionData, subDir));

                                            if (sessionData.SetCookies.Count > 0)
                                            {
                                                htp_.Cookies = sessionData.SetCookies;
                                            }
                                        }
                                        catch (MutexRetryException e)
                                        {
                                            ServerHandler.LogMessage("MutexRetryException. Retrying...", stopwatch);

                                            if (tries >= 10)
                                            {
                                                htp_.BinaryData = enc.GetBytes(Master.GetErrorMsg("Exception in Directory Response for '"
                                                                                              + htp.RequestUrl + "' in Directory Response '" + bestUrlMatch + "'",
                                                    $"<b>An Error occured while processing the output ({tries} Retries)</b><br><br><div style='font-family:\"Consolas\",monospace;font-size: 13;color:#4C4C4C;'>"
                                                    + GetErrorMsg(e, sessionData, msg_).Replace("\r\n", "<br>").Replace(" ", "&nbsp;") + "</div><br>"
                                                    + "</div></p>"));

                                                error = e;
                                                goto SendPackage;
                                            }

                                            tries++;
                                            Thread.Sleep(random.Next((25)*tries));
                                            goto RetryGetData;
                                        }
                                        catch (Exception e)
                                        {
                                            htp_.BinaryData = enc.GetBytes(Master.GetErrorMsg("Exception in Directory Response for '"
                                                                                              + htp.RequestUrl + "' in Directory Response '" + bestUrlMatch + "'",
                                                "<b>An Error occured while processing the output</b><br><br><div style='font-family:\"Consolas\",monospace;font-size: 13;color:#4C4C4C;'>"
                                                + GetErrorMsg(e, sessionData, msg_).Replace("\r\n", "<br>").Replace(" ", "&nbsp;") + "</div><br>"
                                                + "</div></p>"));

                                            error = e;
                                        }

                                        SendPackage:

                                        buffer = htp_.GetPackage(enc);
                                        nws.Write(buffer, 0, buffer.Length);

                                        if (error == null)
                                            ServerHandler.LogMessage("Client requested the Directory URL '" + subDir + "' in Directory Page '" + bestUrlMatch +
                                                                     "'. (C# WebserverApi)", stopwatch);
                                        else
                                            ServerHandler.LogMessage("Client requested the Directory URL '" + subDir + "' in Directory Page '" + bestUrlMatch +
                                                                     "'. (C# WebserverApi)\nThe URL crashed with the following Exception:\n" + error, stopwatch);
                                    }
                                    else
                                    {
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

                                        ServerHandler.LogMessage("Client requested the URL '" + htp.RequestUrl + "' which couldn't be found on the server. Retrieved Error 403/404.", stopwatch);
                                    }
                                }
                            }
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

        private HttpPacket GetFile(string URL, HttpPacket requestPacket, UTF8Encoding enc, string fullPacketString)
        {
            string fileName = URL;
            byte[] contents = null;
            DateTime? lastModified = null;
            PreloadedFile file;
            bool notModified = false;
            string extention = null;

            try
            {
                if (fileName.Length == 0 || fileName[0] != '/')
                    fileName = fileName.Insert(0, "/");

                if (fileName[fileName.Length - 1] == '/') // is directory?
                {
                    fileName += "index.html";
                    extention = "html";

                    if (useCache && GetFromCache(fileName, out file))
                    {
                        lastModified = file.LastModified;

                        if (requestPacket.ModifiedDate != null && requestPacket.ModifiedDate.Value < lastModified)
                        {
                            contents = file.Contents;

                            extention = GetExtention(fileName);
                        }
                        else
                        {
                            contents = file.Contents;

                            notModified = requestPacket.ModifiedDate != null;
                        }
                    }
                    else if (File.Exists(Folder + fileName))
                    {
                        contents = ReadFile(fileName, enc);
                        lastModified = File.GetLastWriteTimeUtc(Folder + fileName);

                        if (useCache)
                        {
                            using (CacheMutex.Lock())
                            {
                                Cache.Add(fileName, new PreloadedFile(fileName, contents, contents.Length, lastModified.Value, false));
                            }
                        }
                    }
                    else
                    {
                        return null;
                    }
                }
                else if (useCache && GetFromCache(fileName, out file))
                {
                    extention = GetExtention(fileName);
                    lastModified = file.LastModified;

                    if (requestPacket.ModifiedDate != null && requestPacket.ModifiedDate.Value < lastModified)
                    {
                        contents = file.Contents;

                        extention = GetExtention(fileName);
                    }
                    else
                    {
                        contents = file.Contents;

                        notModified = requestPacket.ModifiedDate != null;
                    }
                }
                else if (File.Exists(Folder + fileName))
                {
                    extention = GetExtention(fileName);
                    bool isBinary = FileIsBinary(fileName, extention);
                    contents = ReadFile(fileName, enc, isBinary);
                    lastModified = File.GetLastWriteTimeUtc(Folder + fileName);

                    if (useCache)
                    {
                        using (CacheMutex.Lock())
                        {
                            Cache.Add(fileName, new PreloadedFile(fileName, contents, contents.Length, lastModified.Value, isBinary));
                        }

                        ServerHandler.LogMessage("The URL '" + URL + "' is now available through the cache.");
                    }
                }
                else
                {
                    return null;
                }

                if (notModified)
                {
                    return new HttpPacket() {Status = "304 Not Modified", ContentType = null, ModifiedDate = lastModified, BinaryData = crlf};
                }
                else
                {
                    return new HttpPacket() {ContentType = GetMimeType(extention), BinaryData = contents, ModifiedDate = lastModified};
                }
            }
            catch (Exception e)
            {
                return new HttpPacket()
                {
                    Status = "500 Internal Server Error",
                    BinaryData = enc.GetBytes(Master.GetErrorMsg(
                        "Error 500: Internal Server Error",
                        "<p>An Exception occurred while sending the response:<br><br></p><div style='font-family:\"Consolas\",monospace;font-size: 13;color:#4C4C4C;'>"
                        + GetErrorMsg(e, null, fullPacketString).Replace("\r\n", "<br>").Replace(" ", "&nbsp;") + "</div><br>"
                        + "</div>"))
                };
            }
        }

        internal bool FileIsBinary(string fileName, string extention)
        {
            if (fileName.Length < 2)
                return true;

            switch (extention)
            {
                case "html":
                case "css":
                case "js":
                case "txt":
                case "htm":
                case "xml":
                case "json":
                case "rtf":
                case "xhtml":
                case "shtml":
                case "csv":
                    return false;
                default:
                    return true;
            }
        }

        private string GetExtention(string fileName)
        {
            if (fileName.Length < 2)
                return "";

            for (int i = fileName.Length - 2; i >= 0; i--)
            {
                if (fileName[i] == '.')
                {
                    fileName = fileName.Substring(i + 1).ToLower();
                    return fileName;
                }
            }

            return "";
        }

        internal bool GetFromCache(string name, out PreloadedFile file)
        {
            using (CacheMutex.Lock())
            {
                if (Cache.TryGetValue(name, out file))
                {
                    file.LoadCount++;
                    file = file.Clone();
                }
                else
                    return false;
            }

            return true;
        }

        internal string GetMimeType(string extention)
        {
            switch (extention)
            {
                case "html":
                    return "text/html";
                case "css":
                    return "text/css";
                case "js":
                    return "text/javascript";
                case "htm":
                case "xhtml":
                case "shtml":
                    return "text/html";
                case "txt":
                    return "text/plain";
                case "png":
                    return "image/png";
                case "jpeg":
                case "jpg":
                case "jpe":
                    return "image/jpeg";
                case "pdf":
                    return "application/pdf";
                case "zip":
                    return "application/zip";
                case "ico":
                    return "image/x-icon";
                case "xml":
                    return "text/xml";
                case "json":
                    return "application/json";
                case "rtf":
                    return "text/rtf";
                case "csv":
                    return "text/comma-separated-values";
                case "doc":
                case "dot":
                    return "application/msword";
                case "docx":
                    return "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
                case "xls":
                case "xla":
                    return "application/msexcel";
                case "xlsx":
                    return "application/vnd.openxmlformats-officedocument. spreadsheetml.sheet";
                case "ppt":
                case "ppz":
                case "pps":
                case "pot":
                    return "application/mspowerpoint";
                case "gif":
                    return "image/gif";
                case "bmp":
                    return "image";
                case "wav":
                    return "audio/x-wav";
                case "mp2":
                case "mp3":
                case "aac":
                    return "audio/x-mpeg";
                case "aif":
                case "aiff":
                case "aifc":
                    return "audio/x-aiff";
                case "mpeg":
                case "mpg":
                case "mpe":
                    return "video/mpeg";
                case "qt":
                case "mov":
                    return "video/quicktime";
                case "avi":
                    return "video/x-msvideo";
                case "tiff":
                case "tif":
                    return "image/tiff";
                case "swf":
                case "cab":
                    return "application/x-shockwave-flash";
                case "hlp":
                case "chm":
                    return "application/mshelp";
                case "midi":
                case "mid":
                    return "audio/x-midi";
                default:
                    return "application/octet-stream";
            }
        }

        internal bool CacheHas(string name)
        {
            using (CacheMutex.Lock())
            {
                return Cache.ContainsKey(name);
            }
        }

        internal void SetupFileSystemWatcher()
        {
            fileSystemWatcher = new FileSystemWatcher(Folder);

            fileSystemWatcher.Renamed += (object sender, RenamedEventArgs e) =>
            {
                using (CacheMutex.Lock())
                {
                    PreloadedFile file, oldfile = Cache["/" + e.OldName];

                    try
                    {
                        if (Cache.TryGetValue(e.OldName, out file))
                        {
                            Cache.Remove("/" + e.OldName);
                            file.Filename = "/" + e.Name;
                            file.Contents = ReadFile(file.Filename, new UTF8Encoding(), file.IsBinary);
                            file.Size = file.Contents.Length;
                            file.LastModified = File.GetLastWriteTimeUtc(Folder + e.Name);
                            Cache.Add(e.Name, file);
                        }
                    }
                    catch (Exception)
                    {
                        oldfile.Filename = "/" + e.Name;
                        Cache["/" + e.Name] = oldfile;
                    }
                }

                ServerHandler.LogMessage("The URL '" + e.OldName + "' has been renamed to '" + e.Name + "' in the cache and filesystem.");
            };

            fileSystemWatcher.Deleted += (object sender, FileSystemEventArgs e) =>
            {
                using (CacheMutex.Lock())
                {
                    Cache.Remove("/" + e.Name);
                }

                ServerHandler.LogMessage("The URL '" + e.Name + "' has been deleted from the cache and filesystem.");
            };

            fileSystemWatcher.Changed += (object sender, FileSystemEventArgs e) =>
            {
                using (CacheMutex.Lock())
                {
                    PreloadedFile file = Cache["/" + e.Name];

                    try
                    {
                        if (file != null)
                        {
                            file.Contents = ReadFile(file.Filename, new UTF8Encoding(), file.IsBinary);
                            file.Size = file.Contents.Length;
                            file.LastModified = DateTime.Now;
                        }
                    }
                    catch (Exception)
                    {
                    }
                    ;
                }

                ServerHandler.LogMessage("The cache of the URL '" + e.Name + "' has been updated.");
            };

            fileSystemWatcher.EnableRaisingEvents = true;
        }

        internal byte[] ReadFile(string filename, UTF8Encoding enc, bool isBinary = false)
        {
            int i = 10;

            while (i-- > 0) // Chris: if the file has currently been changed you probably have to wait until the writing process has finished
            {
                try
                {
                    if (isBinary)
                    {
                        return File.ReadAllBytes(Folder + filename);
                    }

                    if (Equals(GetEncoding(Folder + filename), Encoding.UTF8))
                    {
                        return File.ReadAllBytes(Folder + filename);
                    }

                    string content = File.ReadAllText(Folder + filename);
                    return Encoding.UTF8.GetBytes(content);
                }
                catch (IOException)
                {
                    Thread.Sleep(2); // Chris: if the file has currently been changed you probably have to wait until the writing process has finished
                }
            }

            throw new Exception("Failed to read from '" + filename + "'.");
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
