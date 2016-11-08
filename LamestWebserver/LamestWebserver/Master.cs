using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LamestWebserver
{
    /// <summary>
    /// Contains Globally available and important methods for LamestWebServer
    /// </summary>
    public static class Master
    {
        /// <summary>
        /// The prototype for a response from the server
        /// </summary>
        /// <param name="data">the current SessionData</param>
        /// <returns>the response as string</returns>
        public delegate string getContents(SessionData data);

        /// <summary>
        /// the prototype for adding new pages in servers.
        /// </summary>
        /// <param name="hash">the URL</param>
        /// <param name="function">the code to execute</param>
        public delegate void addFunction(string hash, getContents function);

        /// <summary>
        /// the prototype for removing a page from the server 
        /// </summary>
        /// <param name="hash">the URL of the page</param>
        public delegate void removeFunction(string hash);

        /// <summary>
        /// the event, that raises if a page is added
        /// </summary>
        public static event addFunction addFunctionEvent;

        /// <summary>
        /// the event, that raises if a page is removed
        /// </summary>
        public static event removeFunction removeFunctionEvent;

        /// <summary>
        /// the event, that raises if a page, which is only available for one request, is added
        /// </summary>
        public static event addFunction addOneTimeFunctionEvent;
        
        /// <summary>
        /// Adds an arbitrary response to the listening servers
        /// </summary>
        /// <param name="URL">the url of the page to add</param>
        /// <param name="getc">the code of the page</param>
        public static void addFuntionToServer(string URL, getContents getc)
        {
            addFunctionEvent(URL, getc);
        }

        /// <summary>
        /// removes an arbitrary response from the listening servers
        /// </summary>
        /// <param name="URL">the URL of the page to remove</param>
        public static void removeFunctionFromServer(string URL)
        {
            removeFunctionEvent(URL);
        }

        /// <summary>
        /// Adds a function to all listening servers, which will only be available once
        /// </summary>
        /// <param name="hash">the URL at which this page will be available</param>
        /// <param name="code">the code to execute</param>
        public static void addOneTimeFuntionToServer(string hash, getContents code)
        {
            addOneTimeFunctionEvent(hash, code);
        }

        /// <summary>
        /// Returns a LamestWebServer-style error message
        /// </summary>
        /// <param name="title">the title of the error message</param>
        /// <param name="message">the error message</param>
        /// <returns>a complete html page as string</returns>
        public static string getErrorMsg(string title, string message)
        {
            return "<head><title>" + title 
                + "</title><style type=\"text/css\">hr{border:solid;border-width:5;color:#FDCD48;'><p style='overflow:overlay;}</style></head><body style='background-color:#f0f0f0;background-image: url(\"/server/error.png\");background-repeat:repeat;background-size:125px;'><div style='font-family:\"Segoe UI\",sans-serif;width:70%;max-width:800px;margin:5em auto;padding:50px;background-color:#fff;border-radius: 1em;padding-top:22px;padding-bottom:22px;border:solid;border-color:#FDD248;border-width:1;'><h1>"
                + title + "</h1><hr>" + message.Replace("\n","<br>") + "<p style='text-align:right'>- LamestWebserver (LameOS)</p></div></body>";
        }

        /// <summary>
        /// casts a string to HPlainText
        /// </summary>
        /// <param name="s">the string</param>
        /// <returns>the string as HElement</returns>
        public static HElement toHElement(this string s)
        {
            return new HPlainText(s);
        }

        /// <summary>
        /// casts a int to string to HPlainText
        /// </summary>
        /// <param name="i">the int</param>
        /// <returns>the int as string as HElement</returns>
        public static HElement toHElement(this int i)
        {
            return new HPlainText(i.ToString());
        }

        /// <summary>
        /// Starts a new Webserver listening for pages to add &amp; remove
        /// </summary>
        /// <param name="port">the port of the server</param>
        /// <param name="directory">the main web directory of the server (e.g. &quot;./web&quot;)</param>
        /// <param name="silent">shall the server print output to the console?</param>
        public static void StartServer(int port, string directory, bool silent = false)
        {
            ServerHandler.RunningServers.Add(new WebServer(port, directory, true, silent));
        }

        /// <summary>
        /// Stops all running servers
        /// </summary>
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

        /// <summary>
        /// Stops an arbitrary server
        /// </summary>
        /// <param name="port">the port of the server to stop</param>
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

        /// <summary>
        /// HTTP URL encodes a given input
        /// </summary>
        /// <param name="input">the input</param>
        /// <returns>the input encoded as HTTP URL</returns>
        public static string FormatTo_HTTP_URL(string input)
        {
            return System.Web.HttpUtility.HtmlEncode(input);
        }

        /// <summary>
        /// HTML encodes a given input
        /// </summary>
        /// <param name="text">the input</param>
        /// <returns>the input encoded as HTML</returns>
        public static string FormatTo_HTML(string text)
        {
            return new System.Web.HtmlString(text).ToHtmlString();
        }
    }
}
