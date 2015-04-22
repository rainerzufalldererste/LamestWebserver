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

        public LServer(int port)
        {
            this.tcpList = new TcpListener(IPAddress.Any, port);
            Thread t = new Thread(new ThreadStart(ListenAndStuff));
            t.Start();
        }

        private void ListenAndStuff()
        {
            try
            {
                this.tcpList.Start();

            }
            catch (Exception e) { Console.WriteLine("I Hate Servers! OTHA PORTZ! " + e.Message); return; };


            while (true)
            {
                TcpClient tcpClient = this.tcpList.AcceptTcpClient();
                threads.Add(new Thread(new ParameterizedThreadStart(DoStuff)));
                threads[threads.Count - 1].Start((object)tcpClient);
                Console.WriteLine("Client Connected!...");
            }
        }


        private void DoStuff(object obj)
        {
            TcpClient client = (TcpClient)obj;
            NetworkStream nws = client.GetStream();

            byte[] msg = new byte[4096];
            int bytes;

            while(true)
            {
                try
                {
                    bytes = nws.Read(msg, 0, 4096);
                }
                catch(Exception e)
                {
                    Console.WriteLine("An error occured! " + e.Message);
                    break;
                }

                if(bytes == 0)
                {
                    Console.WriteLine("An error occured! ");
                    //break;
                }

                UTF8Encoding enc = new UTF8Encoding();

                string msg_ = enc.GetString(msg, 0, 4096);

                Console.WriteLine("\n" + msg_ + "\n");

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
                    if(System.IO.File.Exists("./web" + htp.data))
                    {
                        string s = System.IO.File.ReadAllText("./web" + htp.data);
                        HTTP_Packet htp_ = new HTTP_Packet() { version = "HTTP/1.1", status = "200 OK", data = s, contentLength = enc.GetBytes(s).Length };
                        buffer = enc.GetBytes(htp_.getPackage());
                        ns.Write(buffer, 0, buffer.Length);

                        Console.WriteLine(htp_.getPackage());
                    }
                    else
                    {
                        HTTP_Packet htp_ = new HTTP_Packet() { version = "HTTP/1.1", status = "404 Not Found", data = "", contentLength = 0 };
                        buffer = enc.GetBytes(htp_.getPackage());
                        ns.Write(buffer, 0, buffer.Length);

                        Console.WriteLine(htp_.getPackage());
                    }
                //}
            }
        }
        
    }
}
