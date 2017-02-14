using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Threading;
using System.Net.Sockets;
using System.Drawing;
using System.IO.Compression;
using LamestWebserver.ScriptHook;
using LamestWebserver.Collections;
using System.IO;
using System.Windows.Forms;

namespace LamestWebserver
{
    public class WebServer
    {
        [ThreadStatic]
        public static string CurrentClientRemoteEndpoint = "<?>";
        
        TcpListener tcpListener;
        List<Thread> threads = new List<Thread>();
        Thread mThread;
        public int port;
        public string folder = "./web";
        internal bool running = true;
        internal AVLTree<string, PreloadedFile> cache = new AVLTree<string, PreloadedFile>();

        private AVLHashMap<string, Master.getContents> pageResponses = new AVLHashMap<string, Master.getContents>(256);
        private QueuedAVLTree<string, Master.getContents> oneTimePageResponses = new QueuedAVLTree<string, Master.getContents>(4096);
        private AVLHashMap<string, WebSocketCommunicationHandler> webSocketResponses = new AVLHashMap<string, WebSocketCommunicationHandler>(64);

        private bool csharp_bridge = true;
        internal bool useCache = true;

        Mutex cleanMutex = new Mutex();
        private bool silent;

        private FileSystemWatcher fileSystemWatcher = null;
        private UsableMutex cacheMutex = new UsableMutex();

        private readonly byte[] crlf = new UTF8Encoding().GetBytes("\r\n");
        private Task<TcpClient> tcpRcvTask;

        private Random random = new Random();

        public WebServer(int port, string folder, bool silent = false) : this(port, folder, true, silent)
        { }

        internal WebServer(int port, string folder, bool cs_bridge, bool silent = false)
        {
            if (!TcpPortIsUnused(port))
            {
                if (!silent)
                    Console.WriteLine("Failed to start the WebServer. The tcp port " + port + " is currently used by another application.");

                throw new InvalidOperationException("The tcp port " + port + " is currently used.");
            }

            this.csharp_bridge = cs_bridge;

            if (cs_bridge)
            {
                Master.addFunctionEvent += AddFunction;
                Master.removeFunctionEvent += RemoveFunction;
                Master.addOneTimeFunctionEvent += AddOneTimeFunction;
            }

            // Websockets
            Master.AddWebsocketHandlerEvent += AddWebsocketHandler;
            Master.RemoveWebsocketHandlerEvent += RemoveWebsocketHandler;

            this.port = port;
            this.tcpListener = new TcpListener(IPAddress.Any, port);
            mThread = new Thread(new ThreadStart(HandleTcpListener));
            mThread.Start();
            this.silent = silent;
            this.folder = folder;

            if (useCache)
                setupFileSystemWatcher();

            if (!silent)
                Console.WriteLine("WebServer started on port " + port + ".");
        }

        ~WebServer()
        {
            if (csharp_bridge)
            {
                Master.addFunctionEvent -= AddFunction;
                Master.removeFunctionEvent -= RemoveFunction;
                Master.addOneTimeFunctionEvent -= AddOneTimeFunction;
            }

            try
            {
                StopServer();
            }
            catch (Exception) { }
        }

        public void StopServer()
        {
            running = false;

            try
            {
                tcpListener.Stop();
            }
            catch (Exception e)
            {
                if (!silent)
                    Console.WriteLine(port + ": " + e.Message);
            }

            try
            {
                Master.forceQuitThread(mThread);
            }
            catch (Exception e)
            {
                if (!silent)
                    Console.WriteLine(port + ": " + e.Message);
            }

            try
            {
                tcpRcvTask.Dispose();
            }
            catch (Exception) { }
            
            if(!silent)
                Console.WriteLine("Main Thread stopped! - port: " + port + " - folder: " + folder);

            int i = threads.Count;

            while (threads.Count > 0)
            {
                try
                {
                    Master.forceQuitThread(threads[0]);
                }
                catch (Exception e)
                {
                    if (!silent)
                        Console.WriteLine(port + ": " + e.Message);
                }
                threads.RemoveAt(0);

                if (!silent)
                    Console.WriteLine("Thread stopped! (" + (i - threads.Count) + "/" + i + ") - port: " + port + " - folder: " + folder);
            }
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
                    catch (Exception) { }

                    threads.RemoveAt(i);
                }
            }

