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

            Console.WriteLine("Happy Server! Today is " + DateTime.Now.ToString() + " if you are wondering why you are using such a strange server...");
            Console.WriteLine();


            if (args.Length < 1)
            {
                try
                {

                    Console.WriteLine("Please specify ports in param... trying to use 8080... i will fail - or not - yes - true!  - example: \"lws 8080\"");
                    LServer lserver = new LServer(8080);
                }
                catch (Exception e) { Console.WriteLine("I Hate Servers! " + e.Message); };
            }
            else
            {
                try
                {
                    LServer lserver = new LServer(Int32.Parse(args[0])); //does stuff
                }
                catch (Exception e) { Console.WriteLine("I Hate Servers! " + e.Message); };
            }

            Console.WriteLine();
            Console.WriteLine("Your Server did party all night like it was 1885 - and died.");
            Console.ReadLine();
        }
    }
}
