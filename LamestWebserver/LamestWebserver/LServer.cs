using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Threading;
using System.Net.Sockets;

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

        public LServer(int port)
        {
            this.tcpList = new TcpListener(IPAddress.Any, port);
            mThread = new Thread(new ThreadStart(ListenAndStuff));
            mThread.Start();
            this.port = port;
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

                Console.WriteLine("Thread Dead! (" + threads.Count + "/" + i + ") - port: " + port + " - folder: " + folder);
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
                    if (threads[i].ThreadState == ThreadState.Running || threads[i].ThreadState == ThreadState.Unstarted)
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

            byte[] msg = new byte[4096];
            int bytes;

            while (running)
            {
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
                    Program.addToStuff("An error occured! ");
                    //break;
                }


                try
                {
                    UTF8Encoding enc = new UTF8Encoding();

                    string msg_ = enc.GetString(msg, 0, 4096);

                    int index = 0;

                    for (int i = 0; i < msg_.Length; i++ )
                    {
                        if((int)msg_[i] == '\0')
                        {
                            index= i;
                            break;
                        }
                    }

                    Program.addToStuff(msg_.Substring(0,index));

                    HTTP_Packet htp = new HTTP_Packet(msg_);

                    NetworkStream ns = client.GetStream();

                    byte[] buffer;

                    /*if (htp.version != "HTTP/1.1")
                    {
                        HTTP_Packet htp_ = new HTTP_Packet() { version = "HTTP/1.0", status = "505 HTTP Version not supported", data = "" };
                        buffer = enc.GetBytes(htp.getPackage());
                        ns.Write(buffer, 0, buffer.Length);
                    }
                    else
                    {
                        */



                    try
                    {
                        if (htp.data == "")
                        {
                            HTTP_Packet htp_ = new HTTP_Packet() { version = "HTTP/1.1", status = "501 Not Implemented", data = "<title>Error 501: Not Implemented</title><body style='background-color: #f0f0f2;'><div style='font-family: sans-serif;width: 600px;margin: 5em auto;padding: 50px;background-color: #fff;border-radius: 1em;'><h1>Error 501: Not Implemented!</h1><hr><p>The Package you were sending: <br><br>" + msg_.Replace("\r\n", "<br>") + "</p><hr><br><p>I guess you don't know what that means. You're welcome! I'm done here!</p><p style='text-align:right'>- LamestWebserver (LameOS)</p></div></body>" };
                            htp_.contentLength = enc.GetBytes(htp_.data).Length;
                            buffer = enc.GetBytes(htp_.getPackage());
                            ns.Write(buffer, 0, buffer.Length);

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
                                HTTP_Packet htp_ = new HTTP_Packet() { version = "HTTP/1.1", status = "200 OK", data = cache[cachid].contents, contentLength = cache[cachid].size };
                                buffer = enc.GetBytes(htp_.getPackage());
                                ns.Write(buffer, 0, buffer.Length);

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
                                    HTTP_Packet htp_ = new HTTP_Packet() { version = "HTTP/1.1", status = "200 OK", data = s, contentLength = enc.GetBytes(s).Length };
                                    buffer = enc.GetBytes(htp_.getPackage());
                                    ns.Write(buffer, 0, buffer.Length);

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
                                    HTTP_Packet htp_ = new HTTP_Packet() { version = "HTTP/1.1", status = "403 Forbidden", data = "<title>Error 403: Forbidden</title><body style='background-color: #f0f0f2;'><div style='font-family: sans-serif;width: 600px;margin: 5em auto;padding: 50px;background-color: #fff;border-radius: 1em;'><h1>Error 403: Forbidden!</h1><p>Access denied to: " + htp.data + "</p><hr><p>The Package you were sending: <br><br>" + msg_.Replace("\r\n", "<br>") + "</p><hr><br><p>I guess you don't know what that means. You're welcome! I'm done here!</p><p style='text-align:right'>- LamestWebserver (LameOS)</p></div></body>" };
                                    htp_.contentLength = enc.GetBytes(htp_.data).Length;
                                    buffer = enc.GetBytes(htp_.getPackage());
                                    ns.Write(buffer, 0, buffer.Length);

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
                                HTTP_Packet htp_ = new HTTP_Packet() { version = "HTTP/1.1", status = "200 OK", data = cache[cachid].contents, contentLength = cache[cachid].size };
                                buffer = enc.GetBytes(htp_.getPackage());
                                ns.Write(buffer, 0, buffer.Length);

                                string sx = htp_.getPackage();
                                if (sx.Length > 500)
                                { sx = sx.Substring(0, 500) + "..."; }
                                Program.addToStuff(sx);
                            }
                            else if (System.IO.File.Exists(folder + htp.data))
                            {
                                string s = System.IO.File.ReadAllText(folder + htp.data);
                                HTTP_Packet htp_ = new HTTP_Packet() { version = "HTTP/1.1", status = "200 OK", data = s, contentLength = enc.GetBytes(s).Length };
                                buffer = enc.GetBytes(htp_.getPackage());
                                ns.Write(buffer, 0, buffer.Length);

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
                                HTTP_Packet htp_ = new HTTP_Packet() { version = "HTTP/1.1", status = "404 Not Found", data = "<title>Error 404: Page Not Found</title><body style='background-color: #f0f0f2;'><div style='font-family: sans-serif;width: 600px;margin: 5em auto;padding: 50px;background-color: #fff;border-radius: 1em;'><h1>Error 404: Page Not Found!</h1><p>The following file could not be found: " + htp.data + "</p><hr><p>The Package you were sending: <br><br>" + msg_.Replace("\r\n", "<br>") + "</p><hr><br><p>I guess you don't know what that means. You're welcome! I'm done here!</p><p style='text-align:right'>- LamestWebserver (LameOS)</p></div></body>" };
                                htp_.contentLength = enc.GetBytes(htp_.data).Length;
                                buffer = enc.GetBytes(htp_.getPackage());
                                ns.Write(buffer, 0, buffer.Length);

                                string sx = htp_.getPackage();
                                if (sx.Length > 500)
                                { sx = sx.Substring(0, 500) + "..."; }
                                Program.addToStuff(sx);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Program.addToStuff("I HATE CLIENTS AND STUFF!!! " + e.Message);
                    }
                    //}
                }
                catch (Exception e)
                {
                    Program.addToStuff("nope. " + e.Message);
                }
            }
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
