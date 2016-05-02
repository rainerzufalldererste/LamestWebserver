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
using LameNetHook;

namespace LamestWebserver
{
    public class WebServer
    {
        TcpListener tcpList;
        List<Thread> threads = new List<Thread>();
        Thread mThread;
        public int port;
        public string folder = "./web";
        public bool running = true;
        public bool clearing = false;
        public List<PreloadedFile> cache = new List<PreloadedFile>();
        public int max_cache = 500;
        public bool openPaths = true;
        public System.Diagnostics.Process process = null;
        public bool processIsInit = false;
        public string lastCmdOut = "";
        public List<UserData> users = new List<UserData>();
        private AssocByFileUserData globalData;

        private List<string> hashes = new List<string>();
        private List<Master.getContents> functions = new List<Master.getContents>();
        private bool csharp_bridge = true;
        internal bool useCache = false;

        public WebServer(int port, string folder)
        {
            this.csharp_bridge = true;

            Master.addFunctionEvent += addFunction;
            Master.removeFunctionEvent += removeFunction;

            this.port = port;
            globalData = new AssocByFileUserData(port.ToString());
            tcpList = new TcpListener(IPAddress.Any, port);
            mThread = new Thread(new ThreadStart(ListenAndStuff));
            mThread.Start();
        }

        internal WebServer(int port, string folder, bool cs_bridge)
        {
            this.csharp_bridge = cs_bridge;

            if (cs_bridge)
            {
                Master.addFunctionEvent += addFunction;
                Master.removeFunctionEvent += removeFunction;
            }

            this.port = port;
            globalData = new AssocByFileUserData(port.ToString());
            this.tcpList = new TcpListener(IPAddress.Any, port);
            mThread = new Thread(new ThreadStart(ListenAndStuff));
            mThread.Start();
        }

        ~WebServer()
        {
            if (csharp_bridge)
            {
                Master.addFunctionEvent -= addFunction;
                Master.removeFunctionEvent -= removeFunction;
            }
        }

        public int cacheHas(string name)
        {
            if (!useCache)
                return -1;
            
            for (int i = 0; i < cache.Count; i++)
            {
                if(cache[i].filename == name)
                {
                    return i;
                }
            }
                return -1;
        }

        public void killMe()
        {
            running = false;

            try
            {
                tcpList.Stop();
            }
            catch (Exception e) { Console.WriteLine(port + ": " + e.Message); }

            try
            {
                mThread.Abort();
            }
            catch (Exception e) { Console.WriteLine(port + ": " + e.Message); }

            Console.WriteLine("Main Thread Dead! - port: " + port + " - folder: " + folder);

            int i = threads.Count;

            while (threads.Count > 0)
            {
                try
                {
                    threads[0].Abort();
                }
                catch (Exception e) { Console.WriteLine(port + ": " + e.Message); }
                threads.RemoveAt(0);

                Console.WriteLine("Thread Dead! (" + (i - threads.Count) + "/" + i + ") - port: " + port + " - folder: " + folder);
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

        Mutex cleanMutex = new Mutex();

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
                this.tcpList.Start();

            }
            catch (Exception e) { Console.WriteLine("I Hate Servers! \nTHRY OTHA PORTZ! " + e.Message); return; };


            while (running)
            {
                try
                {
                    if (tcpList.Pending())
                    {
                        TcpClient tcpClient = this.tcpList.AcceptTcpClient();
                        threads.Add(new Thread(new ParameterizedThreadStart(DoStuff)));
                        threads[threads.Count - 1].Start((object)tcpClient);
                        Program.addToStuff("Client Connected!...");

                        if (threads.Count % 25 == 0)
                        {
                            threads.Add(new Thread(new ThreadStart(cleanThreads)));
                            threads[threads.Count - 1].Start();
                        }
                    }
                    else
                    {
                        System.Threading.Thread.Sleep(1);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Something failed... Yes. " + e.Message);
                };
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
                    Program.addToStuff("An error occured! " + e.Message);
                    break;
                }

                if (bytes == 0)
                {
                    break;
                }


                try
                {
                    string msg_ = enc.GetString(msg, 0, bytes);

                    //Program.addToStuff(msg_);

                    HTTP_Packet htp = HTTP_Packet.Constructor(ref msg_, client.Client.RemoteEndPoint);

                    //NetworkStream nws = client.GetStream();

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

                            while(htp.data.Length > 1 && (htp.data[0] == ' ' || htp.data[0] == '/'))
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
                                int cachid = this.cacheHas(folder + htp.data + "index.html");

                                if (cachid > -1)
                                {
                                    HTTP_Packet htp_ = new HTTP_Packet() { data = cache[cachid].contents, contentLength = cache[cachid].size };
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
                                            cache.Add(new PreloadedFile(folder + htp.data + "index.html", s, htp_.contentLength));
                                        }
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
                                    }
                                }
                            }
                            else
                            {
                                int cachid = this.cacheHas(folder + htp.data);
                                if (cachid > -1)
                                {
                                    HTTP_Packet htp_ = new HTTP_Packet() { data = cache[cachid].contents, contentLength = cache[cachid].size };
                                    buffer = enc.GetBytes(htp_.getPackage());
                                    nws.Write(buffer, 0, buffer.Length);
                                }
                                else if (System.IO.File.Exists((folder != "/" ? folder : "") + "/" + htp.data))
                                {
                                    if (htp.data.Substring(htp.data.Length - 4) == ".bmp")
                                    {
                                        byte[] b = System.IO.File.ReadAllBytes((folder != "/" ? folder : "") + "/" + htp.data);

                                        HTTP_Packet htp_ = new HTTP_Packet() { contentLength = b.Length, contentType = "img/Bitmap", data = "", short_ = true };
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

                                        HTTP_Packet htp_ = new HTTP_Packet() { contentLength = b.Length, contentType = "image/jpeg", data = "", short_ = true };
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

                                        HTTP_Packet htp_ = new HTTP_Packet() { contentLength = b.Length, contentType = "image/png", data = "", short_ = true };
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
                                            cache.Add(new PreloadedFile(folder + htp.data, s, htp_.contentLength));
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
                                            cache.Add(new PreloadedFile(folder + htp.data, s, htp_.contentLength));
                                        }
                                    }
                                    else if (htp.data.Substring(htp.data.Length - 4) == ".hcs")
                                    {
                                        string s = Hook.resolveScriptFromFile(folder + "/" + htp.data, client, htp, htp.data, getCurrentUser(client), globalData);
                                        HTTP_Packet htp_ = new HTTP_Packet() { data = s, contentLength = enc.GetBytes(s).Length };
                                        buffer = enc.GetBytes(htp_.getPackage());
                                        nws.Write(buffer, 0, buffer.Length);

                                        buffer = null;
                                        s = null;
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
                                            cache.Add(new PreloadedFile(folder + htp.data, s, htp_.contentLength));
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
                        Program.addToStuff("I HATE CLIENTS AND STUFF!!! " + e);
                    }
                }
                catch (Exception e)
                {
                    Program.addToStuff("nope. " + e);
                }
            }
        }

        private UserData getCurrentUser(TcpClient client)
        {
            for (int i = 0; i < users.Count; i++)
            {
                if (users[i].ipaddress.Equals(((IPEndPoint)(client.Client.RemoteEndPoint)).Address))
                    return users[i];
            }

            return null;
        }

        private bool isIPLoggedIn(TcpClient client)
        {
            for (int i = 0; i < users.Count; i++)
            {
                if (users[i].ipaddress.Equals(((IPEndPoint)(client.Client.RemoteEndPoint)).Address))
                    return true;
            }

            return false;
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
