using System;
using System.Threading;
using LamestWebserver;

namespace Demos
{
    class Program
    {
        /// <summary>
        /// This is the starting point for this program
        /// </summary>
        /// <param name="args">the arguments the executable has started with</param>
        static void Main(string[] args)
        {
            try
            {
                // Create a new Webserver at port 8080 with the data location "./web" - the web folder of this Project
                // Make sure all contents of your data location folder are copied into the build directory on compile so the webserver can find them at runtime.
                //
                // You'll probably be using port 80 later, because that's the default http port - but for now let's just use port 8080.
                using (var webserver = new WebServer(8080, "./web"))
                {
                    // Automatically Discovers the Pages in this assembly and registers them at the webserver
                    // Alternatively you might want to call all constructors of the pages manually here.
                    // 
                    // Pages inheriting from IURLIdentifyable (like PageResponse, DirectoryResponse, ElementResponse, etc.) 
                    //  with an empty constructor (see e.g. MainPage.cs) are automatically added when calling Master.DiscoverPages();
                    //  if you don't want such a constructor to be called mark the containing class with the LamestWebserver.Attributes.IgnoreDiscovery Attribute.
                    Master.DiscoverPages();

                    // Open a browser window at the base-URL of our webserver.
                    System.Diagnostics.Process.Start($"http://localhost:{webserver.Port}/");

                    Console.WriteLine("LamestWebserver Demos.\n\nType 'exit' to quit.");

                    // Keep the Server available until we enter exit.
                    while (Console.ReadLine() != "exit") { }
                }
            }
            // When the tcp port (8080 in this example) is already used by another application an exception is thrown.
            catch (Exception e)
            {
                // Display the message of the exception so you can read it and wait, so you have enough time to read it.
                Console.WriteLine(e.Message);
                Thread.Sleep(5000);
            }
        }
    }
}
