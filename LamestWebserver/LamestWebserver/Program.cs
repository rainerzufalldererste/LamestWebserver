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

            Console.WriteLine("Happy Server! Today is " + DateTime.Now.ToString() + " if you are wondering why you are using such a strange server...");
            Console.WriteLine();


            if (args.Length < 1)
            {
                try
                {

                    Console.WriteLine("Please specify ports in param... trying to use 8080... i will fail - or not - yes - true!  - example: \"lws 8080\"");
                    ports.Add(new LServer(8080));
                }
                catch (Exception e) { Console.WriteLine("I Hate Servers! " + e.Message); };
            }
            else if(args.Length == 1)
            {
                if (args[0] == "-clean")
                {
                    Console.WriteLine("Clean Server Started.");
                }
                else
                {
                    try
                    {
                        ports.Add(new LServer(Int32.Parse(args[0]))); //does stuff
                    }
                    catch (Exception e) { Console.WriteLine("I Hate Servers! " + e.Message); };
                }
            }
            else
            {
                for(int i = 0; i < args.Length; i++)
                {
                    try
                    {
                        ports.Add(new LServer(Int32.Parse(args[i]))); //does stuff
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

                if(s == "exit")
                {
                    Console.WriteLine("no!");
                    y++;

                    if(y > 2)
                    {
                        break;
                    }
                }

                switch(s)
                {
                    case "ports": { for (int i = 0; i < ports.Count; i++) { Console.WriteLine("Port: " + ports[i].port + " Folder: " + ports[i].folder + " Threads: " + ports[i].getThreadCount()); } };
                        Console.WriteLine("Done!");
                        break;

                    case "kill": {
                        try
                        {
                            Console.WriteLine("Port: ");
                            string id = Console.ReadLine();
                            for (int i = 0; i < ports.Count; i++)
                            {
                                if (ports[i].port == Int32.Parse(id))
                                {
                                    ports[i].killMe();
                                    ports.RemoveAt(i);
                                    Console.WriteLine("Done!");
                                    break;
                                }
                            }
                        }
                        catch (Exception e) { Console.WriteLine("Failed!"); }
                    };
                        break;

                    case "new":
                        {
                            try
                            {
                                Console.WriteLine("Port:");
                                string prt = Console.ReadLine();
                                Console.WriteLine("Folder: (\".\\web\")");
                                string fld = Console.ReadLine();
                                ports.Add(new LServer(Int32.Parse(prt)) { folder = fld });
                                Console.WriteLine("Done!");
                            }
                            catch (Exception e) { Console.WriteLine("Failed!"); }
                        };
                        break;

                    default: { Console.WriteLine("Hello " + s + "!"); }
                        break;
                }
            }
        }
    }
}
