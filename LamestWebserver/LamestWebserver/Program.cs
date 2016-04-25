using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LamestWebserver
{
    class Program
    {
        static void Main(string[] args)
        {
            List<LServer> ports = new List<LServer>();

            System.Threading.Thread outp = new System.Threading.Thread(new System.Threading.ThreadStart(Program.showMsgs));
            //outp.Start();

            Console.WriteLine("Happy Server! Today is " + DateTime.Now.ToString() + " if you are wondering why you are using such a strange server...");
            Console.WriteLine();


            if (args.Length < 1)
            {
                try
                {

                    Console.WriteLine("Please specify ports in param... trying to use 8080... i will fail - or not - yes - true!  - example: \"lws 8080\"");
                    ports.Add(new LServer(8080, true));
                }
                catch (Exception e) { Console.WriteLine("I Hate Servers! " + e.Message); };
            }
            else if (args.Length == 1)
            {
                if (args[0] == "-clean")
                {
                    Console.WriteLine("Clean Server Started.");
                }
                else
                {
                    try
                    {
                        ports.Add(new LServer(Int32.Parse(args[0]), true)); //does stuff
                    }
                    catch (Exception e) { Console.WriteLine("I Hate Servers! " + e.Message); };
                }
            }
            else
            {
                for (int i = 0; i < args.Length; i+=2)
                {
                    try
                    {
                        ports.Add(new LServer(Int32.Parse(args[i]), true) { folder = args[i+1] }); //does stuff
                    }
                    catch (Exception e) { Console.WriteLine("I Hate Servers! " + e.Message); };
                }
            }

            Console.WriteLine();
            Console.WriteLine("Your Server did party all night like it was 1885 - and died.");

            int y = 0;

            while (true)
            {
                string s = Console.ReadLine();

                if (s == "exit")
                {
                    Console.WriteLine("no!");
                    y++;

                    if (y > 2)
                    {
                        break;
                    }
                }
                else
                {

                    switch (s)
                    {
                        case "ports": { readme = false_; for (int i = 0; i < ports.Count; i++) { Console.WriteLine("Port: " + ports[i].port + " Folder: " + ports[i].folder + " Threads: " + ports[i].getThreadCount()); } };
                            Console.WriteLine("Done!"); readme = true_;
                            break;

                        case "kill":
                            {
                                try
                                {
                                    readme = false_;
                                    Console.WriteLine("Port: ");
                                    string id = Console.ReadLine();
                                    for (int i = 0; i < ports.Count; i++)
                                    {
                                        if (ports[i].port == Int32.Parse(id))
                                        {
                                            ports[i].killMe();
                                            ports.RemoveAt(i);
                                            Console.WriteLine("Done!");
                                            readme = true_;
                                            break;
                                        }
                                    }
                                }
                                catch (Exception e) { Console.WriteLine("Failed!"); }
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
                                    ports.Add(new LServer(Int32.Parse(prt), true) { folder = fld });
                                    Console.WriteLine("Done!");
                                }
                                catch (Exception e) { Console.WriteLine("Failed!"); }
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
                                catch (Exception e) { Console.WriteLine("Failed!"); }
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
                                catch (Exception e) { Console.WriteLine("Failed!"); }
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
                                            outp = new System.Threading.Thread(new System.Threading.ThreadStart(Program.showMsgs));
                                            outp.Start();
                                        }
                                    }
                                    catch (Exception)
                                    {
                                        Console.WriteLine("Starting the Thread failed...");
                                    }

                                    Console.WriteLine("Done!");
                                }
                                catch (Exception e) { Console.WriteLine("Failed!"); }
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
                                        output = new List<string>();
                                    }
                                    Console.WriteLine("Done!");
                                }
                                catch (Exception e) { Console.WriteLine("Failed!"); }
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
                                catch (Exception e) { Console.WriteLine("Failed!"); }
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
                                    for (int i = 0; i < ports.Count; i++)
                                    {
                                        if (ports[i].port == Int32.Parse(id))
                                        {
                                            lock (ports[i].cache)
                                            {
                                                ports[i].cache = new List<PreloadedFile>();
                                                Console.WriteLine("Done!");
                                                break;
                                            }
                                        }
                                    }
                                }
                                catch (Exception e) { Console.WriteLine("Failed!"); }
                            };
                            readme = true_;
                            break;

                        case "cachesz":
                            {
                                readme = false_;
                                try
                                {
                                    Console.WriteLine("Port: ");
                                    string id = Console.ReadLine();
                                    for (int i = 0; i < ports.Count; i++)
                                    {
                                        if (ports[i].port == Int32.Parse(id))
                                        {
                                            Console.WriteLine("Maximum Size: ");
                                            ports[i].max_cache = Int32.Parse(Console.ReadLine());
                                            Console.WriteLine("Done!");
                                            break;
                                        }
                                    }
                                }
                                catch (Exception e) { Console.WriteLine("Failed!"); }
                            };
                            readme = true_;
                            break;

                        case "help":
                            {
                                readme = false_;
                                try
                                {
                                    true_ = false;
                                    Console.WriteLine("ports     -    List all running servers");
                                    Console.WriteLine("kill      -    Shut a running server off");
                                    Console.WriteLine("new       -    Create a new server on port x");
                                    Console.WriteLine("tstep     -    set log refreshing timestep");
                                    Console.WriteLine("silent    -    hides the log");
                                    Console.WriteLine("unsilent  -    shows the log");
                                    Console.WriteLine("cls       -    deletes the log");
                                    Console.WriteLine("autocls   -    deletes the log automatically at sizes");
                                    Console.WriteLine("frefresh  -    refreshes the file cache of a port");
                                    Console.WriteLine("cachesz   -    sets the maximum file cache of a port");
                                    Console.WriteLine("help      -    Displays this list of cmds");
                                    Console.WriteLine("exit      -    ");

                                    Console.WriteLine();
                                    Console.WriteLine("Done!");
                                }
                                catch (Exception e) { Console.WriteLine("Failed!"); }
                            };
                            readme = true_;
                            break;

                        default: { Console.WriteLine("Hello " + s + "!"); }
                            break;
                    }
                }
            }
        }

        private static List<string> output = new List<string>();
        private static int cmdsleep = 300;
        private static bool readme = true;
        private static bool true_ = true, false_ = false;
        private static int autocls = 1000, autocls_s = 250;

        public static void addToStuff(string s)
        {
            lock (output)
            {
                output.Add(s);

                if(output.Count > autocls)
                {
                    try
                    {
                        output.RemoveRange(0, autocls_s);
                    }
                    catch (Exception) { }
                }
            }
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
                            int c = output.Count - 1;

                            for (int i = 0; i < c; i++)
                            {
                                if (readme)
                                {
                                    Console.WriteLine(output[0]);

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
