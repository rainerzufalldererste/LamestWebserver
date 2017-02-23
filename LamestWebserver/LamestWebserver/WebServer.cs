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
using System.Windows.Forms;
using LamestWebserver.Synchronization;
using ThreadState = System.Threading.ThreadState;

namespace LamestWebserver
{
    public class WebServer : IDisposable
    {
        internal static List<WebServer> RunningServers = new List<WebServer>();
        internal static UsableMutexSlim RunningServerMutex = new UsableMutexSlim();

        [ThreadStatic] public static string CurrentClientRemoteEndpoint = "<?>";

        TcpListener tcpListener;
        List<Thread> threads = new List<Thread>();
        Thread mThread;
        public int Port;
        public string folder = "./web";

        internal bool running
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

        private bool _running = true;
        private ReaderWriterLockSlim _runningLock = new ReaderWriterLockSlim();

        internal AVLTree<string, PreloadedFile> cache = new AVLTree<string, PreloadedFile>();

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

        private readonly byte[] crlf = new UTF8Encoding().GetBytes("\r\n");
        private Task<TcpClient> tcpRcvTask;

        private Random random = new Random();

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
            this.tcpListener = new TcpListener(IPAddress.Any, port);
            mThread = new Thread(new ThreadStart(HandleTcpListener));
            mThread.Start();
            this.silent = silent;
            this.folder = folder;

            if (useCache)
                SetupFileSystemWatcher();

            using (RunningServerMutex.Lock())
                RunningServers.Add(this);
        }

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