            int threadCountAfter = threads.Count;

            cleanMutex.ReleaseMutex();

            ServerHandler.LogMessage("Cleaning up threads. Before: " + threadCount + ", After: " + threadCountAfter + ".");
        }

        public void AddFunction(string URL, Master.getContents getc)
        {
            pageResponses.Add(URL, getc);

            ServerHandler.LogMessage("The URL '" + URL + "' is now assigned to a Page. (WebserverApi)");
        }

        public void AddOneTimeFunction(string URL, Master.getContents getc)
        {
            oneTimePageResponses.Add(URL, getc);

            ServerHandler.LogMessage("The URL '" + URL + "' is now assigned to a Page. (WebserverApi/OneTimeFunction)");
        }

        public void RemoveFunction(string URL)
        {
            pageResponses.Remove(URL);

            ServerHandler.LogMessage("The URL '" + URL + "' is not assigned to a Page anymore. (WebserverApi)");
        }

        public void AddWebsocketHandler(WebSocketCommunicationHandler handler)
        {
            webSocketResponses.Add(handler.URL, handler);

            ServerHandler.LogMessage("The URL '" + handler.URL + "' is now assigned to a Page. (Websocket)");
        }

        public void RemoveWebsocketHandler(string URL)
        {
            webSocketResponses.Remove(URL);

            ServerHandler.LogMessage("The URL '" + URL + "' is not assigned to a Page anymore. (Websocket)");
        }

        private void HandleTcpListener()
        {
            try
            {
                tcpListener.Start();

            }
            catch (Exception e) { Console.ForegroundColor = ConsoleColor.Red; Console.WriteLine("The TcpListener couldn't be started. The Port is probably blocked.\n\n"); Console.ForegroundColor = ConsoleColor.White; Console.WriteLine(e + "\n"); return; };


            while (running)
            {
                try
                {
                    tcpRcvTask = tcpListener.AcceptTcpClientAsync();
                    tcpRcvTask.Wait();
                    TcpClient tcpClient = tcpRcvTask.Result;
                    Thread t = new Thread(handleClient);
                    threads.Add(t);
                    t.Start((object)tcpClient);
                    ServerHandler.LogMessage("Client Connected: " + tcpClient.Client.RemoteEndPoint.ToString());

                    if (threads.Count % 25 == 0)
                    {
                        threads.Add(new Thread(new ThreadStart(CleanThreads)));
                        threads[threads.Count - 1].Start();
                    }
                }
                catch(ThreadAbortException)
                {
                    break;
                }
                catch (Exception e)
                {
                    if (!silent)
                        Console.WriteLine("The TcpListener failed.\n" + e + "\n");
                }
            }
        }

        private void handleClient(object obj)
        {
            TcpClient client = (TcpClient)obj;
            NetworkStream nws = client.GetStream();
            UTF8Encoding enc = new UTF8Encoding();
            string lastmsg = null;
            WebServer.CurrentClientRemoteEndpoint = client.Client.RemoteEndPoint.ToString();

            byte[] msg;
            int bytes = 0;

            while (running)
            {
                msg = new byte[4096];

                try
                {
                    bytes = nws.Read(msg, 0, 4096);
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
                    HTTP_Packet htp = HTTP_Packet.Constructor(ref msg_, client.Client.RemoteEndPoint, lastmsg);

                    byte[] buffer;

                    try
                    {
                        if (htp.version == null)
                        {
                            lastmsg = msg_;
                        }
                        else if (htp.requestData == "")
                        {
                            lastmsg = null;

                            HTTP_Packet htp_ = new HTTP_Packet()
                            {
                                status = "501 Not Implemented",
                                binaryData = enc.GetBytes(Master.getErrorMsg(
                                    "Error 501: Not Implemented",
                                            "<p>The Feature that you were trying to use is not yet implemented.</p>" +
#if DEBUG
                                            "<p>The Package you were sending:<br><div style='font-family:\"Consolas\",monospace;font-size: 13;color:#4C4C4C;'>" + msg_.Replace("\r\n", "<br>") + 
#endif
                                            "</div></p>"))
                            };

                            buffer = htp_.getPackage(enc);
                            nws.Write(buffer, 0, buffer.Length);

                            ServerHandler.LogMessage("Client requested an empty URL. We sent Error 501.");
                        }
                        else
                        {
                            lastmsg = null;

                            while (htp.requestData.Length >= 2 && (htp.requestData[0] == ' ' || htp.requestData[0] == '/'))
                            {
                                htp.requestData = htp.requestData.Remove(0, 1);
                            }

                            Master.getContents currentRequest = pageResponses[htp.requestData];
                            WebSocketCommunicationHandler currentWebSocketHandler;

                            if (htp.IsWebsocketUpgradeRequest && webSocketResponses.TryGetValue(htp.requestData, out currentWebSocketHandler))
                            {
                                var handler =
                                    (Fleck.Handlers.ComposableHandler)
                                    Fleck.HandlerFactory.BuildHandler(Fleck.RequestParser.Parse(msg), currentWebSocketHandler._OnMessage, currentWebSocketHandler._OnClose,
                                        currentWebSocketHandler._OnBinary, currentWebSocketHandler._OnPing, currentWebSocketHandler._OnPong);
                                msg = handler.CreateHandshake();
                                nws.Write(msg, 0, msg.Length);

                                ServerHandler.LogMessage("Client requested the URL '" + htp.requestData + "'. (WebSocket Upgrade Request)");

                                var proxy = new WebSocketHandlerProxy(nws, currentWebSocketHandler, handler);

                                return;
                            }

                            if (currentRequest == null)
                            {
                                currentRequest = oneTimePageResponses[htp.requestData];

                                if (currentRequest != null)
                                    oneTimePageResponses.Remove(htp.requestData);
                            }

                            if (currentRequest != null)
                            {
                                HTTP_Packet htp_ = new HTTP_Packet();

                                Exception error = null;

                                int tries = 0;
                                RetryGetData:
                                try
                                {
                                    SessionData sessionData = new SessionData(htp.additionalHEAD, htp.additionalPOST, htp.valuesHEAD, htp.valuesPOST, htp.cookies, folder,
                                        htp.requestData, msg_, client, nws);
                                    htp_.binaryData = enc.GetBytes(currentRequest.Invoke(sessionData));

                                    if (sessionData.SetCookies.Count > 0)
                                    {
                                        htp_.cookies = sessionData.SetCookies;
                                    }
                                }
                                catch (MutexRetryException e)
                                {
                                    ServerHandler.LogMessage("MutexRetryException. Retrying...");

                                    if (tries >= 10)
                                    {
                                        htp_.binaryData = enc.GetBytes(Master.getErrorMsg("Exception in Page Response for '"
                                        + htp.requestData + "'", $"<b>An Error occured while processing the output ({tries} Retries)</b><br>"
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
                                    Thread.Sleep(random.Next((25) * tries));
                                    goto RetryGetData;
                                }
                                catch (Exception e)
                                {
                                    htp_.binaryData = enc.GetBytes(Master.getErrorMsg("Exception in Page Response for '"
                                        + htp.requestData + "'", "<b>An Error occured while processing the output</b><br>"
                                        + e.ToString().Replace("\r\n", "<br>")
#if DEBUG
                                        + "<hr><p>The Package you were sending:<br><div style='font-family:\"Consolas\",monospace;font-size: 13;color:#4C4C4C;'>"
                                        + msg_.Replace("\r\n", "<br>")
#endif
                                        + "</div></p>"));

                                    error = e;
                                }

                                SendPackage:

                                buffer = htp_.getPackage(enc);
                                nws.Write(buffer, 0, buffer.Length);
                                
                                if(error == null)
                                    ServerHandler.LogMessage("Client requested the URL '" + htp.requestData + "'. (C# WebserverApi)");
                                else
                                    ServerHandler.LogMessage("Client requested the URL '" + htp.requestData + "'. (C# WebserverApi)\nThe URL crashed with the following Exception:\n" + error);
                            }
                            else if (htp.requestData.Length > 3 && htp.requestData.Substring(htp.requestData.Length - 4).ToLower() == ".hcs" && File.Exists((folder != "/" ? folder : "") + "/" + htp.requestData))
                            {
                                string result = "";
                                Exception error = null;

                                try
                                {
                                    result = Hook.resolveScriptFromFile(folder + htp.requestData, new SessionData(htp.additionalHEAD, htp.additionalPOST, htp.valuesHEAD, htp.valuesPOST, htp.cookies, folder, htp.requestData, msg_, client, nws));
                                }
                                catch (Exception e)
                                {
                                    result = Master.getErrorMsg("Exception in C# Script for '"
                                        + htp.requestData + "'", "<b>An Error occured while processing the output</b><br>"
                                        + e.ToString().Replace("\r\n", "<br>")
#if DEBUG
                                        + "<hr><p>The Package you were sending:<br><div style='font-family:\"Consolas\",monospace;font-size: 13;color:#4C4C4C;'>"
                                        + msg_.Replace("\r\n", "<br>")
#endif
                                        + "</div></p>");

                                    error = e;
                                }

                                HTTP_Packet htp_ = new HTTP_Packet() { binaryData = enc.GetBytes(result) };
                                buffer = htp_.getPackage(enc);
                                nws.Write(buffer, 0, buffer.Length);

                                buffer = null;
                                result = null;

                                if (error == null)
                                    ServerHandler.LogMessage("Client requested the URL '" + htp.requestData + "'. (C# Script)");
                                else
                                    ServerHandler.LogMessage("Client requested the URL '" + htp.requestData + "'. (C# Script)\nThe URL crashed with the following Exception:\n" + error);
                            }
                            else
                            {
                                buffer = GetFile(htp.requestData, htp, enc, msg_).getPackage(enc);
                                nws.Write(buffer, 0, buffer.Length);

                                buffer = null;

                                ServerHandler.LogMessage("Client requested the URL '" + htp.requestData + "'.");
                            }
                        }
                    }
                    catch(ThreadAbortException)
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

        private HTTP_Packet GetFile(string URL, HTTP_Packet requestPacket, UTF8Encoding enc, string fullPacketString)
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

                    if (useCache && getFromCache(fileName, out file))
                    {
                        lastModified = file.LastModified;

                        if (requestPacket.modified != null && requestPacket.modified.Value < lastModified)
                        {
                            contents = file.Contents;

                            extention = getExtention(fileName);
                        }
                        else
                        {
                            contents = file.Contents;

                            notModified = requestPacket.modified != null;
                        }
                    }
                    else if (File.Exists(folder + fileName))
                    {
                        contents = ReadFile(fileName, enc);
                        lastModified = File.GetLastWriteTimeUtc(folder + fileName);

                        if (useCache)
                        {
                            using (cacheMutex.Lock())
                            {
                                cache.Add(fileName, new PreloadedFile(fileName, contents, contents.Length, lastModified.Value, false));
                            }
                        }
                    }
                    else
                    {
                        return new HTTP_Packet()
                        {
                            status = "403 Forbidden",
                            binaryData = enc.GetBytes(Master.getErrorMsg(
                                "Error 403: Forbidden",
                                "<p>The Requested URL cannot be delivered due to insufficient priveleges.</p>" +
#if DEBUG
                                "<p>The Package you were sending:<br><div style='font-family:\"Consolas\",monospace;font-size: 13;color:#4C4C4C;'>" + fullPacketString.Replace("\r\n", "<br>") + 
#endif
                                "</div></p>"))
                        };
                    }
                }
                else if (useCache && getFromCache(fileName, out file))
                {
                    extention = getExtention(fileName);
                    lastModified = file.LastModified;

                    if (requestPacket.modified != null && requestPacket.modified.Value < lastModified)
                    {
                        contents = file.Contents;

                        extention = getExtention(fileName);
                    }
                    else
                    {
                        contents = file.Contents;

                        notModified = requestPacket.modified != null;
                    }
                }
                else if (File.Exists(folder + fileName))
                {
                    extention = getExtention(fileName);
                    bool isBinary = fileIsBinary(fileName, extention);
                    contents = ReadFile(fileName, enc, isBinary);
                    lastModified = File.GetLastWriteTimeUtc(folder + fileName);

                    if (useCache)
                    {
                        using (cacheMutex.Lock())
                        {
                            cache.Add(fileName, new PreloadedFile(fileName, contents, contents.Length, lastModified.Value, isBinary));
                        }
                        
                        ServerHandler.LogMessage("The URL '" + URL + "' is now available through the cache.");
                    }
                }
                else
                {
                    return new HTTP_Packet()
                    {
                        status = "404 File Not Found",
                        binaryData = enc.GetBytes(Master.getErrorMsg(
                            "Error 404: Page Not Found",
                            "<p>The URL you requested did not match any page or file on the server.</p>" +
#if DEBUG
                                "<p>The Package you were sending:<br><div style='font-family:\"Consolas\",monospace;font-size: 13;color:#4C4C4C;'>" + fullPacketString.Replace("\r\n", "<br>") + 
#endif
                                "</div></p>"))
                    };
                }

                if (notModified)
                {
                    return new HTTP_Packet() { status = "304 Not Modified", contentType = null, modified = lastModified, binaryData = crlf };
                }
                else
                {
                    return new HTTP_Packet() { contentType = getMimeType(extention), binaryData = contents, modified = lastModified };
                }
            }
            catch (Exception e)
            {
                return new HTTP_Packet()
                {
                    status = "500 Internal Server Error",
                    binaryData = enc.GetBytes(Master.getErrorMsg(
                        "Error 500: Internal Server Error",
                        "<p>An Exception occurred while sending the response:<br></p><div style='font-family:\"Consolas\",monospace;font-size: 13;color:#4C4C4C;'>"
                        + e.ToString().Replace("\r\n", "<br>") + "</div><br>"
#if DEBUG
                        + "<hr><br><p>The Package you were sending:<br><div style='font-family:\"Consolas\",monospace;font-size: 13;color:#4C4C4C;'>" + fullPacketString.Replace("\r\n", "<br>")
#endif
                        + "</div>"))
                };
            }
        }

        internal bool fileIsBinary(string fileName, string extention)
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
                default: return true;
            }
        }

        private string getExtention(string fileName)
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

        internal bool getFromCache(string name, out PreloadedFile file)
        {
            using (cacheMutex.Lock())
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

        internal string getMimeType(string extention)
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

        internal bool cacheHas(string name)
        {
            using (cacheMutex.Lock())
            {
                return cache.ContainsKey(name);
            }
        }

        internal void setupFileSystemWatcher()
        {
            fileSystemWatcher = new FileSystemWatcher(folder);

            fileSystemWatcher.Renamed += (object sender, RenamedEventArgs e) =>
            {
                using (cacheMutex.Lock())
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
                    catch(Exception)
                    {
                        oldfile.Filename = "/" + e.Name;
                        cache["/" + e.Name] = oldfile;
                    }
                }

                ServerHandler.LogMessage("The URL '" + e.OldName + "' has been renamed to '" + e.Name + "' in the cache and filesystem.");
            };

            fileSystemWatcher.Deleted += (object sender, FileSystemEventArgs e) =>
            {
                using (cacheMutex.Lock())
                {
                    cache.Remove("/" + e.Name);
                }

                ServerHandler.LogMessage("The URL '" + e.Name + "' has been deleted from the cache and filesystem.");
            };

            fileSystemWatcher.Changed += (object sender, FileSystemEventArgs e) =>
            {
                using (cacheMutex.Lock())
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
                    catch (Exception) { };
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
            return new PreloadedFile((string)Filename.Clone(), Contents.ToArray(), Size, LastModified, IsBinary);
        }
    }
}
