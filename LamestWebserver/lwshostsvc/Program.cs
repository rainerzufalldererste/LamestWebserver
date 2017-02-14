using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using lwshostcore;
using LamestWebserver;

namespace lwshostsvc
{
    static class Program
    {
        private static List<Host> hosts = new List<Host>();

        /// <summary>
        /// Der Haupteinstiegspunkt für die Anwendung.
        /// </summary>
        static void Main(string[] args)
        {
#if !DEBUG
            if (args.Length == 0)
            {
                try
                {
                    ServiceBase[] ServicesToRun;
                    ServicesToRun = new ServiceBase[]
                    {
                        new HostService()
                    };
                    ServiceBase.Run(ServicesToRun);
                }
                catch (Exception)
                {
                    Console.WriteLine(0);
                }
            }
            else
            {
#endif
                try
                {
                    System.IO.Directory.Delete(Directory.GetCurrentDirectory() + "\\currentRun", true);
                }
                catch (Exception e)
                {
                    
                }

                new Thread(() => {
                    ServerHandler.Main(new string[0]);
                }).Start();
                
                foreach (var port in lwshostcore.HostConfig.CurrentHostConfig.Ports)
                {
                    try
                    {
                        LamestWebserver.Master.StartServer(port, lwshostcore.HostConfig.CurrentHostConfig.WebserverFileDirectory);
                    }
                    catch (Exception e)
                    {
                        LamestWebserver.ServerHandler.LogMessage("Failed to bind port " + port + ":\n" + e);
                    }
                }

                foreach (var hostDirectory in lwshostcore.HostConfig.CurrentHostConfig.BinaryDirectories)
                {
                    hosts.Add(new Host(hostDirectory));
                }
#if !DEBUG
            }
#endif
        }
    }
}
