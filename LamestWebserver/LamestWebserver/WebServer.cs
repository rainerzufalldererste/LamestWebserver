using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Threading;
using System.Net.Sockets;
using System.Windows.Forms;
using System.Drawing;
using System.IO.Compression;
using System.Drawing.Imaging;
using LamestWebserver.ScriptHook;
using LamestWebserver.Collections;
using System.IO;

namespace LamestWebserver
{
    public class WebServer
    {
        TcpListener tcpListener;
        List<Thread> threads = new List<Thread>();
        Thread mThread;
        public int port;
        public string folder = "./web";
        internal bool running = true;
        internal AVLTree<string, PreloadedFile> cache = new AVLTree<string, PreloadedFile>();

        private AVLHashMap<string, Master.getContents> pageResponses = new AVLHashMap<string, Master.getContents>(256);
        private QueuedAVLTree<string, Master.getContents> oneTimePageResponses = new QueuedAVLTree<string, Master.getContents>(4096);

        private bool csharp_bridge = true;
        internal bool useCache = true;

        Mutex cleanMutex = new Mutex();
        private bool silent;

        private FileSystemWatcher fileSystemWatcher = null;
        private UsableMutex cacheMutex = new UsableMutex();

        private readonly byte[] crlf = new UTF8Encoding().GetBytes("\r\n");
        private Task<TcpClient> tcpRcvTask;

        public WebServer(int port, string folder, bool silent = false)
        {
            if (!tcpPortIsUnused(port))
            {
                if (!silent)
                    Console.WriteLine("Failed to start the WebServer. The tcp port " + port + " is currently used.");

                throw new InvalidOperationException("The tcp port " + port + " is currently used.");
            }

            this.csharp_bridge = true;

            Master.addFunctionEvent += addFunction;
            Master.removeFunctionEvent += removeFunction;
            Master.addOneTimeFunctionEvent += addOneTimeFunction;

            this.port = port;
            tcpListener = new TcpListener(IPAddress.Any, port);
            mThread = new Thread(new ThreadStart(handleTcpListener));
            mThread.Start();
            this.silent = silent;

            if (useCache)
                setupFileSystemWatcher();

            if (!silent)
                Console.WriteLine("WebServer started on port " + port + ".");

            while (folder.EndsWith("\\") || folder.EndsWith("/"))
            {
                folder.Remove(folder.Length - 1);
            }
        }

        internal WebServer(int port, string folder, bool cs_bridge, bool silent = false)
        {
            if (!tcpPortIsUnused(port))
            {
                if (!silent)
                    Console.WriteLine("Failed to start the WebServer. The tcp port " + port + " is currently used.");

                throw new InvalidOperationException("The tcp port " + port + " is currently used.");
            }

            this.csharp_bridge = cs_bridge;

            if (cs_bridge)
            {
                Master.addFunctionEvent += addFunction;
                Master.removeFunctionEvent += removeFunction;
                Master.addOneTimeFunctionEvent += addOneTimeFunction;
            }

            this.port = port;
            this.tcpListener = new TcpListener(IPAddress.Any, port);
            mThread = new Thread(new ThreadStart(handleTcpListener));
            mThread.Start();
            this.silent = silent;

            if (useCache)
                setupFileSystemWatcher();

            if (!silent)
                Console.WriteLine("WebServer started on port " + port + ".");
        }

        ~WebServer()
        {
            if (csharp_bridge)
            {
                Master.addFunctionEvent -= addFunction;
                Master.removeFunctionEvent -= removeFunction;
                Master.addOneTimeFunctionEvent -= addOneTimeFunction;
            }

            try
            {
                stopServer();
            }
            catch (Exception) { }
        }

        public void stopServer()
        {
            running = false;

            try
            {
                tcpListener.Stop();
            }
            catch (Exception e) { Console.WriteLine(port + ": " + e.Message); }

            try
            {
                mThread.Abort();
            }
            catch (Exception e) { Console.WriteLine(port + ": " + e.Message); }

            try
            {
                tcpRcvTask.Dispose();
            }
            catch (Exception) { }

            Console.WriteLine("Main Thread stopped! - port: " + port + " - folder: " + folder);

            int i = threads.Count;

            while (threads.Count > 0)
            {
                try
                {
                    threads[0].Abort();
                }
                catch (Exception e) { Console.WriteLine(port + ": " + e.Message); }
                threads.RemoveAt(0);

                Console.WriteLine("Thread stopped! (" + (i - threads.Count) + "/" + i + ") - port: " + port + " - folder: " + folder);
            }
        }

        public int getThreadCount()
        {
            int num = 1;

            cleanThreads();

            for (int i = 0; i < threads.Count; i++)
            {
                if (threads[i] != null && threads[i].IsAlive)
                {
                    num++;
                }
            }

            return num;
        }

        public void cleanThreads()
        {
            cleanMutex.WaitOne();

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

            cleanMutex.ReleaseMutex();
        }

