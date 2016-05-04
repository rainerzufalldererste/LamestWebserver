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
            Master.startServer(8080, "./web");
            Console.WriteLine("Server listening on Port 8080.");

            pageBuilderTest.addLamePageBuilderTest();
            new pageFillerTest();
            CardGame.register();
            XmlTest.register();
            Console.WriteLine("Demos added.\n\nType exit to quit.\n");

            string input = "";

            while (input != "exit")
            {
                input = Console.ReadLine();

                if(input == "lws")
                {
                    ServerHandler.Main(args);
                }
            }
        }
    }
}
