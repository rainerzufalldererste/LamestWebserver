using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using LamestWebserver.Core;
using LamestWebserver.Synchronization;

namespace LamestWebserver
{
    /// <summary>
    /// Provides functionality to overview running servers and logs
    /// </summary>
    public class ServerHandler
    {
        internal static bool Running = false;

        /// <summary>
        /// Starts the IO-Loop for handling the server and showing logs.
        /// </summary>
        public static void StartHandler(bool acceptUserInput = true)
        {
            Running = true;

            Console.WriteLine("\n  ▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█,                             \n  ▓▓▓  ▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▌\"\"▓▓▓▓▓▓▓▓▓▓▓▓▓▓██                             \n  ▓▓▓  ▐▓▓▓▓▓▓▌▀\"\"▀▀▓▓▓▀▀▀\"\"▀█▀\"\"▀▓▓▓█▀\"\"▀▀▓▓▓▀▀\"\"▀█▓▀`  ▀▀▓▓▀▀▀▓▓▀▀▀▓▓██                             \n  ▓▓▓  ▐▓▓▓▓▓▌,,&▄  ▐▓▓  ╓&   ╔&   ▓▌  Æ▄╕  ▓Γ .▄&,,█NL  N▄▓▓,,▄▓▓,,▐▓▓██                             \n  ▓▓▓  ▐▓▓▓▓▓▀      ▐▓▓  ▓▓▌  ▓▓▌  ▓        ▓▌,    `█▓▌  ▓▓▓▓▓▓▓▓▓▓▓▓▓▓██                             \n  ▓▓▓  \"\"\"\"\"▐  ╙▀▀   █▓  ▓▓▌  ▓▓▌  ▓▌  ▀▀` ,▓  \"▀▀  ▄▓▌  ▀▀▓▓  ▓▓▓  ▐▓▓██                             \n  ▓▓▓ggggggg▄█&╦╦g█gg█▓gg▓▓█gg▓▓▌g▄▓▓█▄╦╦g▄▓▓▓▌g╦╦g█▓▓▓▄ggg▓▓gg▓▓▓gg▓▓▓██                             \n  ▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█▀█▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█`\'▌ ▀▓▓▓▓▓▓▓▓▓█,\n  ▓▓  ▐▓▓▌  █▓▓  ▄▓▓▓▓▓▓▓▓▓▌  ▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█  █▓┐ ▐▓▓▓▓▓▓▓▓██\n  ▓▓▌  ▓█    ▓█  ▓▓▀    ╙█▓▌     `▀▓▓▀    \"█▓█┘    ▀▓▓▌ ╙  ╟  █▓▓▌  ▓▀`   \"█▓▓  `  ▓  █▓▓▓  █▓▓  ▐▓▓██\n  ▓▓▓  ▓  ▓  ▐  ╔▓  ≤▀▀L  ▓▌  █▓█  ▓█  ▀▀&g▄▓  Æ▀▀  ╘▓   ▄▓█L  ▓▌  █▌  ▀▀&  ▓▓  ╔▓█▓  ▓▓▓▓▌ ╟▓▓gg▓▓▓██\n  ▓▓▓▌   ╒▓█    █▓  ╔╦╦╦╦╦▓▌  ▓▓▓  ╟█▌⌂╥⌐, `▓  ╦╦╦╦╦╦▓  ▐▓▓▓▓  ╘  ▓▓   ╦╦╦╦╦▓▓  ▓▓▓▓  ▓▓▓▓▌ ▐▓▓▓▓▓▓▓██\n  ▓▓▓▓   █▓▓▌  ,▓▓▌  ``  ▄▓▌   `  ┌█▌  ``  ╔▓▌  ``  ▓▓  ▐▓▓▓▓▓   Æ▓▓█,  `  ╔▓▓  ▓▓▓▓, ▀▓▓█  ▓▓▓  ╟▓▓██\n  ▓▓▓▓███▓▓▓▓███▓▓▓▓█▓▓█▓▓▓████▓▓█▓▓▓▓█▓▓█▓▓▓▓▓█▓▓█▓▓▓███▓▓▓▓▓███▓▓▓▓▓▓▓▓█▓▓▓▓██▓▓▓▓▓┐ ╙▀ ,█▓▓▓▀ ▄▓▓██\n  ▓██████████████████████████████████████████████████████████████████████████████████████▄█████▌██████\n    ▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀\n");
            Console.WriteLine("LamestWebserver Version " + typeof(ServerHandler).Assembly.GetName().Version + "\n");
            
            string input = "";

            while (Running)
            {
                if (acceptUserInput)
                {
                    input = Console.ReadLine();
                }
                else
                {
                    Thread.Sleep(1);
                    continue;
                }

                if (input == "exit")
                {
                    Running = false;
                    Master.StopServers();
                    return;
                }
                else
                {
                    switch (input)
                    {
                        case "ports":
                        {
                                using (WebServer.RunningServerMutex.Lock())
                                {
                                    for (int i = 0; i < WebServer.RunningServers.Count; i++)
                                    {
                                        Console.WriteLine("Port: " + WebServer.RunningServers[i].Port + " Worker Threads: " + WebServer.RunningServers[i].WorkerThreads.Instance.WorkerCount);
                                    }
                                }
                                Console.WriteLine("Done!");
                            break;
                        }

                        case "kill":
                        {
                                try
                                {
                                    Console.WriteLine("Port: ");
                                    string id = Console.ReadLine();
                                    Master.StopServer(int.Parse(id));
                                    Console.WriteLine("Done!");
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine("Failed!" + e);
                                }
                            break;
                        }

                        case "new":
                            {
                                try
                                {
                                    Console.WriteLine("Port:");
                                    string prt = Console.ReadLine();
                                    Console.WriteLine("Folder: (\"./web\")");
                                    string fld = Console.ReadLine();
                                    new WebServer(int.Parse(prt), fld);
                                    Console.WriteLine("Done!");
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine("Failed!" + e);
                                }
                                break;
                            }

                        case "help":
                        {
                                try
                                {
                                    Console.WriteLine("ports        -    List all running servers");
                                    Console.WriteLine("kill         -    Shut a running server off");
                                    Console.WriteLine("new          -    Create a new server on port x");
                                    Console.WriteLine("help         -    Displays this list of cmds");
                                    Console.WriteLine("exit         -    Exit the ServerHandler");
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine("Failed!" + e);
                                }
                            break;
                        }

                        default:
                        {
                                if (Running)
                                    Console.WriteLine("Invalid command '" + input + "'! If you need help, type 'help'.");
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Stops the Handler and all running Servers.
        /// </summary>
        public static void StopHandler()
        {
            Running = false;
            Master.StopServers();
        }

        /// <summary>
        /// Logs a message to the ServerLog
        /// </summary>
        /// <param name="message">the message to log</param>
        public static void LogMessage(string message) => Logger.LogInformation($"[at {WebServer.CurrentClientRemoteEndpoint}]: {message}");

        internal static void LogMessage(string text, Stopwatch stopwatch) => LogMessage($"[in {((double)stopwatch.ElapsedTicks/(double)TimeSpan.TicksPerMillisecond):0.000}ms] {text}");
    }
}
