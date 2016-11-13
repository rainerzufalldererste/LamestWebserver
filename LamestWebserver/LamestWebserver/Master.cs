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
        /// The LWS-Logo as Base64 string for HTML-Img-Elements
        /// </summary>
        private const string lwsLogoBase64 = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAMgAAABFBAMAAAD9WLSrAAAAGXRFWHRTb2Z0d2FyZQBBZG9iZSBJbWFnZVJlYWR5ccllPAAAA3FpVFh0WE1MOmNvbS5hZG9iZS54bXAAAAAAADw/eHBhY2tldCBiZWdpbj0i77u/IiBpZD0iVzVNME1wQ2VoaUh6cmVTek5UY3prYzlkIj8+IDx4OnhtcG1ldGEgeG1sbnM6eD0iYWRvYmU6bnM6bWV0YS8iIHg6eG1wdGs9IkFkb2JlIFhNUCBDb3JlIDUuNS1jMDE0IDc5LjE1MTQ4MSwgMjAxMy8wMy8xMy0xMjowOToxNSAgICAgICAgIj4gPHJkZjpSREYgeG1sbnM6cmRmPSJodHRwOi8vd3d3LnczLm9yZy8xOTk5LzAyLzIyLXJkZi1zeW50YXgtbnMjIj4gPHJkZjpEZXNjcmlwdGlvbiByZGY6YWJvdXQ9IiIgeG1sbnM6eG1wTU09Imh0dHA6Ly9ucy5hZG9iZS5jb20veGFwLzEuMC9tbS8iIHhtbG5zOnN0UmVmPSJodHRwOi8vbnMuYWRvYmUuY29tL3hhcC8xLjAvc1R5cGUvUmVzb3VyY2VSZWYjIiB4bWxuczp4bXA9Imh0dHA6Ly9ucy5hZG9iZS5jb20veGFwLzEuMC8iIHhtcE1NOk9yaWdpbmFsRG9jdW1lbnRJRD0ieG1wLmRpZDo1Mzg5NGZhZS03ODgzLTA5NDktYTBkNi0yMGYzMzA1NGEwZWUiIHhtcE1NOkRvY3VtZW50SUQ9InhtcC5kaWQ6OTM2NjA2NTdBOUI5MTFFNkE3RTg5NzIyRTVENTMyNjIiIHhtcE1NOkluc3RhbmNlSUQ9InhtcC5paWQ6OTM2NjA2NTZBOUI5MTFFNkE3RTg5NzIyRTVENTMyNjIiIHhtcDpDcmVhdG9yVG9vbD0iQWRvYmUgUGhvdG9zaG9wIENDIChXaW5kb3dzKSI+IDx4bXBNTTpEZXJpdmVkRnJvbSBzdFJlZjppbnN0YW5jZUlEPSJ4bXAuaWlkOjZmNDkwMzhjLTM2NWQtODg0OS05NzNlLWUyMGI1ODllMGI5NiIgc3RSZWY6ZG9jdW1lbnRJRD0ieG1wLmRpZDowZWQ3MTcwYS1jZmU5LWZmNDQtOWIyOS00MWUyZWY3YWMzYzkiLz4gPC9y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";

        /// <summary>
        /// Returns a LamestWebServer-style error message
        /// </summary>
        /// <param name="title">the title of the error message</param>
        /// <param name="message">the error message</param>
        /// <returns>a complete html page as string</returns>
        public static string getErrorMsg(string title, string message)
        {
            return "<head><title>" + title 
                + "</title><style type=\"text/css\">hr{border:solid;border-width:3;color:#efefef;} p {overflow:overlay;}</style></head><body style='background-color: #f1f1f1;margin: 0;'><div style='font-family: \"Segoe UI\" ,sans-serif;width: 70%;max-width: 1200px;margin: 0em auto;font-size: 16pt;background-color: #fdfdfd;padding: 4em 8em;color: #4e4e4e;'><h1 style='font-weight: lighter;font-size: 50pt;'>"
                + title + "</h1><hr>" + message.Replace("\n","<br>") + "<img style='position: fixed;bottom: 1em;right: 1em;' src=\"" + lwsLogoBase64 + "\"/></p></body>";
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
