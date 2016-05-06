using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LamestWebserver
{
    public static class Master
    {
        public delegate string getContents(SessionData data);

        public delegate void addFunction(string hash, getContents function);
        public delegate void removeFunction(string hash);

        public static event addFunction addFunctionEvent;
        public static event removeFunction removeFunctionEvent;

        internal static void addFuntionToServer(string URL, getContents getc)
        {
            addFunctionEvent(URL, getc);
        }

        internal static void removeFunctionFromServer(string URL)
        {
            removeFunctionEvent(URL);
        }

        public static string getErrorMsg(string title, string message)
        {
            return "<head><title>" + title 
                + "</title><style type=\"text/css\">hr{border:solid;border-width:5;color:#FDCD48;'><p style='overflow:overlay;}</style></head><body style='background-color:#f0f0f0;background-image: url(\"/server/error.png\");background-repeat:repeat;background-size:125px;'><div style='font-family:\"Segoe UI\",sans-serif;width:70%;max-width:800px;margin:5em auto;padding:50px;background-color:#fff;border-radius: 1em;padding-top:22px;padding-bottom:22px;border:solid;border-color:#FDD248;border-width:1;'><h1>"
                + title + "</h1><hr>" + message.Replace("\n","<br>") + "<p style='text-align:right'>- LamestWebserver (LameOS)</p></div></body>";
        }

        public static HElement toHElemenet(this string s)
        {
            return new HPlainText(s);
        }

        public static HElement toHElemenet(this int i)
        {
            return new HPlainText(i.ToString());
        }

        public static void StartServer(int port, string directory, bool silent = false)
        {
            ServerHandler.RunningServers.Add(new WebServer(port, directory, true, silent));
        }

        public static void StopServers()
        {
            for (int i = ServerHandler.RunningServers.Count - 1; i > -1; i--)
            {
                try
                {
                    ServerHandler.RunningServers[i].stopServer();
                    ServerHandler.RunningServers.RemoveAt(i);
                }
                catch (Exception) { }
            }
        }

        public static void StopServer(int port)
        {
            for (int i = ServerHandler.RunningServers.Count - 1; i > -1; i--)
            {
                if (ServerHandler.RunningServers[i].port == port)
                {
                    try
                    {
                        ServerHandler.RunningServers[i].stopServer();
                        ServerHandler.RunningServers.RemoveAt(i);
                    }
                    catch (Exception) { }
                }
            }
        }
    }
}
