using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LamestWebserver;

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

            Master.StopServers();
        }
    }
}
