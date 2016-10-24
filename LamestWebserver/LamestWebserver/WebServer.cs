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
using LamestScriptHook;
using LamestWebserver.Collections;

namespace LamestWebserver
{
    public class WebServer
    {
        TcpListener tcpListener;
        List<Thread> threads = new List<Thread>();
        Thread mThread;
        public int port;
        public string folder = "./web";
        public bool running = true;
        public bool clearing = false;
        public AVLTree<string, PreloadedFile> cache = new AVLTree<string, PreloadedFile>(); // TODO: Change to AVLTree and use FileSystemWatcher
        public int max_cache = 500;
        public bool openPaths = true;
        public System.Diagnostics.Process process = null;
        public bool processIsInit = false;
        public string lastCmdOut = "";

        private List<string> hashes = new List<string>();
        private List<Master.getContents> functions = new List<Master.getContents>();
        private bool csharp_bridge = true;
        internal bool useCache = false;

        Mutex cleanMutex = new Mutex();
        private bool silent;

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

            this.port = port;
            tcpListener = new TcpListener(IPAddress.Any, port);
            mThread = new Thread(new ThreadStart(ListenAndStuff));
            mThread.Start();
            this.silent = silent;

            if (!silent)
                Console.WriteLine("WebServer started on port " + port + ".");
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
            }

            this.port = port;
            this.tcpListener = new TcpListener(IPAddress.Any, port);
            mThread = new Thread(new ThreadStart(ListenAndStuff));
            mThread.Start();
            this.silent = silent;