        public void Stop()
        {
            running = false;

            try
            {
                tcpListener.Stop();
            }
            catch
            {
            }

            try
            {
                Master.ForceQuitThread(mThread);
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

            int i = threads.Count;

            while (threads.Count > 0)
            {
                try
                {
                    Master.ForceQuitThread(threads[0]);
                }
                catch
                {
                }

                threads.RemoveAt(0);
            }

            using (RunningServerMutex.Lock())
                RunningServers.Remove(this);
            
            if (RunningServers.Count == 0)
                Master.StopServers();
        }

        public int GetThreadCount()
        {
            int num = 1;

            CleanThreads();

            for (int i = 0; i < threads.Count; i++)
            {
                if (threads[i] != null && threads[i].IsAlive)
                {
                    num++;
                }
            }

            return num;
        }

        public void CleanThreads()
        {
            cleanMutex.WaitOne();

            int threadCount = threads.Count;
            int i = 0;

            while (i < threads.Count)
            {
                if (threads[i] == null ||
                    threads[i].ThreadState == ThreadState.Running ||
                    threads[i].ThreadState == ThreadState.Unstarted ||
                    threads[i].ThreadState == ThreadState.AbortRequested)
                {
                    i++;
                }
                else
                {
                    try
                    {
                        threads[i].Abort();
                    }
                    catch (Exception)
                    {
                    }

                    networkStreamsMutex.WaitOne();

                    var networkStream = networkStreams[threads[i].ManagedThreadId];

                    if (networkStream != null)
                    {
                        try
                        {
                            networkStream.Close();
                        }
                        catch
                        {
                        }

                        networkStreams.Remove(threads[i].ManagedThreadId);
                    }

                    networkStreamsMutex.ReleaseMutex();

                    threads.RemoveAt(i);
                }
            }

            int threadCountAfter = threads.Count;

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
                tcpListener.Start();
            }
            catch (Exception e)
            {
                ServerHandler.LogMessage("The TcpListener couldn't be started. The Port is probably blocked.\n" + e);

                if (!silent)
                    Console.WriteLine("Failed to start TcpListener.\n" + e);
                return;
            }

            while (running)
            {
                try
                {
                    tcpRcvTask = tcpListener.AcceptTcpClientAsync();
                    tcpRcvTask.Wait();
                    TcpClient tcpClient = tcpRcvTask.Result;
                    Thread t = new Thread(HandleClient);
                    threads.Add(t);
                    t.Start((object) tcpClient);
                    ServerHandler.LogMessage("Client Connected: " + tcpClient.Client.RemoteEndPoint.ToString());

                    if (threads.Count%25 == 0)
                    {
                        threads.Add(new Thread(new ThreadStart(CleanThreads)));
                        threads[threads.Count - 1].Start();
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

            ThreadedWorker.CurrentWorker.EnqueueJob((Action<NetworkStream>) (networkStream =>
            {
                networkStreamsMutex.WaitOne();
                networkStreams.Add(Thread.CurrentThread.ManagedThreadId, networkStream);
                networkStreamsMutex.ReleaseMutex();
            }), nws);

            while (running)
            {
                msg = new byte[RequestMaxPacketSize];

                try
                {
                    bytes = nws.Read(msg, 0, RequestMaxPacketSize);
                }
                catch (ThreadAbortException e)
                {
                    break;
                }
                catch (Exception e)
                {
                    ServerHandler.LogMessage("An error occured in the client handler:  " + e);
                    break;
                }

                if (bytes == 0)
                {
                    break;
                }

                try
                {
                    string msg_ = enc.GetString(msg, 0, bytes);
                    HttpPacket htp = HttpPacket.Constructor(ref msg_, client.Client.RemoteEndPoint, lastmsg);

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

                            ServerHandler.LogMessage("Client requested an empty URL. We sent Error 501.");
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

                                ServerHandler.LogMessage("Client requested the URL '" + htp.RequestUrl + "'. (WebSocket Upgrade Request)");

                                var proxy = new WebSocketHandlerProxy(nws, currentWebSocketHandler, handler, (ushort) this.Port);

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

                                RetryGetData:

                                try
                                {
                                    SessionData sessionData = new SessionData(htp.VariablesHEAD, htp.VariablesPOST, htp.ValuesHEAD, htp.ValuesPOST, htp.Cookies, folder,
                                        htp.RequestUrl, msg_, client, nws, (ushort) this.Port);

                                    htp_.BinaryData = enc.GetBytes(currentRequest.Invoke(sessionData));

                                    if (sessionData.SetCookies.Count > 0)
                                    {
                                        htp_.Cookies = sessionData.SetCookies;
                                    }
                                }
                                catch (MutexRetryException e)
                                {
                                    ServerHandler.LogMessage("MutexRetryException. Retrying...");

                                    if (tries >= 10)
                                    {
                                        htp_.BinaryData = enc.GetBytes(Master.GetErrorMsg("Exception in Page Response for '"
                                                                                          + htp.RequestUrl + "'",
                                            $"<b>An Error occured while processing the output ({tries} Retries)</b><br>"
                                            + e.ToString().Replace("\r\n", "<br>")
#if DEBUG
                                            + "<hr><p>The Package you were sending:<br><div style='font-family:\"Consolas\",monospace;font-size: 13;color:#4C4C4C;'>"
                                            + msg_.Replace("\r\n", "<br>")
#endif
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
                                                                                      + htp.RequestUrl + "'", "<b>An Error occured while processing the output</b><br>"
                                                                                                                  + e.ToString().Replace("\r\n", "<br>")
#if DEBUG
                                                                                                                  +
                                                                                                                  "<hr><p>The Package you were sending:<br><div style='font-family:\"Consolas\",monospace;font-size: 13;color:#4C4C4C;'>"
                                                                                                                  + msg_.Replace("\r\n", "<br>")
#endif
                                                                                                                  + "</div></p>"));

                                    error = e;
                                }

                                SendPackage:

                                buffer = htp_.GetPackage(enc);
                                nws.Write(buffer, 0, buffer.Length);

                                if (error == null)
                                    ServerHandler.LogMessage("Client requested the URL '" + htp.RequestUrl + "'. (C# WebserverApi)");
                                else
                                    ServerHandler.LogMessage("Client requested the URL '" + htp.RequestUrl +
                                                             "'. (C# WebserverApi)\nThe URL crashed with the following Exception:\n" + error);
                            }
                            else
                            {
                                var response = GetFile(htp.RequestUrl, htp, enc, msg_);

                                if (response != null)
                                {
                                    buffer = response.GetPackage(enc);
                                    nws.Write(buffer, 0, buffer.Length);

                                    buffer = null;

                                    ServerHandler.LogMessage("Client requested the URL '" + htp.RequestUrl + "'.");
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
                                        RetryGetData:

                                        try
                                        {
                                            SessionData sessionData = new SessionData(htp.VariablesHEAD, htp.VariablesPOST, htp.ValuesHEAD, htp.ValuesPOST, htp.Cookies, folder,
                                                htp.RequestUrl, msg_, client, nws, (ushort) this.Port);

                                            htp_.BinaryData = enc.GetBytes(directory.Invoke(sessionData, subDir));

                                            if (sessionData.SetCookies.Count > 0)
                                            {
                                                htp_.Cookies = sessionData.SetCookies;
                                            }
                                        }
                                        catch (MutexRetryException e)
                                        {
                                            ServerHandler.LogMessage("MutexRetryException. Retrying...");

                                            if (tries >= 10)
                                            {
                                                htp_.BinaryData = enc.GetBytes(Master.GetErrorMsg("Exception in Page Response for '"
                                                                                                  + htp.RequestUrl + "'",
                                                    $"<b>An Error occured while processing the output ({tries} Retries)</b><br>"
                                                    + e.ToString().Replace("\r\n", "<br>")
#if DEBUG
                                                    + "<hr><p>The Package you were sending:<br><div style='font-family:\"Consolas\",monospace;font-size: 13;color:#4C4C4C;'>"
                                                    + msg_.Replace("\r\n", "<br>")
#endif
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
                                                "<b>An Error occured while processing the output</b><br>"
                                                + e.ToString().Replace("\r\n", "<br>")
#if DEBUG
                                                +
                                                "<hr><p>The Package you were sending:<br><div style='font-family:\"Consolas\",monospace;font-size: 13;color:#4C4C4C;'>"
                                                + msg_.Replace("\r\n", "<br>")
#endif
                                                + "</div></p>"));

                                            error = e;
                                        }

                                        SendPackage:

                                        buffer = htp_.GetPackage(enc);
                                        nws.Write(buffer, 0, buffer.Length);

                                        if (error == null)
                                            ServerHandler.LogMessage("Client requested the Directory URL '" + subDir + "' in Directory Page '" + bestUrlMatch +
                                                                     "'. (C# WebserverApi)");
                                        else
                                            ServerHandler.LogMessage("Client requested the Directory URL '" + subDir + "' in Directory Page '" + bestUrlMatch +
                                                                     "'. (C# WebserverApi)\nThe URL crashed with the following Exception:\n" + error);
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

                                        ServerHandler.LogMessage("Client requested the URL '" + htp.RequestUrl + "' which couldn't be found on the server. Retrieved Error 404.");
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
                        ServerHandler.LogMessage("An error occured in the client handler: " + e);
                    }
                }
                catch (ThreadAbortException)
                {
                    return;
                }
                catch (Exception e)
                {
                    ServerHandler.LogMessage("An error occured in the client handler: " + e);
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
                    else if (File.Exists(folder + fileName))
                    {
                        contents = ReadFile(fileName, enc);
                        lastModified = File.GetLastWriteTimeUtc(folder + fileName);

                        if (useCache)
                        {
                            using (CacheMutex.Lock())
                            {
                                cache.Add(fileName, new PreloadedFile(fileName, contents, contents.Length, lastModified.Value, false));
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
                else if (File.Exists(folder + fileName))
                {
                    extention = GetExtention(fileName);
                    bool isBinary = FileIsBinary(fileName, extention);
                    contents = ReadFile(fileName, enc, isBinary);
                    lastModified = File.GetLastWriteTimeUtc(folder + fileName);

                    if (useCache)
                    {
                        using (CacheMutex.Lock())
                        {
                            cache.Add(fileName, new PreloadedFile(fileName, contents, contents.Length, lastModified.Value, isBinary));
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
                        "<p>An Exception occurred while sending the response:<br></p><div style='font-family:\"Consolas\",monospace;font-size: 13;color:#4C4C4C;'>"
                        + e.ToString().Replace("\r\n", "<br>") + "</div><br>"
#if DEBUG
                        + "<hr><br><p>The Package you were sending:<br><div style='font-family:\"Consolas\",monospace;font-size: 13;color:#4C4C4C;'>" +
                        fullPacketString.Replace("\r\n", "<br>")
#endif
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
                if (cache.TryGetValue(name, out file))
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
                return cache.ContainsKey(name);
            }
        }

        internal void SetupFileSystemWatcher()
        {
            fileSystemWatcher = new FileSystemWatcher(folder);

            fileSystemWatcher.Renamed += (object sender, RenamedEventArgs e) =>
            {
                using (CacheMutex.Lock())
                {
                    PreloadedFile file, oldfile = cache["/" + e.OldName];

                    try
                    {
                        if (cache.TryGetValue(e.OldName, out file))
                        {
                            cache.Remove("/" + e.OldName);
                            file.Filename = "/" + e.Name;
                            file.Contents = ReadFile(file.Filename, new UTF8Encoding(), file.IsBinary);
                            file.Size = file.Contents.Length;
                            file.LastModified = File.GetLastWriteTimeUtc(folder + e.Name);
                            cache.Add(e.Name, file);
                        }
                    }
                    catch (Exception)
                    {
                        oldfile.Filename = "/" + e.Name;
                        cache["/" + e.Name] = oldfile;
                    }
                }

                ServerHandler.LogMessage("The URL '" + e.OldName + "' has been renamed to '" + e.Name + "' in the cache and filesystem.");
            };

            fileSystemWatcher.Deleted += (object sender, FileSystemEventArgs e) =>
            {
                using (CacheMutex.Lock())
                {
                    cache.Remove("/" + e.Name);
                }

                ServerHandler.LogMessage("The URL '" + e.Name + "' has been deleted from the cache and filesystem.");
            };

            fileSystemWatcher.Changed += (object sender, FileSystemEventArgs e) =>
            {
                using (CacheMutex.Lock())
                {
                    PreloadedFile file = cache["/" + e.Name];

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
                        return File.ReadAllBytes(folder + filename);
                    }

                    if (Equals(GetEncoding(folder + filename), Encoding.UTF8))
                    {
                        return File.ReadAllBytes(folder + filename);
                    }

                    string content = File.ReadAllText(folder + filename);
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
    }

    internal class PreloadedFile
    {
        public string Filename;
        public byte[] Contents;
        public int Size;
        public DateTime LastModified;
        public bool IsBinary;
        public int LoadCount;

        public PreloadedFile(string filename, byte[] contents, int size, DateTime lastModified, bool isBinary)
        {
            Filename = filename;
            Contents = contents;
            Size = size;
            LastModified = lastModified;
            IsBinary = isBinary;
            LoadCount = 1;
        }

        internal PreloadedFile Clone()
        {
            return new PreloadedFile((string) Filename.Clone(), Contents.ToArray(), Size, LastModified, IsBinary);
        }
    }
}
