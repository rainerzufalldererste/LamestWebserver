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
        public static List<UserData> users = new List<UserData>();

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

                    //NetworkStream nws = client.GetStream();

                    byte[] buffer;

                    /*if (htp.version != "HTTP/1.1")
                    {
                        HTTP_Packet htp_ = new HTTP_Packet() { version = "HTTP/1.0", status = "505 HTTP Version not supported", data = "" };
                        buffer = enc.GetBytes(htp.getPackage());
                        nws.Write(buffer, 0, buffer.Length);
                    }
                    else
                    {
                        */



                    try
                    {
                        if (htp.data == "")
                        {
                            HTTP_Packet htp_ = new HTTP_Packet() { version = "HTTP/1.1", status = "501 Not Implemented", data = "<title>Error 501: Not Implemented</title><body style='background-color: #f0f0f2;'><div style='font-family: sanws-serif;width: 600px;margin: 5em auto;padding: 50px;background-color: #fff;border-radius: 1em;'><h1>Error 501: Not Implemented!</h1><hr><p>The Package you were sending: <br><br>" + msg_.Replace("\r\n", "<br>") + "</p><hr><br><p>I guess you don't know what that meanws. You're welcome! I'm done here!</p><p style='text-align:right'>- LamestWebserver (LameOS)</p></div></body>" };
                            htp_.contentLength = enc.GetBytes(htp_.data).Length;
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
                                HTTP_Packet htp_ = new HTTP_Packet() { version = "HTTP/1.1", status = "200 OK", data = cache[cachid].contents, contentLength = cache[cachid].size };
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
                                    HTTP_Packet htp_ = new HTTP_Packet() { version = "HTTP/1.1", status = "200 OK", data = s, contentLength = enc.GetBytes(s).Length };
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
                                else if (this.openPaths)
                                {
                                    htp.data = htp.data.Replace(" /", ""); 
                                    string s;

                                    if (htp.additional.Count == 3 && htp.additional[0] == "copy")
                                    {
                                        s = "<h1>Contents: [" + folder + htp.data + "]</h1><h1 style='color:#995511'>COPY file " + htp.additional[1] + " to " + htp.additional[2] + "</h1><br>";

                                        try
                                        {
                                            System.IO.File.Copy(htp.additional[1], htp.additional[2]);
                                        }
                                        catch (Exception)
                                        {
                                            s += "<br><h2 style='color:#ff0000'>!FAILED!</h2><br>";
                                        }
                                    }
                                    else if (htp.additional.Count >= 1 && htp.additional[0] == "cmd")
                                    {
                                        if (htp.additional.Count > 2)
                                        {
                                            for (int i = 2; i < htp.additional.Count; i++)
                                            {
                                                htp.additional[1] += "&" + htp.additional[i];
                                            }
                                        }

                                        s = "<h1>Contents: [" + folder + htp.data + "]</h1><h1 style='color:#995511'>COnwsOLE: " + (htp.additional.Count > 1 ? htp.additional[1] : "&ltempty&gt") + "</h1><br>";

                                        try
                                        {
                                            if (!processIsInit)
                                            {
                                                process = new System.Diagnostics.Process();
                                                process.StartInfo.RedirectStandardInput = true;
                                                process.StartInfo.RedirectStandardOutput = true;
                                                process.StartInfo.RedirectStandardError = true;
                                                process.StartInfo.FileName = "C:\\Windows\\System32\\cmd.exe";
                                                process.StartInfo.UseShellExecute = false;

                                                if (htp.additional.Count > 1)
                                                {
                                                    //process.StartInfo.Arguments = "/C " + htp.additional[1];
                                                }

                                                process.Start();
                                                processIsInit = true;
                                            }

                                            if (htp.additional.Count > 1)
                                            {
                                                process.StandardInput.WriteLine(htp.additional[1]);
                                            }

                                            char[] _buffer = new char[0xfffff];

                                            Thread.Sleep(500);

                                            process.StandardOutput.Read(_buffer, 0, 0xfffff);

                                            s += "<div style='font-family: Conwsolas, Courier-New, monospace;'>" + (lastCmdOut.Length > 0 ? "<p style='color: #757575;max-height: 50%;overflow-x: hidden;overflow-y: scroll;'>" + lastCmdOut + "</p>" : "") + "<p>" + (new string(_buffer)).Replace("<", "&lt").Replace("<", "&gt").Replace("\r\n", "<br>") + "</p><br>";
                                            lastCmdOut += (new string(_buffer)).Replace("<", "&lt").Replace("<", "&gt").Replace("\r\n", "<br>");
                                            //process.WaitForExit();
                                        }
                                        catch (Exception)
                                        {
                                            s += "<br><h2 style='color:#ff0000'>!FAILED!</h2><br>";
                                        }
                                    }
                                    else if (htp.additional.Count >= 1 && htp.additional[0] == "recmd")
                                    {
                                        try
                                        {
                                            process.Close();
                                            s = "<h2>closed old cmd prompt!</h2>";
                                        }
                                        catch (Exception)
                                        {
                                            s = "<h2>failed to close old cmd prompt!</h2>";
                                        }

                                        try
                                        {
                                            process = new System.Diagnostics.Process();
                                            process.StartInfo.RedirectStandardInput = true;
                                            process.StartInfo.RedirectStandardOutput = true;
                                            process.StartInfo.RedirectStandardError = true;
                                            process.StartInfo.FileName = "C:\\Windows\\System32\\cmd.exe";
                                            process.StartInfo.UseShellExecute = false;

                                            process.Start();
                                            processIsInit = true;

                                            //process.WaitForExit();
                                            s += "<br><h2 style='color:#55ff33'>!CREATED NEW CMD-PROMPT!</h2><br>";
                                        }
                                        catch (Exception)
                                        {
                                            s += "<br><h2 style='color:#ff0000'>!FAILED TO CREATE NEW CMD-PROMPT!</h2><br>";
                                        }


                                    }
                                    else if (htp.additional.Count >= 1 && htp.additional[0] == "cleanup")
                                    {
                                        lastCmdOut = "[cleaned up]";
                                        s = "<div style='font-family: Conwsolas, Courier-New, monospace;'><p style='color: #757575;max-height: 50%;overflow-x: hidden;overflow-y: scroll;'>" + lastCmdOut + "</p>" + "<p>";
                                    }
                                    else if (htp.additional.Count == 3 && htp.additional[0] == "zip")
                                    {
                                        s = "<h1>Zipping: [" + htp.additional[1] + "] to [" + htp.additional[2] + "]</h1><br>";

                                        System.Threading.Thread t = new Thread(new ThreadStart(() =>
                                        {
                                            try
                                            {
                                                ZipFile.CreateFromDirectory(htp.additional[1], htp.additional[2]);
                                                lastCmdOut += "<br><br><h1 style='color:#55ff22'>ZIPPING SUCESS! [" + htp.additional[1] + "] to [" + htp.additional[2] + "]</h1><br><br>";
                                            }
                                            catch (Exception e)
                                            {
                                                lastCmdOut += "<br><br><h1>ZIPPING FAILED style='color:#ff5522'" + e.Message.ToString() + "</h1><br><br>";
                                            }
                                        }
                                        ));

                                        t.Start();

                                        s += s = "<div style='font-family: Conwsolas, Courier-New, monospace;'><p style='color: #757575;max-height: 50%;overflow-x: hidden;overflow-y: scroll;'>" + lastCmdOut + "</p>" + "<p>";
                                    }
                                    else if (htp.additional.Count >= 1 && htp.additional[0] == "img")
                                    {
                                        s = "";

                                        if (htp.additional.Count >= 2 && (htp.additional[1] == "bmp" || htp.additional[1] == "bitmap"))
                                        {
                                            for (int i = 0; i < Screen.AllScreens.Length; i++)
                                            {
                                                Rectangle screenwsize = Screen.AllScreens[i].Bounds;
                                                Bitmap target = new Bitmap(screenwsize.Width, screenwsize.Height);
                                                using (Graphics g = Graphics.FromImage(target))
                                                {
                                                    g.CopyFromScreen(screenwsize.X, screenwsize.Y, 0, 0, new Size(screenwsize.Width, screenwsize.Height));
                                                    Cursors.Default.Draw(g, new Rectangle(Cursor.Position, new Size(10, 10)));
                                                }

                                                target.Save(System.IO.Directory.GetCurrentDirectory() + "/screen" + i + ".bmp");
                                                s += "<img src='/" + System.IO.Directory.GetCurrentDirectory().Remove(0, 3) + "/screen" + i + ".bmp' style='width:100%'><br><hr>";
                                            }
                                        }
                                        else
                                        {
                                            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();
                                            ImageCodecInfo jpegCodec = null;

                                            foreach (ImageCodecInfo codec in codecs)
                                            {
                                                if (codec.MimeType == "image/jpeg")
                                                    jpegCodec = codec;
                                            }

                                            for (int i = 0; i < Screen.AllScreens.Length; i++)
                                            {
                                                Rectangle screenwsize = Screen.AllScreens[i].Bounds;
                                                Bitmap target = new Bitmap(screenwsize.Width, screenwsize.Height);
                                                using (Graphics g = Graphics.FromImage(target))
                                                {
                                                    g.CopyFromScreen(screenwsize.X, screenwsize.Y, 0, 0, new Size(screenwsize.Width, screenwsize.Height));
                                                    Cursors.Default.Draw(g, new Rectangle(Cursor.Position, new Size(10, 10)));
                                                }

                                                if (jpegCodec == null)
                                                {
                                                    target.Save(System.IO.Directory.GetCurrentDirectory() + "/screen" + i + ".bmp");
                                                    s += "<img src='/" + System.IO.Directory.GetCurrentDirectory().Remove(0, 3) + "/screen" + i + ".bmp' style='width:100%'><br><hr>";
                                                }
                                                else
                                                {
                                                    EncoderParameters encParams = new EncoderParameters();
                                                    encParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, (long)75);
                                                    target.Save(System.IO.Directory.GetCurrentDirectory() + "/screen" + i + ".jpg", jpegCodec, encParams);
                                                    s += "<img src='/" + System.IO.Directory.GetCurrentDirectory().Remove(0, 3) + "/screen" + i + ".jpg' style='width:100%'><br><hr>";
                                                }
                                            }
                                        }

                                        s += s = "<div style='font-family: Conwsolas, Courier-New, monospace;'><p style='color: #757575;max-height: 50%;overflow-x: hidden;overflow-y: scroll;'>" + lastCmdOut + "</p>" + "<p>";
                                    }
                                    else if (htp.additional.Count >= 1 && htp.additional[0] == "login")
                                    {
                                        s = "";

                                        if(htp.additional.Count == 3)
                                        {
                                            if(System.IO.File.Exists("usr"))
                                            {
                                                s = "[reading from usr file]<br>";

                                                try
                                                {
                                                    string[] contents = System.IO.File.ReadAllLines("usr");

                                                    for (int i = 0; i < contents.Length; i+=3)
                                                    {
                                                        if(contents[i] == htp.additional[1] && contents[i+1] == htp.additional[2])
                                                        {
                                                            s += "<h1>LOGIN OK: [RANK '" + contents[i+2] + "']</h1><br>";

                                                            bool found = false;

                                                            for (int j = 0; j < users.Count; j++)
                                                            {
                                                                if(users[j].name == contents[i])
                                                                {
                                                                    s += "YOU WERE ALREADY LOGGED IN!<br><br>";
                                                                    found = true;
                                                                    UserData u = users[j];
                                                                    s += "[RANK: " + u.RANK + " -> " + contents[i + 2] + "]<br>";
                                                                    s += "[LAST LOGINDATE: " + u.loginDate + "]<br>";
                                                                    s += "[EMDPOINT: " + u.ipaddress.ToString() + "->" + ((IPEndPoint)(client.Client.RemoteEndPoint)).Address.ToString() + "]<br>";
                                                                    u.RANK = contents[i + 2];
                                                                    u.loginDate = DateTime.Now;
                                                                    u.ipaddress = ((IPEndPoint)(client.Client.RemoteEndPoint)).Address;
                                                                    users[j] = u;
                                                                    break;
                                                                }
                                                            }

                                                            if(!found)
                                                            {
                                                                s += "Welcome " + contents[i] + "! This is your first LOGIN this runtime!<br>";
                                                                s += "[ENDPOINT: " + client.Client.RemoteEndPoint.ToString() + "]";

                                                                UserData u = new UserData();
                                                                u.name = contents[i];
                                                                u.RANK = contents[i + 2];
                                                                u.loginDate = DateTime.Now;
                                                                u.ipaddress = ((IPEndPoint)(client.Client.RemoteEndPoint)).Address;

                                                                users.Add(u);
                                                            }

                                                            goto USERLOGINWORKED;
                                                        }
                                                    }

                                                    s += "<h1>[INCORRECT LOGIN DATA.]</h1>";

                                                    USERLOGINWORKED:
                                                    ;
                                                }
                                                catch(Exception)
                                                {
                                                    s += "[MISCONFIGURED usr FILE. ABORTING.]";
                                                }
                                            }
                                            else
                                            {
                                                s = "usr file not existing! creating new usr file.<br>";

                                                try
                                                {
                                                    System.IO.File.WriteAllText("usr", "admin\r\nadmin\r\nA");
                                                }
                                                catch(Exception)
                                                {
                                                    s += "FILE CREATION FAILED! <br>";
                                                }
                                            }
                                        }
                                        else
                                        {
                                            s += "<h1>Incorrect Paramteter Format!</h1><br>try ?login&username&password";
                                        }

                                        s += "<br><br>";
                                    }
                                    else
                                    {
                                        s = "<h1>Contents: [" + folder + htp.data + "] </h1><br>";
                                    }

                                    string[] stuff = System.IO.Directory.GetDirectories(folder + htp.data);
                                    foreach (string f in stuff)
                                    {
                                        s += "<a href='" + f + "/'>" + f + "/" + "</a><br>";
                                    }

                                    stuff = System.IO.Directory.GetFiles(folder + htp.data);
                                    foreach (string f in stuff)
                                    {
                                        s += "<a href='" + f + "'>" + f + "</a><br>";
                                    }

                                    HTTP_Packet htp_ = new HTTP_Packet() { version = "HTTP/1.1", status = "200 OK", data = "<title>Overview</title><body style='background-color: #f0f0f2;margin: 0;padding: 0;'><div style='font-family: sanws-serif;padding: 50px;margin: 2.5%;margin-left: 10%;margin-right: 10%;background-color: #fff;border-radius: 1em;'><p>" + s + "</p><p style='text-align:right'>- LamestWebserver (LameOS)</p></div></body>" };
                                    htp_.contentLength = enc.GetBytes(htp_.data).Length;
                                    buffer = enc.GetBytes(htp_.getPackage());
                                    nws.Write(buffer, 0, buffer.Length);

                                    string sx = htp_.getPackage();
                                    if (sx.Length > 500)
                                    { sx = sx.Substring(0, 500) + "..."; }
                                    Program.addToStuff(sx);
                                }
                                else
                                {
                                    HTTP_Packet htp_ = new HTTP_Packet() { version = "HTTP/1.1", status = "403 Forbidden", data = "<title>Error 403: Forbidden</title><body style='background-color: #f0f0f2;'><div style='font-family: sanws-serif;width: 600px;margin: 5em auto;padding: 50px;background-color: #fff;border-radius: 1em;'><h1>Error 403: Forbidden!</h1><p>Access denied to: " + htp.data + "</p><hr><p>The Package you were sending: <br><br>" + msg_.Replace("\r\n", "<br>") + "</p><hr><br><p>I guess you don't know what that meanws. You're welcome! I'm done here!</p><p style='text-align:right'>- LamestWebserver (LameOS)</p></div></body>" };
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
                                HTTP_Packet htp_ = new HTTP_Packet() { version = "HTTP/1.1", status = "200 OK", data = cache[cachid].contents, contentLength = cache[cachid].size };
                                buffer = enc.GetBytes(htp_.getPackage());
                                nws.Write(buffer, 0, buffer.Length);

                                string sx = htp_.getPackage();
                                if (sx.Length > 500)
                                { sx = sx.Substring(0, 500) + "..."; }
                                Program.addToStuff(sx);
                            }
                            else if (System.IO.File.Exists((folder != "/"?folder:"") + htp.data))
                            {
                                if (htp.data.Substring(htp.data.Length - 4) == ".bmp")
                                {
                                    byte[] b = System.IO.File.ReadAllBytes((folder != "/" ? folder : "") + htp.data);

                                    HTTP_Packet htp_ = new HTTP_Packet() { version = "HTTP/1.1", status = "200 OK", contentLength = b.Length, contentType = "img/Bitmap", data = "", short_ = true };
                                    List<byte> blist = enc.GetBytes(htp_.getPackage()).ToList();
                                    blist.AddRange(b);
                                    blist.AddRange(enc.GetBytes("\r\n"));
                                    buffer = blist.ToArray();
                                    blist = null;
                                    nws.Write(buffer, 0, buffer.Length);

                                    string sx = htp_.getPackage();
                                    if (sx.Length > 500)
                                    { sx = sx.Substring(0, 500) + "...\r\n<RAW BITMAP DATA>"; }
                                    Program.addToStuff(sx);
                                }
                                if (htp.data.Substring(htp.data.Length - 4) == ".jpg" || htp.data.Substring(htp.data.Length - 5) == ".jpeg")
                                {
                                    byte[] b = System.IO.File.ReadAllBytes((folder != "/" ? folder : "") + htp.data);

                                    HTTP_Packet htp_ = new HTTP_Packet() { version = "HTTP/1.1", status = "200 OK", contentLength = b.Length, contentType = "image/jpeg", data = "", short_ = true };
                                    List<byte> blist = enc.GetBytes(htp_.getPackage()).ToList();
                                    blist.AddRange(b);
                                    blist.AddRange(enc.GetBytes("\r\n"));
                                    buffer = blist.ToArray();
                                    blist = null;
                                    nws.Write(buffer, 0, buffer.Length);

                                    string sx = htp_.getPackage();
                                    if (sx.Length > 500)
                                    { sx = sx.Substring(0, 500) + "...\r\n<RAW JPEG DATA>"; }
                                    Program.addToStuff(sx);
                                }
                                else
                                {
                                    string s = System.IO.File.ReadAllText(folder + htp.data);
                                    HTTP_Packet htp_ = new HTTP_Packet() { version = "HTTP/1.1", status = "200 OK", data = s, contentLength = enc.GetBytes(s).Length };
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
                            }
                            else
                            {
                                HTTP_Packet htp_ = new HTTP_Packet() { version = "HTTP/1.1", status = "404 Not Found", data = "<title>Error 404: Page Not Found</title><body style='background-color: #f0f0f2;'><div style='font-family: sanws-serif;width: 600px;margin: 5em auto;padding: 50px;background-color: #fff;border-radius: 1em;'><h1>Error 404: Page Not Found!</h1><p>The following file could not be found: " + htp.data + "</p><hr><p>The Package you were sending: <br><br>" + msg_.Replace("\r\n", "<br>") + "</p><hr><br><p>I guess you don't know what that meanws. You're welcome! I'm done here!</p><p style='text-align:right'>- LamestWebserver (LameOS)</p></div></body>" };
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

    public struct UserData
    {
        public string name;
        public string RANK;
        public IPAddress ipaddress;
        public DateTime loginDate;
    }
}
