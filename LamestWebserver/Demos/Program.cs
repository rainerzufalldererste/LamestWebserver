using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using LamestWebserver;
using LamestWebserver.ProxyServices;

namespace Demos
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Demos for LamestWebServer\n");
            try
            {
                Master.StartServer(8080, "./web");
            }
            catch(InvalidOperationException)
            {
                Console.ReadLine();
                return;
            }

            pageBuilderTest.addLamePageBuilderTest();
            new pageFillerTest();
            CardGame.register();
            XmlTest.register();
            new jsconn();
            Console.WriteLine("Demos added.\nEnter 'exit' to quit.\n");
            
            TransparentProxy proxy = new TransparentProxy(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8080), 1234, Encoding.UTF8.GetBytes("HTTP/1.1 200 OK\r\n\r\n\r\n<html><head></head><body>THE SERVER DID NOT REPLY IN TIME.</body></html>"));

            string input = "";

            while (input != "exit")
            {
                input = Console.ReadLine();

                if (input == "lws")
                {
                    ServerHandler.Main(args);
                    break;
                }
            }

            proxy.Stop();
            Master.StopServers();
        }
    }
}
