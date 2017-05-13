﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using LamestWebserver.NotificationService;
using LamestWebserver.UI;
using System.Web;

namespace LamestWebserver
{
    /// <summary>
    /// Contains Globally available and important methods for LamestWebServer
    /// </summary>
    public static class Master
    {
        /// <summary>
        /// Automatically discovers pages in the caller Assembly
        /// </summary>
        public static void DiscoverPages()
        {
            DiscoverPages(Assembly.GetCallingAssembly());
        }
        
        /// <summary>
        /// Automatically discovers pages in the specified assembly file
        /// <param name="filename">the assembly file path to load</param>
        /// </summary>
        public static void DiscoverPagesFromFile(string filename)
        {
            DiscoverPages(Assembly.LoadFile(filename));
        }

        /// <summary>
        /// Automatically discovers pages in the specified assembly-directory
        /// <param name="path">the assembly directory path to load</param>
        /// </summary>
        public static void DiscoverPagesFromDirectory(string path)
        {
            foreach (var filename in Directory.GetFiles(path))
            {
                try
                {
                    if (filename.EndsWith(".exe") || filename.EndsWith(".dll"))
                        DiscoverPages(Assembly.LoadFile(filename));
                }
                catch { }
            }
        }

        /// <summary>
        /// Automatically discovers pages in the specified Assembly
        /// </summary>
        /// <param name="asm">the assembly to discover pages in</param>
        /// <param name="onPageFound">the code to be executed on every page found (Parameter is the Name of the Type)</param>
        public static void DiscoverPages(Assembly asm, Action<string> onPageFound = null)
        {
            foreach (var type in asm.GetTypes())
            {
                bool IgnoreDiscovery = false;

                try
                {
                    foreach (var attribute in type.GetCustomAttributes())
                        IgnoreDiscovery |= attribute is Attributes.IgnoreDiscovery;

                    if (!IgnoreDiscovery)
                    {
                        foreach (var interface_ in type.GetInterfaces())
                        {
                            try
                            {
                                if (interface_ == typeof(IURLIdentifyable))
                                {
                                    var constructor = type.GetConstructor(new Type[] { });

                                    if (constructor == null)
                                        continue;
                                    constructor.Invoke(new object[0]);
                                }
                            }
                            catch
                            {
                            }
                        }
                    }
                }
                catch
                {
                }
            }
        }

        /// <summary>
        /// The prototype for a response from the server
        /// </summary>
        /// <param name="data">the current SessionData</param>
        /// <returns>the response as string</returns>
        public delegate string GetContents(HttpSessionData data);

        /// <summary>
        /// The prototype for a response of a directory page from the server.
        /// </summary>
        /// <param name="data">the current SessionData</param>
        /// <param name="subUrl">the sub-URL of this directory request</param>
        /// <returns>the response as string</returns>
        public delegate string GetDirectoryContents(HttpSessionData data, string subUrl);

        /// <summary>
        /// the prototype for adding new pages to the servers.
        /// </summary>
        /// <param name="url">the URL</param>
        /// <param name="function">the code to execute</param>
        public delegate void AddFunction(string url, GetContents function);

        /// <summary>
        /// The prototype for adding new directory pages to the servers.
        /// </summary>
        /// <param name="url">the url of the directory</param>
        /// <param name="function">the function to add</param>
        public delegate void AddDirectoryFunction(string url, GetDirectoryContents function);

        /// <summary>
        /// the prototype for removing a page from the server 
        /// </summary>
        /// <param name="url">the URL of the page</param>
        public delegate void RemoveFunction(string url);

        /// <summary>
        /// the event, that raises if a page is added
        /// </summary>
        public static event AddFunction AddFunctionEvent;

        /// <summary>
        /// the event, that raises if a page is removed
        /// </summary>
        public static event RemoveFunction RemoveFunctionEvent;

        /// <summary>
        /// the event, that raises if a page, which is only available for one request, is added
        /// </summary>
        public static event AddFunction AddOneTimeFunctionEvent;

        /// <summary>
        /// the event, that raises if a directory page is added
        /// </summary>
        public static event AddDirectoryFunction AddDirectoryFunctionEvent = (url, function) => { };

        /// <summary>
        /// the event, thath raises if a directory page is removed
        /// </summary>
        public static event RemoveFunction RemoveDirectoryFunctionEvent = (url) => { };

        /// <summary>
        /// Decodes the characters of a HTML string.
        /// </summary>
        /// <param name="s">the string to decode</param>
        /// <returns>the decoded string</returns>
        public static string DecodeHtml(string s) => HttpUtility.HtmlDecode(s);

        /// <summary>
        /// Decodes the characters of a Url string.
        /// </summary>
        /// <param name="s">the string to decode</param>
        /// <returns>the decoded string</returns>
        public static string DecodeUrl(string s) => HttpUtility.UrlDecode(s);

        /// <summary>
        /// Adds an arbitrary response to the listening servers
        /// </summary>
        /// <param name="url">the url of the page to add</param>
        /// <param name="function">the code of the page</param>
        public static void AddFuntionToServer(string url, GetContents function)
        {
            AddFunctionEvent(url, function);
        }

