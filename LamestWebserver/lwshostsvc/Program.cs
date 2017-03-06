using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using lwshostcore;
using LamestWebserver;
using LamestWebserver.RequestHandlers;

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
            foreach (var arg in args)
            {
                string current = arg;

                if (current.StartsWith("/"))
                {
                    current = current.Remove(0, 1);
                    current = current.Insert(0, "-");
                }

                switch (current)
                {

                    case "-i":
                    case "--install":
                        Console.WriteLine("Installing the Service...");

                        try
                        {
                            LamestWebserver.Security.ElevateRightsWindows.ElevateRights();

                            var result = HostServiceInstaller.Install();

                            switch (result)
                            {
                                    case HostServiceInstaller.EHostServiceInstallState.InstallAndCommitCompleted:
                                    Console.WriteLine("The Service has been successfully installed and comitted.");
                                    return;

                                    case HostServiceInstaller.EHostServiceInstallState.InstallationFailed:
                                    Console.WriteLine("The Service could not be installed.");
                                    return;

                                    case HostServiceInstaller.EHostServiceInstallState.RollbackCompleted:
                                    Console.WriteLine("The Service could not be installed. Rollback completed.");
                                    return;

                                    case HostServiceInstaller.EHostServiceInstallState.RollbackFailed:
                                    Console.WriteLine("The Service could not be installed. Rollback failed.");
                                    return;
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Installation Failed.\n" + e);
                        }
                        return;

                    case "-u":
                    case "--uninstall":

                        try
                        {
                            LamestWebserver.Security.ElevateRightsWindows.ElevateRights();

                            var result = HostServiceInstaller.Install(true);

                            switch (result)
                            {
                                case HostServiceInstaller.EHostServiceInstallState.UninstalCompleted:
                                    Console.WriteLine("The Service has been successfully uninstalled.");
                                    return;

                                case HostServiceInstaller.EHostServiceInstallState.InstallationFailed:
                                    Console.WriteLine("The Service could not be uninstalled.");
                                    return;

                                case HostServiceInstaller.EHostServiceInstallState.RollbackCompleted:
                                    Console.WriteLine("The Service could not be uninstalled. Rollback completed.");
                                    return;

                                case HostServiceInstaller.EHostServiceInstallState.RollbackFailed:
                                    Console.WriteLine("The Service could not be uninstalled. Rollback failed.");
                                    return;
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Operation Failed.\n" + e);
                        }
                        return;

                    case "-?":
                    default:
                        Console.WriteLine("\n===========================================\n|||                                     |||\n|||     LamestWebserver Host Service    |||\n|||                                     |||\n===========================================\n");
                        Console.WriteLine("-i   (--install)      Install the Service");
                        Console.WriteLine("-u   (--uninstall)    Uninstall the Service");
                        return;
                }
            }

#if !DEBUG
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
#else
            try
            {
                Directory.Delete(Directory.GetCurrentDirectory() + "\\lwshost\\CurrentRun", true);
            }
            catch (Exception)
            {

            }

            new Thread(() => {
                ServerHandler.StartHandler();
                hosts.ForEach(h => h.Stop());
            }).Start();

            HostConfig.CurrentHostConfig.ApplyConfig();

            ResponseHandler.CurrentResponseHandler.AddSecondaryRequestHandler(new ErrorRequestHandler());
            ResponseHandler.CurrentResponseHandler.AddRequestHandler(new WebSocketRequestHandler());
            ResponseHandler.CurrentResponseHandler.AddRequestHandler(new PageResponseRequestHandler());
            ResponseHandler.CurrentResponseHandler.AddRequestHandler(new OneTimePageResponseRequestHandler());

            foreach (var directory in HostConfig.CurrentHostConfig.WebserverFileDirectories)
            {
                ResponseHandler.CurrentResponseHandler.AddRequestHandler(new CachedFileRequestHandler(directory));
                ServerHandler.LogMessage($"Added WebserverFileDirectory '{directory}'");
            }

            ResponseHandler.CurrentResponseHandler.AddRequestHandler(new DirectoryResponseRequestHandler());

            foreach (var port in HostConfig.CurrentHostConfig.Ports)
            {
                try
                {
                    new WebServer(port);
                }
                catch (Exception e)
                {
                    ServerHandler.LogMessage("Failed to bind port " + port + ":\n" + e);
                }
            }

            if (WebServer.ServerCount == 0)
            {
                ServerHandler.LogMessage("No server started. Aborting.\n\nPress Enter to Quit.");
                ServerHandler.StopHandler();
                Thread.Sleep(5000);
                return;
            }
            else
            {
                ServerHandler.LogMessage($"{WebServer.ServerCount} port-listening Server(s) started.");
            }

            // Discover the HostServiceDefaultResponse
            Master.DiscoverPages();

            bool removed = false;

            Action<string> onRegister = s =>
            {
                if (!removed)
                    LamestWebserver.Master.RemoveDirectoryPageFromServer("/");

                removed = true;
            };

            foreach (var hostDirectory in lwshostcore.HostConfig.CurrentHostConfig.BinaryDirectories)
            {
                var host = new Host(hostDirectory, onRegister);
                hosts.Add(host);
            }
#endif
        }
    }
}
