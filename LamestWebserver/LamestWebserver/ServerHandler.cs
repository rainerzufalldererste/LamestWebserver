using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace LamestWebserver
{
    public class ServerHandler
    {
        internal static List<WebServer> RunningServers = new List<WebServer>();

        public static void Main(string[] args)
        {
            Thread outp = new Thread(showMsgs);

            explicitLogging = true;
            outp.Start();

            Console.WriteLine("\n=== === === === LAMEST WEBSERVER === === === ===\n");
            
            if (args.Length == 1)
            {
                if (args[0] == "-clean")
                {
                    Console.WriteLine("Clean Server Started.");
                }
                else
                {
                    try
                    {
                        RunningServers.Add(new WebServer(Int32.Parse(args[0]), "./web", true));
                    }
                    catch (Exception e) { Console.WriteLine("Something went wrong.\n" + e.Message); };
                }
            }
            else if(args.Length < 1)
            {
                for (int i = 0; i < args.Length; i+=2)
                {
                    try
                    {
                        RunningServers.Add(new WebServer(Int32.Parse(args[i]), args[i + 1], true));
                    }
                    catch (Exception e) { Console.WriteLine("Something went wrong.\n" + e.Message); };
                }
            }

            Console.WriteLine("Type Help to get help. Type exit to quit.\n");

            int y = 0;

            while (true)
            {
                string s = Console.ReadLine();

                if (s == "exit")
                {
                    RunningServers.ForEach(srv => srv.StopServer());
                }
                else
                {
                    switch (s)
                    {
                        case "ports": { readme = false_; for (int i = 0; i < RunningServers.Count; i++) { Console.WriteLine("Port: " + RunningServers[i].port + " Folder: " + RunningServers[i].folder + " Threads: " + RunningServers[i].GetThreadCount()); } };
                            Console.WriteLine("Done!"); readme = true_;
                            break;

                        case "kill":
                            {
                                try
                                {
                                    readme = false_;
                                    Console.WriteLine("Port: ");
                                    string id = Console.ReadLine();
                                    for (int i = 0; i < RunningServers.Count; i++)
                                    {
                                        if (RunningServers[i].port == Int32.Parse(id))
                                        {
                                            RunningServers[i].StopServer();
                                            RunningServers.RemoveAt(i);
                                            Console.WriteLine("Done!");
                                            readme = true_;
                                            break;
                                        }
                                    }
                                }
                                catch (Exception e) { Console.WriteLine("Failed!" + e); }
                            };
                            readme = true_;
                            break;

                        case "new":
                            {
                                readme = false_;
                                try
                                {
                                    Console.WriteLine("Port:");
                                    string prt = Console.ReadLine();
                                    Console.WriteLine("Folder: (\"./web\")");
                                    string fld = Console.ReadLine();
                                    RunningServers.Add(new WebServer(Int32.Parse(prt), fld, true));
                                    Console.WriteLine("Done!");
                                }
                                catch (Exception e) { Console.WriteLine("Failed!" + e); }
                            };
                            readme = true_;
                            break;

                        case "tstep":
                            {
                                readme = false_;
                                try
                                {
                                    Console.WriteLine("New TimeStep in ms:");
                                    string prt = Console.ReadLine();
                                    cmdsleep = Int32.Parse(prt);
                                    Console.WriteLine("Done!");
                                }
                                catch (Exception e) { Console.WriteLine("Failed!" + e); }
                            };
                            readme = true_;
                            break;

                        case "silent":
                            {
                                readme = false_;
                                try
                                {
                                    true_ = false;
                                    Console.WriteLine("silence is now " + (true_?"OFF":"ON"));
                                    try
                                    {
                                        if (outp.ThreadState == System.Threading.ThreadState.Running)
                                            outp.Abort();
                                    }
                                    catch (Exception) { }

                                    Console.WriteLine("Done!");
                                }
                                catch (Exception e) { Console.WriteLine("Failed!" + e); }
                            };
                            readme = true_;
                            break;

                        case "unsilent":
                            {
                                readme = false_;
                                try
                                {
                                    true_ = true;
                                    Console.WriteLine("silence is now " + (true_ ? "OFF" : "ON"));

                                    try
                                    {
                                        if (outp.ThreadState == System.Threading.ThreadState.Aborted || outp.ThreadState == System.Threading.ThreadState.Unstarted)
                                        {
                                            outp = new System.Threading.Thread(showMsgs);
                                            outp.Start();
                                        }
                                    }
                                    catch (Exception)
                                    {
                                        Console.WriteLine("Starting the Thread failed...");
                                    }

                                    Console.WriteLine("Done!");
                                }
                                catch (Exception e) { Console.WriteLine("Failed!" + e); }
                            };
                            readme = true_;
                            break;

                        case "cls":
                            {
                                readme = false_;
                                try
                                {
                                    lock (output)
                                    {
                                        output = new List<Output>();
                                    }
                                    Console.WriteLine("Done!");
                                }
                                catch (Exception e) { Console.WriteLine("Failed!" + e); }
                            };
                            readme = true_;
                            break;


                        case "autocls":
                            {
                                readme = false_;
                                try
                                {
                                    Console.WriteLine("At which size should the log be cut off");
                                    autocls = Int32.Parse(Console.ReadLine());
                                    Console.WriteLine("the deleted size");
                                    autocls_s = Int32.Parse(Console.ReadLine());

                                    if (autocls_s > autocls)
                                    {
                                        int tmp = autocls;
                                        autocls = autocls_s;
                                        autocls_s = tmp;
                                        Console.WriteLine("you messed things up - swapping values!");
                                    }
                                    Console.WriteLine("Done!");
                                }
                                catch (Exception e) { Console.WriteLine("Failed!" + e); }
                            };
                            readme = true_;
                            break;

                        case "frefresh":
                            {
                                readme = false_;
                                try
                                {
                                    Console.WriteLine("Port: ");
                                    string id = Console.ReadLine();
                                    for (int i = 0; i < RunningServers.Count; i++)
                                    {
                                        if (RunningServers[i].port == Int32.Parse(id))
                                        {
                                            lock (RunningServers[i].cache)
                                            {
                                                RunningServers[i].cache.Clear();
                                                Console.WriteLine("Done!");
                                                break;
                                            }
                                        }
                                    }
                                }
                                catch (Exception e) { Console.WriteLine("Failed!" + e); }
                            };
                            readme = true_;
                            break;
                            
                            /*
                        case "cachesz":
                            {
                                readme = false_;
                                try
                                {
                                    Console.WriteLine("Port: ");
                                    string id = Console.ReadLine();
                                    for (int i = 0; i < RunningServers.Count; i++)
                                    {
                                        if (RunningServers[i].port == Int32.Parse(id))
                                        {
                                            Console.WriteLine("Maximum Size: ");
                                            RunningServers[i].max_cache = Int32.Parse(Console.ReadLine());
                                            Console.WriteLine("Done!");
                                            break;
                                        }
                                    }
                                }
                                catch (Exception e) { Console.WriteLine("Failed!" + e); }
                            };
                            readme = true_;
                            break;
                            */

                        case "cache":
                            {
                                readme = false_;
                                try
                                {
                                    Console.WriteLine("Port: ");
                                    string id = Console.ReadLine();
                                    for (int i = 0; i < RunningServers.Count; i++)
                                    {
                                        if (RunningServers[i].port == Int32.Parse(id))
                                        {
                                            RunningServers[i].useCache = true;
                                            Console.WriteLine("Done!");
                                            break;
                                        }
                                    }
                                }
                                catch (Exception e) { Console.WriteLine("Failed!" + e); }
                            };
                            readme = true_;
                            break;

                        case "uncache":
                            {
                                readme = false_;
                                try
                                {
                                    Console.WriteLine("Port: ");
                                    string id = Console.ReadLine();
                                    for (int i = 0; i < RunningServers.Count; i++)
                                    {
                                        if (RunningServers[i].port == Int32.Parse(id))
                                        {
                                            RunningServers[i].useCache = false;
                                            RunningServers[i].cache.Clear();
                                            Console.WriteLine("Done!");
                                            break;
                                        }
                                    }
                                }
                                catch (Exception e) { Console.WriteLine("Failed!" + e); }
                            };
                            readme = true_;
                            break;

                        case "explicitlog":
                            {
                                readme = false_;
                                try
                                {
                                    explicitLogging = true;
                                    Console.WriteLine("Success! Logging now explicitly!");
                                }
                                catch (Exception e) { Console.WriteLine("Failed!" + e); }
                            };
                            readme = true_;
                            break;

                        case "litelog":
                            {
                                readme = false_;
                                try
                                {
                                    explicitLogging = false;
                                    Console.WriteLine("Success! Logging now not explicitly!");
                                }
                                catch (Exception e) { Console.WriteLine("Failed!" + e); }
                            };
                            readme = true_;
                            break;

                        case "log":
                            nolog = false;
                            Console.WriteLine("Logging is now turned on.");
                            break;

                        case "nolog":
                            nolog = true;
                            Console.WriteLine("Logging is now turned off.");
                            break;

                        case "help":
                            {
                                readme = false_;
                                try
                                {
                                    true_ = false;
                                    Console.WriteLine("ports        -    List all running servers");
                                    Console.WriteLine("kill         -    Shut a running server off");
                                    Console.WriteLine("new          -    Create a new server on port x");
                                    Console.WriteLine("tstep        -    set log refreshing timestep");
                                    Console.WriteLine("silent       -    hides the log");
                                    Console.WriteLine("unsilent     -    shows the log");
                                    Console.WriteLine("explicitlog  -    shows the caller in the log");
                                    Console.WriteLine("litelog      -    hides the caller in the log");
                                    Console.WriteLine("log          -    enables logging");
                                    Console.WriteLine("nolog        -    disables logging");
                                    Console.WriteLine("cls          -    deletes the log");
                                    Console.WriteLine("autocls      -    deletes the log automatically at sizes");
                                    Console.WriteLine("cache        -    enables the cache of a port");
                                    Console.WriteLine("uncache      -    disables the cache of a port");
                                    Console.WriteLine("frefresh     -    refreshes the file cache of a port");
                                    //Console.WriteLine("cachesz      -    sets the maximum file cache of a port");
                                    Console.WriteLine("help         -    Displays this list of cmds");
                                    Console.WriteLine("exit         -    Exit the ServerHandler");
                                }
                                catch (Exception e) { Console.WriteLine("Failed!" + e); }
                            };
                            readme = true_;
                            break;

                        default: { Console.WriteLine("Invalid command '" + s + "'! If you need help, type 'help'."); }
                            break;
                    }
                }
            }
        }

        private static List<Output> output = new List<Output>();
        private static int cmdsleep = 300;
        private static bool readme = true;
        private static bool true_ = true, false_ = false;
        private static int autocls = 1000, autocls_s = 250;
        private static bool explicitLogging = false;
        private static bool nolog = false;

        class Output
        {
            internal string msg;
            internal string caller;

            internal Output(string msg, StackTrace stackTrace, string endpoint)
            {
                this.msg = $"[{DateTime.Now}] @{endpoint ?? "<?>"} " + msg;

                if (stackTrace != null && stackTrace.GetFrame(4).GetMethod().DeclaringType.Namespace.Contains("LamestWebserver"))
                    for (int i = 4; i < stackTrace.FrameCount; i++)
                    {
                        var frame = stackTrace.GetFrame(i);

                        if (frame.GetMethod() == null || frame.HasNativeImage() || frame.GetMethod().DeclaringType.Namespace.Contains("LamestWebserver"))
                        {
                            continue;
                        }

                        var trace_ = stackTrace.ToString().Split(new[] { "\r\n" }, StringSplitOptions.None);

                        string trace = null;
                        bool started = false;

                        for (int j = 1; j <= trace_.Length; j++)
                        {
                            if (!trace_[j].Contains("LamestWebserver"))
                            {
                                if (string.IsNullOrEmpty(trace))
                                    trace = trace_[j];
                                else
                                    trace += "\n" + trace_[j];

                                started = true;
                            }
                            else if (started)
                            {
                                break;
                            }
                        }

                        this.caller = trace;

                        break;
                    }
            }

            internal void Print()
            {
                Console.ResetColor();
                Console.WriteLine(msg);

                if (explicitLogging && this.caller != null)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine(caller);
                }
            }
        }

        public static void LogMessage(string message)
        {
            if (nolog)
                return;

            StackTrace stackTrace = null;
            string endpoint = WebServer.CurrentClientRemoteEndpoint;

            if (explicitLogging)
                stackTrace = new StackTrace();

            new Thread(() =>
            {
                lock (output)
                {
                    output.Add(new Output(message, stackTrace, endpoint));

                    if (output.Count > autocls)
                    {
                        try
                        {
                            output.RemoveRange(0, autocls_s);
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
            }).Start();
        }

        private static void showMsgs()
        {
            while (true)
            {
                if (readme)
                {
                    while (output.Count > 0)
                    {
                        try
                        {
                            int c = output.Count;

                            for (int i = 0; i < c; i++)
                            {
                                if (readme)
                                {
                                    output[0].Print();

                                    output.RemoveAt(0);
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }
                        catch (Exception) { }
                    }
                }

                System.Threading.Thread.Sleep(cmdsleep);
            }
        }
    }
}