        /// <summary>
        /// removes an arbitrary response from the listening servers
        /// </summary>
        /// <param name="url">the URL of the page to remove</param>
        public static void RemoveFunctionFromServer(string url)
        {
            RemoveFunctionEvent(url);
        }

        /// <summary>
        /// Adds a function to all listening servers, which will only be available once
        /// </summary>
        /// <param name="url">the URL at which this page will be available</param>
        /// <param name="function">the code to execute</param>
        public static void AddOneTimeFuntionToServer(string url, GetContents function)
        {
            AddOneTimeFunctionEvent(url, function);
        }

        /// <summary>
        /// Adds a directory function to all listening servers
        /// </summary>
        /// <param name="url">the URL at which this directory page will be available</param>
        /// <param name="function">the code to execute</param>
        public static void AddDirectoryPageToServer(string url, GetDirectoryContents function)
        {
            if (!url.EndsWith("/"))
                url += "/";

            if (url.StartsWith("/"))
                url = url.Substring(1);

            AddDirectoryFunctionEvent(url, function);
        }

        /// <summary>
        /// Removes a directory function from all listening servers
        /// </summary>
        /// <param name="url">the URL at which this directory page is available</param>
        public static void RemoveDirectoryPageFromServer(string url)
        {
            if (!url.EndsWith("/"))
                url += "/";

            if (url.StartsWith("/"))
                url = url.Substring(1);

            RemoveDirectoryFunctionEvent(url);
        }

        /// <summary>
        /// The LWS-Logo as Base64 string for HTML-Img-Elements
        /// </summary>
        private const string lwsLogoBase64 =
            "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAMgAAABFBAMAAAD9WLSrAAAAGXRFWHRTb2Z0d2FyZQBBZG9iZSBJbWFnZVJlYWR5ccllPAAAA3FpVFh0WE1MOmNvbS5hZG9iZS54bXAAAAAAADw/eHBhY2tldCBiZWdpbj0i77u/IiBpZD0iVzVNME1wQ2VoaUh6cmVTek5UY3prYzlkIj8+IDx4OnhtcG1ldGEgeG1sbnM6eD0iYWRvYmU6bnM6bWV0YS8iIHg6eG1wdGs9IkFkb2JlIFhNUCBDb3JlIDUuNS1jMDE0IDc5LjE1MTQ4MSwgMjAxMy8wMy8xMy0xMjowOToxNSAgICAgICAgIj4gPHJkZjpSREYgeG1sbnM6cmRmPSJodHRwOi8vd3d3LnczLm9yZy8xOTk5LzAyLzIyLXJkZi1zeW50YXgtbnMjIj4gPHJkZjpEZXNjcmlwdGlvbiByZGY6YWJvdXQ9IiIgeG1sbnM6eG1wTU09Imh0dHA6Ly9ucy5hZG9iZS5jb20veGFwLzEuMC9tbS8iIHhtbG5zOnN0UmVmPSJodHRwOi8vbnMuYWRvYmUuY29tL3hhcC8xLjAvc1R5cGUvUmVzb3VyY2VSZWYjIiB4bWxuczp4bXA9Imh0dHA6Ly9ucy5hZG9iZS5jb20veGFwLzEuMC8iIHhtcE1NOk9yaWdpbmFsRG9jdW1lbnRJRD0ieG1wLmRpZDo1Mzg5NGZhZS03ODgzLTA5NDktYTBkNi0yMGYzMzA1NGEwZWUiIHhtcE1NOkRvY3VtZW50SUQ9InhtcC5kaWQ6OTM2NjA2NTdBOUI5MTFFNkE3RTg5NzIyRTVENTMyNjIiIHhtcE1NOkluc3RhbmNlSUQ9InhtcC5paWQ6OTM2NjA2NTZBOUI5MTFFNkE3RTg5NzIyRTVENTMyNjIiIHhtcDpDcmVhdG9yVG9vbD0iQWRvYmUgUGhvdG9zaG9wIENDIChXaW5kb3dzKSI+IDx4bXBNTTpEZXJpdmVkRnJvbSBzdFJlZjppbnN0YW5jZUlEPSJ4bXAuaWlkOjZmNDkwMzhjLTM2NWQtODg0OS05NzNlLWUyMGI1ODllMGI5NiIgc3RSZWY6ZG9jdW1lbnRJRD0ieG1wLmRpZDowZWQ3MTcwYS1jZmU5LWZmNDQtOWIyOS00MWUyZWY3YWMzYzkiLz4gPC9y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";

