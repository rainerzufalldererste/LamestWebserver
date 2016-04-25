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
    public class LServer
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
        public System.IO.TextWriter writer;
        public System.IO.TextReader reader;
        public string lastCmdOut = "";
        public List<UserData> users = new List<UserData>();
        private AssocByFileUserData globalData;

        private List<string> hashes = new List<string>();
        private List<Master.getContents> functions = new List<Master.getContents>();
        private bool csharp_bridge = true;

        public LServer(int port, string folder)
        {
            this.csharp_bridge = true;

            Master.addFunctionEvent += addFunction;

            this.port = port;
            globalData = new AssocByFileUserData(port.ToString());
            this.tcpList = new TcpListener(IPAddress.Any, port);
            mThread = new Thread(new ThreadStart(ListenAndStuff));
            mThread.Start();
        }

        internal LServer(int port, bool cs_bridge)
        {
            this.csharp_bridge = cs_bridge;

            if(cs_bridge)
                Master.addFunctionEvent += addFunction;

            this.port = port;
            globalData = new AssocByFileUserData(port.ToString());
            this.tcpList = new TcpListener(IPAddress.Any, port);
            mThread = new Thread(new ThreadStart(ListenAndStuff));
            mThread.Start();

#if DEBUG
            if (csharp_bridge)
            {
                new AdminTools.pageFillerTest().register();
                AdminTools.pageBuilderTest.addLamePageBuilderTest();
            }
#endif
        }

        ~LServer()
        {
            if(csharp_bridge)
                Master.addFunctionEvent -= addFunction;
        }

        public int cacheHas(string name)
        {
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

        public int getThreadCount() { int num = 1; for (int i = 0; i < threads.Count; i++) { if (threads[i].IsAlive) { num++; } } return num; }

        public void cleanThreads()
        {
            if (!clearing)
            {
                clearing = true;

                int i = 0;

                while (i < threads.Count)
                {
                    if (threads[i].ThreadState == ThreadState.Running ||
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

                clearing = false;
            }
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
                    TcpClient tcpClient = this.tcpList.AcceptTcpClient();
                    threads.Add(new Thread(new ParameterizedThreadStart(DoStuff)));
                    threads[threads.Count - 1].Start((object)tcpClient);
                    Program.addToStuff("Client Connected!...");
                    
                    if(threads.Count % 100 == 0)
                    {
                        threads.Add(new Thread(new ThreadStart(cleanThreads)));
                        threads[threads.Count - 1].Start();
                    }
                }
                catch (Exception e) { Console.WriteLine("Something failed... Yes. " + e.Message); };
            }
        }

        private void DoStuff(object obj)
        {
            TcpClient client = (TcpClient)obj;
            NetworkStream nws = client.GetStream();
            UTF8Encoding enc = new UTF8Encoding();

            byte[] msg = new byte[4096];
            int bytes;

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
                    continue;
                }


                try
                {
                    string msg_ = enc.GetString(msg, 0, bytes);

                    Program.addToStuff(msg_);

                    HTTP_Packet htp = new HTTP_Packet(msg_);

                    //NetworkStream nws = client.GetStream();

                    byte[] buffer;

                    try
                    {
                        if (htp.data == "")
                        {
                            HTTP_Packet htp_ = new HTTP_Packet()
                            {
                                data = "<title>Error 501: Not Implemented</title><body style='background-color: #f0f0f2;'><div style='font-family: sans-serif;width: 600px;margin: 5em auto;padding: 50px;background-color: #fff;border-radius: 1em;'><h1>Error 501: Not Implemented!</h1><hr><p>The Package you were sending: <br><br>" + msg_.Replace("\r\n", "<br>") + "</p><hr><br><p>I guess you don't know what that means. You're welcome! I'm done here!</p><p style='text-align:right'>- LamestWebserver (LameOS)</p></div></body>"
                            };

                            htp_.contentLength = enc.GetByteCount(htp_.data);
                            buffer = enc.GetBytes(htp_.getPackage());
                            nws.Write(buffer, 0, buffer.Length);

                            string sx = htp_.getPackage();
                            if (sx.Length > 500)
                            { sx = sx.Substring(0, 500) + "..."; }
                            Program.addToStuff(sx);
                        }
                        else
                        {
                            int hashNUM = 0;
                            bool found = false;

                            for (; hashNUM < hashes.Count; hashNUM++)
                            {
                                if (hashes[hashNUM] == htp.data || hashes[hashNUM] == htp.data.Substring(1))
                                {
                                    found = true;
                                    break;
                                }
                            }

                            if (found)
                            {
                                HTTP_Packet htp_ = new HTTP_Packet()
                                {
                                    data = functions[hashNUM](new SessionData(htp.additionalHEAD, htp.additionalPOST, htp.valuesHEAD, htp.valuesPOST, folder))
                                };


                                htp_.contentLength = enc.GetByteCount(htp_.data);
                                buffer = enc.GetBytes(htp_.getPackage());
                                nws.Write(buffer, 0, buffer.Length);

                                string sx = htp_.getPackage();
                                if (sx.Length > 500)
                                { sx = sx.Substring(0, 500) + "..."; }
                                Program.addToStuff(sx);
                            }
                            else if (htp.data[htp.data.Length - 1] == '\\' || htp.data[htp.data.Length - 1] == '/')
                            {
                                int cachid = this.cacheHas(folder + htp.data + "index.html");

                                if (cachid > -1)
                                {
                                    HTTP_Packet htp_ = new HTTP_Packet() { data = cache[cachid].contents, contentLength = cache[cachid].size };
                                    buffer = enc.GetBytes(htp_.getPackage());
                                    nws.Write(buffer, 0, buffer.Length);

                                    string sx = htp_.getPackage();
                                    if (sx.Length > 500)
                                    { sx = sx.Substring(0, 500) + "..."; }
                                    Program.addToStuff(sx);
                                }
                                else
                                {
                                    if (System.IO.File.Exists(folder + htp.data + "index.html"))
                                    {
                                        string s = System.IO.File.ReadAllText(folder + htp.data + "index.html");
                                        HTTP_Packet htp_ = new HTTP_Packet() { data = s, contentLength = enc.GetBytes(s).Length };
                                        buffer = enc.GetBytes(htp_.getPackage());
                                        nws.Write(buffer, 0, buffer.Length);

                                        if (cache.Count < max_cache)
                                        {
                                            cache.Add(new PreloadedFile(folder + htp.data + "index.html", s, htp_.contentLength));
                                        }

                                        string sx = htp_.getPackage();
                                        if (sx.Length > 500)
                                        { sx = sx.Substring(0, 500) + "..."; }
                                        Program.addToStuff(sx);
                                    }
                                    else
                                    {
                                        HTTP_Packet htp_ = new HTTP_Packet()
                                        {
                                            data = "<title>Error 403: Forbidden</title><body style='background-color: #f0f0f2;'><div style='font-family: sans-serif;width: 600px;margin: 5em auto;padding: 50px;background-color: #fff;border-radius: 1em;'><h1>Error 403: Forbidden!</h1><p>Access denied to: " + htp.data + "</p><hr><p>The Package you were sending: <br><br>" + msg_.Replace("\r\n", "<br>") + "</p><hr><br><p>I guess you don't know what that means. You're welcome! I'm done here!</p><p style='text-align:right'>- LamestWebserver (LameOS)</p></div></body>"
                                        };

                                        htp_.contentLength = enc.GetBytes(htp_.data).Length;
                                        buffer = enc.GetBytes(htp_.getPackage());
                                        nws.Write(buffer, 0, buffer.Length);

                                        string sx = htp_.getPackage();
                                        if (sx.Length > 500)
                                        { sx = sx.Substring(0, 500) + "..."; }
                                        Program.addToStuff(sx);
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

                                    string sx = htp_.getPackage();
                                    if (sx.Length > 500)
                                    { sx = sx.Substring(0, 500) + "..."; }
                                    Program.addToStuff(sx);
                                }
                                else if (System.IO.File.Exists((folder != "/" ? folder : "") + htp.data))
                                {
                                    if (htp.data.Substring(htp.data.Length - 4) == ".bmp")
                                    {
                                        byte[] b = System.IO.File.ReadAllBytes((folder != "/" ? folder : "") + htp.data);

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

                                        string sx = htp_.getPackage();
                                        if (sx.Length > 500)
                                        { sx = sx.Substring(0, 500) + "...\r\n<RAW BITMAP DATA>"; }
                                        Program.addToStuff(sx);
                                    }
                                    else if (htp.data.Substring(htp.data.Length - 4) == ".jpg" || htp.data.Substring(htp.data.Length - 5) == ".jpeg")
                                    {
                                        byte[] b = System.IO.File.ReadAllBytes((folder != "/" ? folder : "") + htp.data);

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

                                        string sx = htp_.getPackage();
                                        if (sx.Length > 500)
                                        { sx = sx.Substring(0, 500) + "...\r\n<RAW JPEG DATA>"; }
                                        Program.addToStuff(sx);
                                    }
                                    else if (htp.data.Substring(htp.data.Length - 4) == ".hcs")
                                    {
                                        string s = Hook.resolveScriptFromFile(folder + htp.data, client, htp, htp.data, getCurrentUser(client), globalData);
                                        HTTP_Packet htp_ = new HTTP_Packet() { data = s, contentLength = enc.GetBytes(s).Length };
                                        buffer = enc.GetBytes(htp_.getPackage());
                                        nws.Write(buffer, 0, buffer.Length);

                                        buffer = null;
                                        s = null;

                                        if (cache.Count < max_cache)
                                        {
                                            cache.Add(new PreloadedFile(folder + htp.data + "index.html", s, htp_.contentLength));
                                        }

                                        string sx = htp_.getPackage();
                                        if (sx.Length > 500)
                                        { sx = sx.Substring(0, 500) + "..."; }
                                        Program.addToStuff(sx);
                                    }
                                    else
                                    {
                                        string s = System.IO.File.ReadAllText(folder + htp.data);
                                        HTTP_Packet htp_ = new HTTP_Packet() { data = s, contentLength = enc.GetBytes(s).Length };
                                        buffer = enc.GetBytes(htp_.getPackage());
                                        nws.Write(buffer, 0, buffer.Length);

                                        buffer = null;
                                        s = null;

                                        if (cache.Count < max_cache)
                                        {
                                            cache.Add(new PreloadedFile(folder + htp.data + "index.html", s, htp_.contentLength));
                                        }

                                        string sx = htp_.getPackage();
                                        if (sx.Length > 500)
                                        { sx = sx.Substring(0, 500) + "..."; }
                                        Program.addToStuff(sx);
                                    }
                                }
                                else
                                {
                                    HTTP_Packet htp_ = new HTTP_Packet()
                                    {
                                        data = "<title>Error 404: Page Not Found</title><body style='background-color: #f0f0f2;'><div style='font-family: sans-serif;width: 600px;margin: 5em auto;padding: 50px;background-color: #fff;border-radius: 1em;'><h1>Error 404: Page Not Found!</h1><p>The following file could not be found: " + htp.data + "</p><hr><p>The Package you were sending: <br><br>" + msg_.Replace("\r\n", "<br>") + "</p><hr><br><p>I guess you don't know what that means. You're welcome! I'm done here!</p><p style='text-align:right'>- LamestWebserver (LameOS)</p></div></body>"
                                    };

                                    htp_.contentLength = enc.GetBytes(htp_.data).Length;
                                    buffer = enc.GetBytes(htp_.getPackage());
                                    nws.Write(buffer, 0, buffer.Length);

                                    string sx = htp_.getPackage();
                                    if (sx.Length > 500)
                                    { sx = sx.Substring(0, 500) + "..."; }
                                    Program.addToStuff(sx);
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Program.addToStuff("I HATE CLIENTS AND STUFF!!! " + e.Message);
                    }
                }
                catch (Exception e)
                {
                    Program.addToStuff("nope. " + e.Message);
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