            if (!silent)
                Console.WriteLine("WebServer started on port " + port + ".");
        }

        ~WebServer()
        {
            if (csharp_bridge)
            {
                Master.addFunctionEvent -= addFunction;
                Master.removeFunctionEvent -= removeFunction;

                for (int i = 0; i < threads.Count; i++)
                {
                    try
                    {
                        threads[i].Abort();
                    }
                    catch (Exception) { }
                }
            }
        }

        public bool cacheHas(string name, out PreloadedFile file)
        {
            if (!useCache)
            {
                file = default(PreloadedFile);
                return false;
            }

            return cache.TryGetValue(name, out file);
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
            int id = -1;

            for (int i = 0; i < hashes.Count; i++)
            {
                if (hashes[i] == hash)
                {
                    id = i;
                    break;
                }
            }

            if(id > -1)
            {
                functions[id] = getc;
            }
            else
            {
                hashes.Add(hash);
                functions.Add(getc);
            }
        }

        public void removeFunction(string hash)
        {
            for (int i = 0; i < hashes.Count; i++)
            {
                if(hashes[i] == hash)
                {
                    hashes.RemoveAt(i);
                    functions.RemoveAt(i);
                    return;
                }
            }
        }

        private void ListenAndStuff()
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
                    //if (tcpListener.Pending())
                    //{
                        // TcpClient tcpClient = tcpListener.AcceptTcpClient();
                        Task<TcpClient> tcpRcvTask = tcpListener.AcceptTcpClientAsync();
                        tcpRcvTask.Wait();
                        TcpClient tcpClient = tcpRcvTask.Result;
                        Thread t = new Thread(new ParameterizedThreadStart(DoStuff));
                        threads.Add(t);
                        t.Start((object)tcpClient);
                        ServerHandler.addToStuff("Client Connected: " + tcpClient.Client.RemoteEndPoint.ToString());

                        if (threads.Count % 25 == 0)
                        {
                            threads.Add(new Thread(new ThreadStart(cleanThreads)));
                            threads[threads.Count - 1].Start();
                        }
                    /*}
                    else
                    {
                        Thread.Sleep(1);
                    }*/
                }
                catch (Exception e)
                {
                    if(!silent)
                        Console.WriteLine("The TcpListener failed.\n" + e + "\n");
                }
            }
        }

        private void DoStuff(object obj)
        {
            TcpClient client = (TcpClient)obj;
            NetworkStream nws = client.GetStream();
            UTF8Encoding enc = new UTF8Encoding();

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
                    HTTP_Packet htp = HTTP_Packet.Constructor(ref msg_, client.Client.RemoteEndPoint);

                    byte[] buffer;

                    try
                    {
                        if(htp.version == "POST_PACKET_INCOMING")
                        {
                            continue;
                        }
                        else if (htp.data == "")
                        {
                            HTTP_Packet htp_ = new HTTP_Packet()
                            {
                                status = "501 Not Implemented",
                                data = Master.getErrorMsg(
                                    "Error 501: Not Implemented",
                                            "<p>The Package you were sending:<br><div style='font-family:\"Consolas\",monospace;font-size: 13;color:#4C4C4C;'>" + msg_.Replace("\r\n", "<br>") + "</div></p><hr><p>I guess you don't know what that means. You're welcome! I'm done here!</p>")
                            };

                            htp_.contentLength = enc.GetByteCount(htp_.data);
                            buffer = enc.GetBytes(htp_.getPackage());
                            nws.Write(buffer, 0, buffer.Length);
                        }
                        else
                        {
                            int hashNUM = 0;
                            bool found = false;

                            while(htp.data.Length >= 2 && (htp.data[0] == ' ' || htp.data[0] == '/'))
                            {
                                htp.data = htp.data.Remove(0, 1);
                            }

                            for (; hashNUM < hashes.Count; hashNUM++)
                            {
                                if (hashes[hashNUM] == htp.data)
                                {
                                    found = true;
                                    break;
                                }
                            }

                            if (found)
                            {
                                HTTP_Packet htp_ = new HTTP_Packet();

                                try
                                {
                                    htp_.data = functions[hashNUM].Invoke(new SessionData(htp.additionalHEAD, htp.additionalPOST, htp.valuesHEAD, htp.valuesPOST, folder, htp.data, msg_, client, nws));
                                }
                                catch(Exception e)
                                {
                                    htp_.data = Master.getErrorMsg("Exception in Page Response for '"
                                        + htp.data + "'", "<b>An Error occured while processing the output</b><br>"
                                        + e.ToString() + "<hr><p>The Package you were sending:<br><div style='font-family:\"Consolas\",monospace;font-size: 13;color:#4C4C4C;'>"
                                        + msg_.Replace("\r\n", "<br>") + "</div></p>");
                                }
                                
                                htp_.contentLength = enc.GetByteCount(htp_.data);
                                buffer = enc.GetBytes(htp_.getPackage());
                                nws.Write(buffer, 0, buffer.Length);
                            }
                            else if (htp.data[htp.data.Length - 1] == '\\' || htp.data[htp.data.Length - 1] == '/')
                            {
                                PreloadedFile cachedFile;
                                bool cached = cacheHas(folder + htp.data + "index.html", out cachedFile);

                                if (cached)
                                {
                                    HTTP_Packet htp_ = new HTTP_Packet() { data = cachedFile.contents, contentLength = cachedFile.size };
                                    buffer = enc.GetBytes(htp_.getPackage());
                                    nws.Write(buffer, 0, buffer.Length);
                                }
                                else
                                {
                                    if (System.IO.File.Exists(folder + htp.data + "index.html"))
                                    {
                                        string s = System.IO.File.ReadAllText(folder + htp.data + "index.html");
                                        HTTP_Packet htp_ = new HTTP_Packet() { data = s, contentLength = enc.GetBytes(s).Length };
                                        buffer = enc.GetBytes(htp_.getPackage());
                                        nws.Write(buffer, 0, buffer.Length);

                                        if (useCache && cache.Count < max_cache)
                                        {
                                            string name = folder + htp.data + "index.html";
                                            cache.Add(name, new PreloadedFile(name, s, htp_.contentLength));
                                        }

                                        buffer = null;
                                    }
                                    else
                                    {
                                        HTTP_Packet htp_ = new HTTP_Packet()
                                        {
                                            status = "403 Forbidden",
                                            data = Master.getErrorMsg(
                                                "Error 403: Forbidden",
                                            "<p>The Package you were sending:<br><div style='font-family:\"Consolas\",monospace;font-size: 13;color:#4C4C4C;'>" + msg_.Replace("\r\n", "<br>") + "</div></p><hr><p>I guess you don't know what that means. You're welcome! I'm done here!</p>")
                                        };

                                        htp_.contentLength = enc.GetBytes(htp_.data).Length;
                                        buffer = enc.GetBytes(htp_.getPackage());
                                        nws.Write(buffer, 0, buffer.Length);

                                        buffer = null;
                                    }
                                }
                            }
                            else
                            {
                                PreloadedFile cachedFile;
                                bool cached = this.cacheHas(folder + htp.data, out cachedFile);

                                if (cached)
                                {
                                    HTTP_Packet htp_ = new HTTP_Packet() { data = cachedFile.contents, contentLength = cachedFile.size };
                                    buffer = enc.GetBytes(htp_.getPackage());
                                    nws.Write(buffer, 0, buffer.Length);
                                }
                                else if (System.IO.File.Exists((folder != "/" ? folder : "") + "/" + htp.data))
                                {
                                    if (htp.data.Substring(htp.data.Length - 4) == ".bmp")
                                    {
                                        byte[] b = System.IO.File.ReadAllBytes((folder != "/" ? folder : "") + "/" + htp.data);

                                        HTTP_Packet htp_ = new HTTP_Packet() { contentLength = b.Length, contentType = "img/Bitmap", data = "" };
                                        List<byte> blist = enc.GetBytes(htp_.getPackage()).ToList();
                                        blist.AddRange(b);
                                        blist.AddRange(enc.GetBytes("\r\n"));
                                        buffer = blist.ToArray();
                                        blist = null;
                                        nws.Write(buffer, 0, buffer.Length);

                                        blist = null;
                                        buffer = null;
                                        b = null;
                                    }
                                    else if (htp.data.Substring(htp.data.Length - 4) == ".jpg" || htp.data.Substring(htp.data.Length - 5) == ".jpeg")
                                    {
                                        byte[] b = System.IO.File.ReadAllBytes((folder != "/" ? folder : "") + "/" + htp.data);

                                        HTTP_Packet htp_ = new HTTP_Packet() { contentLength = b.Length, contentType = "image/jpeg", data = "" };
                                        List<byte> blist = enc.GetBytes(htp_.getPackage()).ToList();
                                        blist.AddRange(b);
                                        blist.AddRange(enc.GetBytes("\r\n"));
                                        buffer = blist.ToArray();
                                        blist = null;
                                        nws.Write(buffer, 0, buffer.Length);

                                        blist = null;
                                        buffer = null;
                                        b = null;
                                    }
                                    else if (htp.data.Substring(htp.data.Length - 4) == ".png")
                                    {
                                        byte[] b = System.IO.File.ReadAllBytes((folder != "/" ? folder : "") + "/" + htp.data);

                                        HTTP_Packet htp_ = new HTTP_Packet() { contentLength = b.Length, contentType = "image/png", data = "" };
                                        List<byte> blist = enc.GetBytes(htp_.getPackage()).ToList();
                                        blist.AddRange(b);
                                        blist.AddRange(enc.GetBytes("\r\n"));
                                        buffer = blist.ToArray();
                                        blist = null;
                                        nws.Write(buffer, 0, buffer.Length);

                                        blist = null;
                                        buffer = null;
                                        b = null;
                                    }
                                    else if (htp.data.Substring(htp.data.Length - 4) == ".css")
                                    {
                                        string s = System.IO.File.ReadAllText(folder + "/" + htp.data);
                                        HTTP_Packet htp_ = new HTTP_Packet() { data = s, contentLength = enc.GetBytes(s).Length, contentType = "text/css" };
                                        buffer = enc.GetBytes(htp_.getPackage());
                                        nws.Write(buffer, 0, buffer.Length);

                                        buffer = null;
                                        s = null;

                                        if (useCache && cache.Count < max_cache)
                                        {
                                            string name = folder + htp.data;
                                            cache.Add(name, new PreloadedFile(name, s, htp_.contentLength));
                                        }
                                    }
                                    else if (htp.data.Substring(htp.data.Length - 4) == ".js")
                                    {
                                        string s = System.IO.File.ReadAllText(folder + "/" + htp.data);
                                        HTTP_Packet htp_ = new HTTP_Packet() { data = s, contentLength = enc.GetBytes(s).Length, contentType = "text/javascript" };
                                        buffer = enc.GetBytes(htp_.getPackage());
                                        nws.Write(buffer, 0, buffer.Length);

                                        buffer = null;
                                        s = null;

                                        if (useCache && cache.Count < max_cache)
                                        {
                                            string name = folder + htp.data;
                                            cache.Add(name, new PreloadedFile(name, s, htp_.contentLength));
                                        }
                                    }
                                    else if (htp.data.Substring(htp.data.Length - 4) == ".hcs")
                                    {
                                        string result = "";

                                        try
                                        {
                                            result = Hook.resolveScriptFromFile(folder + "/" + htp.data, new SessionData(htp.additionalHEAD, htp.additionalPOST, htp.valuesHEAD, htp.valuesPOST, folder, htp.data, msg_, client, nws));
                                        }
                                        catch (Exception e)
                                        {
                                            result = Master.getErrorMsg("Exception in C# Script for '"
                                                + htp.data + "'", "<b>An Error occured while processing the output</b><br>"
                                                + e.ToString() + "<hr><p>The Package you were sending:<br><div style='font-family:\"Consolas\",monospace;font-size: 13;color:#4C4C4C;'>"
                                                + msg_.Replace("\r\n", "<br>") + "</div></p>");
                                        }

                                        HTTP_Packet htp_ = new HTTP_Packet() { data = result, contentLength = enc.GetBytes(result).Length };
                                        buffer = enc.GetBytes(htp_.getPackage());
                                        nws.Write(buffer, 0, buffer.Length);

                                        buffer = null;
                                        result = null;
                                    }
                                    else
                                    {
                                        string s = System.IO.File.ReadAllText(folder + "/" + htp.data);
                                        HTTP_Packet htp_ = new HTTP_Packet() { data = s, contentLength = enc.GetBytes(s).Length };
                                        buffer = enc.GetBytes(htp_.getPackage());
                                        nws.Write(buffer, 0, buffer.Length);

                                        buffer = null;
                                        s = null;

                                        if (useCache && cache.Count < max_cache)
                                        {
                                            string name = folder + htp.data;
                                            cache.Add(name, new PreloadedFile(name, s, htp_.contentLength));
                                        }
                                    }
                                }
                                else
                                {
                                    HTTP_Packet htp_ = new HTTP_Packet()
                                    {
                                        status = "404 File Not Found",
                                        data = Master.getErrorMsg(
                                            "Error 404: Page Not Found",
                                            "<p>The Package you were sending:<br><div style='font-family:\"Consolas\",monospace;font-size: 13;color:#4C4C4C;'>" + msg_.Replace("\r\n", "<br>") + "</div></p><hr><p>I guess you don't know what that means. You're welcome! I'm done here!</p>")
                                    };

                                    htp_.contentLength = enc.GetBytes(htp_.data).Length;
                                    buffer = enc.GetBytes(htp_.getPackage());
                                    nws.Write(buffer, 0, buffer.Length);
                                }
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

        private HTTP_Packet getFile(string URL, out bool found)
        {
            // TODO: Support HTTP 304 Not Modified https://tools.ietf.org/html/rfc7232
            throw new NotImplementedException();
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
    }

    public struct PreloadedFile
    {
        public string filename;
        public string contents;
        public int size;

        public PreloadedFile(string name, string contents, int size)
        {
            this.filename = name;
            this.contents = contents;
            this.size = size;
        }
    }
}