        /// <summary>
        /// Returns a LamestWebServer-style error message
        /// </summary>
        /// <param name="title">the title of the error message</param>
        /// <param name="message">the error message</param>
        /// <returns>a complete html page as string</returns>
        public static string GetErrorMsg(string title, string message)
        {
            return "<head><title>" + title
                   +
                   "</title><style type=\"text/css\">hr{border:solid;border-width:3;color:#efefef;} p {overflow:overlay;}</style></head><body style='background-color: #f1f1f1;margin: 0;'><div style='font-family: \"Segoe UI\" ,sans-serif;width: 70%;max-width: 1200px;margin: 0em auto;font-size: 16pt;background-color: #fdfdfd;padding: 4em 8em;color: #4e4e4e;'><h1 style='font-weight: lighter;font-size: 50pt;'>"
                   + title + "</h1><hr>" + message.Replace("\n", "<br>").Replace("\t", "&nbsp;&nbsp;&nbsp;") + "<img style='position: fixed;bottom: 1em;right: 1em;' src=\"" + lwsLogoBase64 + "\"/></p></body>";
        }

        /// <summary>
        /// casts a string to HPlainText
        /// </summary>
        /// <param name="s">the string</param>
        /// <returns>the string as HElement</returns>
        public static HElement ToHElement(this string s)
        {
            return new HPlainText(s);
        }

        /// <summary>
        /// casts a int to string to HPlainText
        /// </summary>
        /// <param name="i">the int</param>
        /// <returns>the int as string as HElement</returns>
        public static HElement ToHElement(this int i)
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
            new WebServer(port, directory);
        }

        /// <summary>
        /// Stops all running servers
        /// </summary>
        public static void StopServers()
        {
            using (WebServer.RunningServerMutex.Lock())
            {
                for (int i = WebServer.RunningServers.Count - 1; i > -1; i--)
                {
                    try
                    {
                        WebServer.RunningServers[i].Stop();
                        WebServer.RunningServers.RemoveAt(i);
                    }
                    catch
                    {
                    }
                }
            }

            NotificationHandler.StopAllNotificationHandlers();
            ThreadedWorker.CurrentWorker.Stop();
        }

        /// <summary>
        /// Stops an arbitrary server. If it's the last one it stops everything server related (Threaded Worker, Notification Handlers)
        /// </summary>
        /// <param name="port">the port of the server to stop</param>
        public static void StopServer(int port)
        {
            using (WebServer.RunningServerMutex.Lock())
            {
                for (int i = WebServer.RunningServers.Count - 1; i > -1; i--)
                {
                    if (WebServer.RunningServers[i].Port == port)
                    {
                        try
                        {
                            WebServer.RunningServers[i].Stop();
                            WebServer.RunningServers.RemoveAt(i);
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
            }
        }

        /// <summary>
        /// HTTP URL encodes a given input
        /// </summary>
        /// <param name="input">the input</param>
        /// <returns>the input encoded as HTTP URL</returns>
        public static string FormatToHttpUrl(string input) => System.Web.HttpUtility.HtmlEncode(input);

        /// <summary>
        /// HTML encodes a given input
        /// </summary>
        /// <param name="text">the input</param>
        /// <returns>the input encoded as HTML</returns>
        public static string FormatToHtml(string text) => new System.Web.HtmlString(text).ToHtmlString();

        /// <summary>
        /// Gets the index of an Element from a List
        /// </summary>
        /// <typeparam name="T">The Type of the List-Elements</typeparam>
        /// <param name="list">The List</param>
        /// <param name="value">The Value</param>
        /// <returns>Index or null if not contained or value is null</returns>
        public static int? GetIndex<T>(this List<T> list, T value)
        {
            if (value == null)
                return null;

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null && list[i].Equals(value))
                {
                    return i;
                }
            }

            return null;
        }

        internal static char[] hexCharLut = {'0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f'};

        /// <summary>
        /// Converts a byte[] to a hex string
        /// </summary>
        /// <param name="bytes">the byte[]</param>
        /// <returns>the byte[] as hex string</returns>
        public static string ToHexString(this byte[] bytes)
        {
            char[] s = new char[bytes.Length*2];

            for (int i = 0; i < bytes.Length; i++)
            {
                s[i*2] = hexCharLut[bytes[i] & 0x0F];
                s[i*2 + 1] = hexCharLut[(bytes[i] & 0xF0) >> 4];
            }

            return new string(s);
        }

        internal static event Action<WebSocketCommunicationHandler> AddWebsocketHandlerEvent = x => { };
        internal static event Action<string> RemoveWebsocketHandlerEvent = x => { };

        /// <summary>
        /// Adds a WebSocketCommunicationHandler to all listening Servers.
        /// </summary>
        /// <param name="webSocketCommunicationHandler">the WebSocketCommunicationHandler</param>
        public static void AddWebsocketHandler(WebSocketCommunicationHandler webSocketCommunicationHandler)
        {
            AddWebsocketHandlerEvent(webSocketCommunicationHandler);
        }

        /// <summary>
        /// Removes a WebSocketCommunicationHandler from all listening Servers.
        /// </summary>
        /// <param name="URL">the URL of the WebSocketCommunicationHandler</param>
        public static void RemoveWebsocketHandler(string URL)
        {
            RemoveWebsocketHandlerEvent(URL);
        }

        [System.Security.Permissions.SecurityPermissionAttribute(System.Security.Permissions.SecurityAction.Demand, ControlThread = true)]
        internal static void ForceQuitThread(System.Threading.Thread thread)
        {
            thread.Abort();
        }
    }
}
