using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
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
        /// Shall messages be logged?
        /// </summary>
        public static bool LogMessages
        {
            get
            {
                return !nolog;
            }
            set
            {
                nolog = !value;
            }
        }

        /// <summary>
        /// Starts the IO-Loop for handling the server and showing logs.
        /// </summary>
        public static void StartHandler(bool acceptUserInput = true)
        {
            nolog = false;
            Running = true;

            Console.WriteLine("\n  ▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█,                             \n  ▓▓▓  ▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▌\"\"▓▓▓▓▓▓▓▓▓▓▓▓▓▓██                             \n  ▓▓▓  ▐▓▓▓▓▓▓▌▀\"\"▀▀▓▓▓▀▀▀\"\"▀█▀\"\"▀▓▓▓█▀\"\"▀▀▓▓▓▀▀\"\"▀█▓▀`  ▀▀▓▓▀▀▀▓▓▀▀▀▓▓██                             \n  ▓▓▓  ▐▓▓▓▓▓▌,,&▄  ▐▓▓  ╓&   ╔&   ▓▌  Æ▄╕  ▓Γ .▄&,,█NL  N▄▓▓,,▄▓▓,,▐▓▓██                             \n  ▓▓▓  ▐▓▓▓▓▓▀      ▐▓▓  ▓▓▌  ▓▓▌  ▓        ▓▌,    `█▓▌  ▓▓▓▓▓▓▓▓▓▓▓▓▓▓██                             \n  ▓▓▓  \"\"\"\"\"▐  ╙▀▀   █▓  ▓▓▌  ▓▓▌  ▓▌  ▀▀` ,▓  \"▀▀  ▄▓▌  ▀▀▓▓  ▓▓▓  ▐▓▓██                             \n  ▓▓▓ggggggg▄█&╦╦g█gg█▓gg▓▓█gg▓▓▌g▄▓▓█▄╦╦g▄▓▓▓▌g╦╦g█▓▓▓▄ggg▓▓gg▓▓▓gg▓▓▓██                             \n  ▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█▀█▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█`\'▌ ▀▓▓▓▓▓▓▓▓▓█,\n  ▓▓  ▐▓▓▌  █▓▓  ▄▓▓▓▓▓▓▓▓▓▌  ▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓█  █▓┐ ▐▓▓▓▓▓▓▓▓██\n  ▓▓▌  ▓█    ▓█  ▓▓▀    ╙█▓▌     `▀▓▓▀    \"█▓█┘    ▀▓▓▌ ╙  ╟  █▓▓▌  ▓▀`   \"█▓▓  `  ▓  █▓▓▓  █▓▓  ▐▓▓██\n  ▓▓▓  ▓  ▓  ▐  ╔▓  ≤▀▀L  ▓▌  █▓█  ▓█  ▀▀&g▄▓  Æ▀▀  ╘▓   ▄▓█L  ▓▌  █▌  ▀▀&  ▓▓  ╔▓█▓  ▓▓▓▓▌ ╟▓▓gg▓▓▓██\n  ▓▓▓▌   ╒▓█    █▓  ╔╦╦╦╦╦▓▌  ▓▓▓  ╟█▌⌂╥⌐, `▓  ╦╦╦╦╦╦▓  ▐▓▓▓▓  ╘  ▓▓   ╦╦╦╦╦▓▓  ▓▓▓▓  ▓▓▓▓▌ ▐▓▓▓▓▓▓▓██\n  ▓▓▓▓   █▓▓▌  ,▓▓▌  ``  ▄▓▌   `  ┌█▌  ``  ╔▓▌  ``  ▓▓  ▐▓▓▓▓▓   Æ▓▓█,  `  ╔▓▓  ▓▓▓▓, ▀▓▓█  ▓▓▓  ╟▓▓██\n  ▓▓▓▓███▓▓▓▓███▓▓▓▓█▓▓█▓▓▓████▓▓█▓▓▓▓█▓▓█▓▓▓▓▓█▓▓█▓▓▓███▓▓▓▓▓███▓▓▓▓▓▓▓▓█▓▓▓▓██▓▓▓▓▓┐ ╙▀ ,█▓▓▓▀ ▄▓▓██\n  ▓██████████████████████████████████████████████████████████████████████████████████████▄█████▌██████\n    ▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀\n");
            Console.WriteLine("LamestWebserver Version " + typeof(ServerHandler).Assembly.GetName().Version + "\n");
            
            Thread outp = new Thread(ShowMsgs);

            explicitLogging = true;
            outp.Start();
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
                            using (outputMutex.Lock())
                            {
                                using (WebServer.RunningServerMutex.Lock())
                                {
                                    for (int i = 0; i < WebServer.RunningServers.Count; i++)
                                    {
                                        Console.WriteLine("Port: " + WebServer.RunningServers[i].Port + " Worker Threads: " + WebServer.RunningServers[i].WorkerThreads.Instance.WorkerCount);
                                    }
                                }
                                Console.WriteLine("Done!");
                            }
                            break;
                        }

                        case "kill":
                        {
                            using (outputMutex.Lock())
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
                            }
                            break;
                        }

                        case "new":
                        {
                            using (outputMutex.Lock())
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
                            }
                            break;
                        }

                        case "tstep":
                        {
                            using (outputMutex.Lock())
                            {
                                try
                                {
                                    Console.WriteLine("New TimeStep in ms:");
                                    string prt = Console.ReadLine();
                                    cmdsleep = int.Parse(prt);
                                    Console.WriteLine("Done!");
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine("Failed!" + e);
                                }
                            }
                            break;
                        }


                        case "silent":
                        {
                            using (outputMutex.Lock())
                            {
                                try
                                {
                                    silent = true;
                                    Console.WriteLine("silence is now " + (silent ? "OFF" : "ON"));
                                    try
                                    {
                                        if (outp.ThreadState == System.Threading.ThreadState.Running)
                                            outp.Abort();
                                    }
                                    catch (Exception)
                                    {
                                    }

                                    Console.WriteLine("Done!");
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine("Failed!" + e);
                                }
                            }
                            break;
                        }

                        case "unsilent":
                        {
                            using (outputMutex.Lock())
                            {
                                try
                                {
                                    silent = false;
                                    Console.WriteLine("silence is now " + (silent ? "OFF" : "ON"));

                                    try
                                    {
                                        if (outp.ThreadState == System.Threading.ThreadState.Aborted || outp.ThreadState == System.Threading.ThreadState.Unstarted)
                                        {
                                            outp = new System.Threading.Thread(ShowMsgs);
                                            outp.Start();
                                        }
                                    }
                                    catch (Exception)
                                    {
                                        Console.WriteLine("Starting the Thread failed...");
                                    }

                                    Console.WriteLine("Done!");
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine("Failed!" + e);
                                }
                            }
                            break;
                        }

                        case "cls":
                        {
                            using (outputMutex.Lock())
                            {
                                try
                                {
                                    output = new List<Output>();
                                    Console.WriteLine("Done!");
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine("Failed!" + e);
                                }
                            }
                            break;
                        }


                        case "autocls":
                        {
                            using (outputMutex.Lock())
                            {
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
                                catch (Exception e)
                                {
                                    Console.WriteLine("Failed!" + e);
                                }
                            }
                            break;
                        }
                            
                        case "explicitlog":
                        {
                            using (outputMutex.Lock())
                            {
                                try
                                {
                                    explicitLogging = true;
                                    Console.WriteLine("Success! Logging now explicitly!");
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine("Failed!" + e);
                                }
                            }
                            break;
                        }

                        case "litelog":
                        {
                            using (outputMutex.Lock())
                            {
                                try
                                {
                                    explicitLogging = false;
                                    Console.WriteLine("Success! Logging now not explicitly!");
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine("Failed!" + e);
                                }
                            }
                            break;
                        }

                        case "log":
                        {
                            using (outputMutex.Lock())
                            {
                                nolog = false;
                                Console.WriteLine("Logging is now turned on.");
                            }
                            break;
                        }

                        case "nolog":
                        {
                            using (outputMutex.Lock())
                            {
                                nolog = true;
                                Console.WriteLine("Logging is now turned off.");
                            }
                            break;
                        }

                        case "help":
                        {
                            using (outputMutex.Lock())
                            {
                                try
                                {
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
                                    Console.WriteLine("help         -    Displays this list of cmds");
                                    Console.WriteLine("exit         -    Exit the ServerHandler");
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine("Failed!" + e);
                                }
                            }
                            break;
                        }

                        default:
                        {
                            if (Running)
                            {
                                using (outputMutex.Lock())
                                {
                                    Console.WriteLine("Invalid command '" + input + "'! If you need help, type 'help'.");
                                }
                            }
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

        private static List<Output> output = new List<Output>();
        private static int cmdsleep = 300;
        private static int autocls = 1000, autocls_s = 250;
        private static bool explicitLogging = false;
        private static bool nolog = true;
        private static UsableMutexSlim outputMutex = new UsableMutexSlim();
        private static bool silent = false;

        class Output
        {
            internal string msg;
            internal string caller;

            internal Output(string msg, StackTrace stackTrace, string endpoint)
            {
                this.msg = $"[{DateTime.Now}] " + (endpoint == null ? "" : ("@" + endpoint + " ")) + msg;

                if (stackTrace != null && stackTrace.FrameCount > 4 && stackTrace.GetFrame(4).GetMethod().DeclaringType.Namespace.Contains("LamestWebserver"))
                    for (int i = 4; i < stackTrace.FrameCount; i++)
                    {
                        var frame = stackTrace.GetFrame(i);

                        if (frame.GetMethod() == null || frame.GetMethod().DeclaringType.Namespace.Contains("LamestWebserver"))
                        {
                            continue;
                        }

                        var trace_ = stackTrace.ToString().Split(new[] {"\r\n"}, StringSplitOptions.None);

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

        /// <summary>
        /// Logs a message to the ServerLog
        /// </summary>
        /// <param name="message">the message to log</param>
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
                try
                {
                    using (outputMutex.Lock())
                    {
                        output.Add(new Output(message, stackTrace, endpoint));

                        if (output.Count > autocls)
                        {
                            try
                            {
                                output.RemoveRange(0, autocls_s);
                            }
                            catch
                            {
                            }
                        }
                    }
                }
                catch {Console.WriteLine("Failed to process message (" + message + ").");}
            }).Start();

            if (!Running && !nolog)
            {
                using (outputMutex.Lock())
                {
                    foreach (Output outp in output)
                    {
                        Console.WriteLine(outp.msg);
                    }

                    output.Clear();
                }
            }
        }

        internal static void LogMessage(string text, Stopwatch stopwatch)
        {
            LogMessage($"[in {((double)stopwatch.ElapsedTicks/(double)TimeSpan.TicksPerMillisecond):0.000}ms] {text}");
        }

        private static void ShowMsgs()
        {
            while (Running)
            {
                using (outputMutex.Lock())
                {
                    while (output.Count > 0)
                    {
                        try
                        {
                            int c = output.Count;

                            for (int i = 0; i < c; i++)
                            {
                                output[0].Print();

                                output.RemoveAt(0);
                            }
                        }
                        catch (Exception)
                        {
                        }
                    }
                }

                Thread.Sleep(cmdsleep);
            }
        }
    }
}