        public void addFunction(string hash, Master.getContents getc)
        {
            pageResponses.Add(hash, getc);
        }

        public void addOneTimeFunction(string hash, Master.getContents getc)
        {
            oneTimePageResponses.Add(hash, getc);
        }

        public void removeFunction(string hash)
        {
            pageResponses.Remove(hash);
        }

        private void handleTcpListener()
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
                    Thread t = new Thread(new ParameterizedThreadStart(handleClient));
                    threads.Add(t);
                    t.Start((object)tcpClient);
                    ServerHandler.addToStuff("Client Connected: " + tcpClient.Client.RemoteEndPoint.ToString());

                    if (threads.Count % 25 == 0)
                    {
                        threads.Add(new Thread(new ThreadStart(cleanThreads)));
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
                    ServerHandler.addToStuff("An error occured in the client handler:  " + e);
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
                                            "<p>The Feature that you were trying to use is not yet implemented.</p><p>The Package you were sending:<br><div style='font-family:\"Consolas\",monospace;font-size: 13;color:#4C4C4C;'>" + msg_.Replace("\r\n", "<br>") + "</div></p>"))
                            };

                            buffer = htp_.getPackage(enc);
                            nws.Write(buffer, 0, buffer.Length);
                        }
                        else
                        {
                            lastmsg = null;

                            while (htp.requestData.Length >= 2 && (htp.requestData[0] == ' ' || htp.requestData[0] == '/'))
                            {
                                htp.requestData = htp.requestData.Remove(0, 1);
                            }

                            Master.getContents currentRequest = pageResponses[htp.requestData];

                            if (currentRequest == null)
                            {
                                currentRequest = oneTimePageResponses[htp.requestData];

                                if (currentRequest != null)
                                    oneTimePageResponses.Remove(htp.requestData);
                            }

                            if (currentRequest != null)
                            {
                                HTTP_Packet htp_ = new HTTP_Packet();

                                try
                                {
                                    SessionData sessionData = new SessionData(htp.additionalHEAD, htp.additionalPOST, htp.valuesHEAD, htp.valuesPOST, htp.cookies, folder, htp.requestData, msg_, client, nws);
                                    htp_.binaryData = enc.GetBytes(currentRequest.Invoke(sessionData));

                                    if (sessionData.Cookies.Count > 0)
                                    {
                                        htp_.cookies = sessionData.Cookies;
                                    }
                                }
                                catch (Exception e)
                                {
                                    htp_.binaryData = enc.GetBytes(Master.getErrorMsg("Exception in Page Response for '"
                                        + htp.requestData + "'", "<b>An Error occured while processing the output</b><br>"
                                        + e.ToString() + "<hr><p>The Package you were sending:<br><div style='font-family:\"Consolas\",monospace;font-size: 13;color:#4C4C4C;'>"
                                        + msg_.Replace("\r\n", "<br>") + "</div></p>"));
                                }

                                buffer = htp_.getPackage(enc);
                                nws.Write(buffer, 0, buffer.Length);
                            }
                            else if (htp.requestData.Length > 3 && htp.requestData.Substring(htp.requestData.Length - 4).ToLower() == ".hcs" && File.Exists((folder != "/" ? folder : "") + "/" + htp.requestData))
                            {
                                string result = "";

                                try
                                {
                                    result = Hook.resolveScriptFromFile(folder + htp.requestData, new SessionData(htp.additionalHEAD, htp.additionalPOST, htp.valuesHEAD, htp.valuesPOST, htp.cookies, folder, htp.requestData, msg_, client, nws));
                                }
                                catch (Exception e)
                                {
                                    result = Master.getErrorMsg("Exception in C# Script for '"
                                        + htp.requestData + "'", "<b>An Error occured while processing the output</b><br>"
                                        + e.ToString() + "<hr><p>The Package you were sending:<br><div style='font-family:\"Consolas\",monospace;font-size: 13;color:#4C4C4C;'>"
                                        + msg_.Replace("\r\n", "<br>") + "</div></p>");
                                }

                                HTTP_Packet htp_ = new HTTP_Packet() { binaryData = enc.GetBytes(result) };
                                buffer = htp_.getPackage(enc);
                                nws.Write(buffer, 0, buffer.Length);

                                buffer = null;
                                result = null;
                            }
                            else
                            {
                                buffer = getFile(htp.requestData, htp, enc, msg_).getPackage(enc);
                                nws.Write(buffer, 0, buffer.Length);

                                buffer = null;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        ServerHandler.addToStuff("An error occured in the client handler: " + e);
                    }
                }
                catch (Exception e)
                {
                    ServerHandler.addToStuff("An error occured in the client handler: " + e);
                }
            }
        }

        private HTTP_Packet getFile(string URL, HTTP_Packet requestPacket, UTF8Encoding enc, string fullPacketString)
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
                        lastModified = file.lastModified;

                        if (requestPacket.modified != null && requestPacket.modified.Value < lastModified)
                        {
                            contents = file.contents;

                            extention = getExtention(fileName);
                        }
                        else
                        {
                            contents = file.contents;

                            notModified = requestPacket.modified != null;
                        }
                    }
                    else if (File.Exists(folder + fileName))
                    {
                        contents = readFile(fileName, enc, false);
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
                                "<p>The Requested URL cannot be delivered due to insufficient priveleges.</p><p>The Package you were sending:<br><div style='font-family:\"Consolas\",monospace;font-size: 13;color:#4C4C4C;'>" + fullPacketString.Replace("\r\n", "<br>") + "</div></p>"))
                        };
                    }
                }
                else if (useCache && getFromCache(fileName, out file))
                {
                    extention = getExtention(fileName);
                    lastModified = file.lastModified;

                    if (requestPacket.modified != null && requestPacket.modified.Value < lastModified)
                    {
                        contents = file.contents;

                        extention = getExtention(fileName);
                    }
                    else
                    {
                        contents = file.contents;

                        notModified = requestPacket.modified != null;
                    }
                }
                else if (File.Exists(folder + fileName))
                {
                    extention = getExtention(fileName);
                    bool isBinary = fileIsBinary(fileName, extention);
                    contents = readFile(fileName, enc, isBinary);
                    lastModified = File.GetLastWriteTimeUtc(folder + fileName);

                    if (useCache)
                    {
                        using (cacheMutex.Lock())
                        {
                            cache.Add(fileName, new PreloadedFile(fileName, contents, contents.Length, lastModified.Value, isBinary));
                        }
                    }
                }
                else
                {
                    return new HTTP_Packet()
                    {
                        status = "404 File Not Found",
                        binaryData = enc.GetBytes(Master.getErrorMsg(
                            "Error 404: Page Not Found",
                            "<p>The URL you requested did not match any page or file on the server.</p><p>The Package you were sending:<br><div style='font-family:\"Consolas\",monospace;font-size: 13;color:#4C4C4C;'>" + fullPacketString.Replace("\r\n", "<br>") + "</div></p>"))
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
                        "<p>An Exception occurred while sending the response:<br></p><div style='font-family:\"Consolas\",monospace;font-size: 13;color:#4C4C4C;'>" + e.ToString().Replace("\r\n", "<br>") + "</div><br><hr><br><p>The Package you were sending:<br><div style='font-family:\"Consolas\",monospace;font-size: 13;color:#4C4C4C;'>" + fullPacketString.Replace("\r\n", "<br>") + "</div>"))
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
                    file.loadCount++;
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
                            file.filename = "/" + e.Name;
                            file.contents = readFile(file.filename, new UTF8Encoding(), file.isBinary);
                            file.size = file.contents.Length;
                            file.lastModified = File.GetLastWriteTimeUtc(folder + e.Name);
                            cache.Add(e.Name, file);
                        }
                    }
                    catch(Exception)
                    {
                        oldfile.filename = "/" + e.Name;
                        cache["/" + e.Name] = oldfile;
                    }
                }
            };

            fileSystemWatcher.Deleted += (object sender, FileSystemEventArgs e) =>
            {
                using (cacheMutex.Lock())
                {
                    cache.Remove("/" + e.Name);
                }
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
                            file.contents = readFile(file.filename, new UTF8Encoding(), file.isBinary);
                            file.size = file.contents.Length;
                            file.lastModified = DateTime.Now;
                        }
                    }
                    catch (Exception) { };
                }
            };

            fileSystemWatcher.EnableRaisingEvents = true;
        }

        internal byte[] readFile(string filename, UTF8Encoding enc, bool isBinary = false)
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
                    else
                    {
                        if (GetEncoding(folder + filename) == Encoding.UTF8)
                        {
                            return File.ReadAllBytes(folder + filename);
                        }
                        else
                        {
                            string content = File.ReadAllText(folder + filename);
                            UTF8Encoding utf8Encoding = new UTF8Encoding();
                            return utf8Encoding.GetBytes(content);
                        }
                    }
                }
                catch (IOException)
                {
                    System.Threading.Thread.Sleep(2);
                }
            }

            throw new Exception("Failed to read from '" + filename + "'.");
        }

        /// <summary>
        /// Source: http://stackoverflow.com/questions/570098/in-c-how-to-check-if-a-tcp-port-is-available
        /// </summary>
        /// <param name="port">The TCP-Port to check for</param>
        /// <returns>true if unused</returns>
        public static bool tcpPortIsUnused(int port)
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
        public string filename;
        public byte[] contents;
        public int size;
        public DateTime lastModified;
        public bool isBinary;
        public int loadCount;

        public PreloadedFile(string filename, byte[] contents, int size, DateTime lastModified, bool isBinary)
        {
            this.filename = filename;
            this.contents = contents;
            this.size = size;
            this.lastModified = lastModified;
            this.isBinary = isBinary;
            this.loadCount = 1;
        }

        internal PreloadedFile Clone()
        {
            return new PreloadedFile((string)filename.Clone(), contents.ToArray(), size, lastModified, isBinary);
        }
    }
}
