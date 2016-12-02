using System;

namespace LamestWebserver
{
    /// <summary>
    /// A Page referencable by URL
    /// </summary>
    public interface IURLIdentifyable
    {
        /// <summary>
        /// The URL of this Page
        /// </summary>
        string URL { get; }
    }

    /// <summary>
    /// A abstract class for directly responding with a string to the client request
    /// </summary>
    public abstract class PageResponse : IURLIdentifyable
    {
        /// <summary>
        /// The URL of this Page
        /// </summary>
        public string URL { get; protected set; }

        /// <summary>
        /// Constructs (and also registers if you want to) a new Page Response
        /// </summary>
        /// <param name="URL">the URL of this page</param>
        /// <param name="register">shall this page automatically be registered?</param>
        protected PageResponse(string URL, bool register = true)
        {
            this.URL = URL;

            if (register)
                Master.addFuntionToServer(URL, getContents);
        }

        /// <summary>
        /// This method removes the current page from the server (as URL identifyable object)
        /// </summary>
        protected void removeFromServer()
        {
            Master.removeFunctionFromServer(URL);
        }

        /// <summary>
        /// A direct answer to the client as string
        /// </summary>
        /// <param name="sessionData">the current sessionData</param>
        /// <returns>the response</returns>
        protected abstract string getContents(SessionData sessionData);
    }

    /// <summary>
    /// A syncronized direct response as string to the client request
    /// </summary>
    public abstract class SyncronizedPageResponse : PageResponse
    {
        private UsableMutex mutex = new UsableMutex();

        /// <summary>
        /// Constructs a new SyncronizedPageResponse and registers it if specified at the given URL
        /// </summary>
        /// <param name="URL">the URL of this Page</param>
        /// <param name="register">shall this page be automatically registered?</param>
        protected SyncronizedPageResponse(string URL, bool register = true) : base(URL, false)
        {
            if (register)
                Master.addFuntionToServer(URL, getContentSyncronously);
        }

        private string getContentSyncronously(SessionData sessionData)
        {
            using (mutex.Lock())
            {
                return getContents(sessionData);
            }
        }

        /// <summary>
        /// A direct answer to the client as string
        /// </summary>
        /// <param name="sessionData">the current sessionData</param>
        /// <returns>the response</returns>
        protected abstract override string getContents(SessionData sessionData);
    }

    /// <summary>
    /// A direct response as HElement to the client request
    /// </summary>
    public abstract class ElementResponse : IURLIdentifyable
    {
        /// <summary>
        /// the specified URL of this page
        /// </summary>
        public string URL { get; protected set; }

        /// <summary>
        /// Constructs a new ElementResponse and registers it if specified at the given URL
        /// </summary>
        /// <param name="URL">the URL of this page</param>
        /// <param name="register">shall this page be automatically registered?</param>
        protected ElementResponse(string URL, bool register = true)
        {
            this.URL = URL;

            if (register)
                Master.addFuntionToServer(URL, getContents);
        }

        /// <summary>
        /// This method is used to remove the current page from the server (as URL identifyable object)
        /// </summary>
        protected void removeFromServer()
        {
            Master.removeFunctionFromServer(URL);
        }

        private string getContents(SessionData sessionData)
        {
            return getElement(sessionData) * sessionData;
        }

        /// <summary>
        /// A direct answer to the clients request as HElement
        /// </summary>
        /// <param name="sessionData">the current sessionData</param>
        /// <returns>the response</returns>
        protected abstract HElement getElement(SessionData sessionData);
    }

    /// <summary>
    /// A syncronized direct response as HElement to the clients request
    /// </summary>
    public abstract class SyncronizedElementResponse : ElementResponse
    {
        private UsableMutex mutex = new UsableMutex();

        /// <summary>
        /// Constructs a new SyncronizedElementResponse and registers it if specified at the given URL
        /// </summary>
        /// <param name="URL">the URL of this page</param>
        /// <param name="register">shall this page be automatically registered at the server?</param>
        protected SyncronizedElementResponse(string URL, bool register = true) : base(URL, false)
        {
            if (register)
                Master.addFuntionToServer(URL, getContents);
        }

        private string getContents(SessionData sessionData)
        {
            using (mutex.Lock())
            {
                return getElement(sessionData) * sessionData;
            }
        }

        /// <summary>
        /// The direct pre-syncronized response to the clients request as HElement
        /// </summary>
        /// <param name="sessionData">the current sessionData</param>
        /// <returns>the response</returns>
        protected abstract override HElement getElement(SessionData sessionData);
    }

    /// <summary>
    /// This Helper-Class is Used to quickly define new pages at the server
    /// </summary>
    public static class InstantPageResponse
    {
        /// <summary>
        /// adds a page to the server, that executes the given code
        /// </summary>
        public static void addInstantPageResponse(string URL, Master.getContents code)
        {
            Master.addFuntionToServer(URL, code);
        }

        /// <summary>
        /// adds a temporary page to the server, that executes the given code (only available for ONE request)
        /// </summary>
        /// <param name="code">the code to execute</param>
        /// <param name="instantlyRemove">runtime code should instantly remove these - constructors should not remove, since then they'll be gone the next compile</param>
        /// <returns>the name at which this temporary page will be available at.</returns>
        public static string addOneTimeInstantPageResponse(Master.getContents code, bool instantlyRemove)
        {
            string hash = SessionContainer.generateUnusedHash();

            if (instantlyRemove)
            {
                Master.addOneTimeFuntionToServer(hash, code);
            }
            else
            {
                Master.addFuntionToServer(hash, code);

            }

            return "/" + hash;
        }

        /// <summary>
        /// adds a page to the server, that redirects to "destinationURL" in X milliseconds
        /// </summary>
        public static void addTimedRedirect(string originURL, string message, int milliseconds, string destinationURL, bool copyPOST = false)
        {
            addInstantPageResponse(originURL, (SessionData sessionData) =>
            {
                return generateRedirectInMillisecondsCode(destinationURL, message, milliseconds, sessionData, copyPOST);
            });
        }

        /// <summary>
        /// adds a page to the server, that redirects to "destinationURL"
        /// </summary>
        public static void addRedirect(string originURL, string destinationURL, bool copyPOST = false)
        {
            addInstantPageResponse(originURL, (SessionData sessionData) =>
            {
                return generateRedirectCode(destinationURL, sessionData, copyPOST);
            });
        }

        /// <summary>
        /// adds a page to the server, that redirects to "destinationURL" and executes the given code
        /// </summary>
        public static void addRedirectWithCode(string originURL, string destinationURL, Action<SessionData> action, bool copyPOST = false)
        {
            addInstantPageResponse(originURL, (SessionData sessionData) =>
            {
                action(sessionData);
                return generateRedirectCode(destinationURL, sessionData, copyPOST);
            });
        }

        /// <summary>
        /// adds a page to the server, that redirects to "destinationURLifTRUE" if the conditional code returns true and redirects to "destinationURLifFALSE" if the conditional code returns false
        /// </summary>
        public static void addConditionalRedirect(string originalURL, string destinationURLifTRUE, string destinationURLifFALSE, Func<SessionData, bool> conditionalCode, bool copyPOST = false)
        {
            addInstantPageResponse(originalURL, (SessionData sessionData) =>
            {
                if (conditionalCode(sessionData))
                    return generateRedirectCode(destinationURLifTRUE, sessionData, copyPOST);

                return generateRedirectCode(destinationURLifFALSE, sessionData, copyPOST);
            });
        }

        /// <summary>
        /// adds a page to the server, that redirects if the conditional code returns true and executes other code if the conditional code returns false
        /// </summary>
        public static void addRedirectOrCode(string originalURL, string destinationURLifTRUE, Master.getContents codeIfFALSE, Func<SessionData, bool> conditionalCode, bool copyPOST = false)
        {
            addInstantPageResponse(originalURL, (SessionData sessionData) =>
            {
                if (conditionalCode(sessionData))
                    return generateRedirectCode(destinationURLifTRUE, sessionData, copyPOST);

                return codeIfFALSE(sessionData);
            });
        }

        /// <summary>
        /// adds a temporary page to the server, that redirects to "destinationURL" (only available for ONE request)
        /// </summary>
        /// <param name="destinationURL">the desired URL to reach</param>
        /// <param name="instantlyRemove">runtime code should instantly remove these - constructors should not remove, since then they'll be gone the next compile</param>
        /// <param name="copyPOST">specifies whether all POST values given should be copied throughout the whole redirecting process</param>
        /// <returns>the name at which this temporary page will be available at.</returns>
        public static string addOneTimeRedirect(string destinationURL, bool instantlyRemove, bool copyPOST = false)
        {
            return addOneTimeInstantPageResponse((SessionData sessionData) =>
            {
                return generateRedirectCode(destinationURL, sessionData, copyPOST);
            }
                , instantlyRemove);
        }

        /// <summary>
        /// adds a temporary page to the server, that redirects to "destinationURL" and executes the given code (only available for ONE request)
        /// </summary>
        /// <param name="destinationURL">the desired URL to reach</param>
        /// <param name="instantlyRemove">runtime code should instantly remove these - constructors should not remove, since then they'll be gone the next compile</param>
        /// <param name="action">the code to execute</param>
        /// <param name="copyPOST">specifies whether all POST values given should be copied throughout the whole redirecting process</param>
        public static string addOneTimeRedirectWithCode(string destinationURL, bool instantlyRemove, Action<SessionData> action, bool copyPOST = false)
        {
            return addOneTimeInstantPageResponse((SessionData sessionData) =>
            {
                action(sessionData);
                return generateRedirectCode(destinationURL, sessionData, copyPOST);
            }
            , instantlyRemove);
        }

        /// <summary>
        /// adds a temporary page to the server, that redirects to "destinationURL" in X milliseconds (only available for ONE request)
        /// </summary>
        /// <param name="destinationURL">the desired URL to reach</param>
        /// <param name="message">the message to display</param>
        /// <param name="milliseconds">the amount of milliseconds to wait before redirecting</param>
        /// <param name="instantlyRemove">runtime code should instantly remove these - constructors should not remove, since then they'll be gone the next compile</param>
        /// <param name="copyPOST">specifies whether all POST values given should be copied throughout the whole redirecting process</param>
        /// <returns>the name at which this temporary page will be available at.</returns>
        public static string addOneTimeTimedRedirect(string destinationURL, string message, int milliseconds, bool instantlyRemove, bool copyPOST = false)
        {
            return addOneTimeInstantPageResponse((SessionData sessionData) =>
            {
                return generateRedirectInMillisecondsCode(destinationURL, message, milliseconds, sessionData, copyPOST);
            }
            , instantlyRemove);
        }

        /// <summary>
        /// adds a temporary page to the server, that redirects to "destinationURLifTRUE" if the conditional code returns true and redirects to "destinationURLifFALSE" if the conditional code returns false (only available for ONE request)
        /// </summary>
        /// <param name="destinationURLifTRUE">the desired URL to reach if the code returns true</param>
        /// <param name="destinationURLifFALSE">the desired URL to reach if the code returns false</param>
        /// <param name="instantlyRemove">runtime code should instantly remove these - constructors should not remove, since then they'll be gone the next compile</param>
        /// <param name="conditionalCode">the conditional code to execute</param>
        /// <param name="copyPOST">specifies whether all POST values given should be copied throughout the whole redirecting process</param>
        /// <returns>the name at which this temporary page will be available at.</returns>
        public static string addOneTimeConditionalRedirect(string destinationURLifTRUE, string destinationURLifFALSE, bool instantlyRemove, Func<SessionData, bool> conditionalCode, bool copyPOST = false)
        {
            return addOneTimeInstantPageResponse((SessionData sessionData) =>
            {
                if (conditionalCode(sessionData))
                    return generateRedirectCode(destinationURLifTRUE, sessionData, copyPOST);

                return generateRedirectCode(destinationURLifFALSE, sessionData, copyPOST);
            }
                , instantlyRemove);
        }

        /// <summary>
        /// adds a temporary page to the server, that redirects if the conditional code returns true and executes other code if the conditional code returns false (only available for ONE request)
        /// </summary>
        /// <param name="destinationURLifTRUE">the desired URL to reach if the conditional code returns true</param>
        /// <param name="codeIfFALSE">the code to be executed if the conditional code returns false</param>
        /// <param name="instantlyRemove">runtime code should instantly remove these - constructors should not remove, since then they'll be gone the next compile</param>
        /// <param name="conditionalCode">the conditional code to execute</param>
        /// <param name="copyPOST">specifies whether all POST values given should be copied throughout the whole redirecting process</param>
        /// <returns>the name at which this temporary page will be available at.</returns>
        public static string addOneTimeRedirectOrCode(string destinationURLifTRUE, Master.getContents codeIfFALSE, bool instantlyRemove, Func<SessionData, bool> conditionalCode, bool copyPOST = false)
        {
            return addOneTimeInstantPageResponse((SessionData sessionData) =>
            {
                if (conditionalCode(sessionData))
                    return generateRedirectCode(destinationURLifTRUE, sessionData, copyPOST);

                return codeIfFALSE(sessionData);
            }
            , instantlyRemove);
        }

        /// <summary>
        /// quickly generates a redirecting html page
        /// </summary>
        /// <param name="destinationURL">the desired url to reach</param>
        /// <param name="sessionData">the current SessionData</param>
        /// <param name="copyPOST">shall the POST-Values be copied?</param>
        /// <returns>the page as string</returns>
        public static string generateRedirectCode(string destinationURL, SessionData sessionData = null, bool copyPOST = false)
        {
            if(!copyPOST)
            {
                if (sessionData == null)
                {
                    return "<head><meta http-equiv=\"refresh\" content=\"1; url = "
                                   + destinationURL + "\"><script type=\"text/javascript\">window.location.href = \""
                                   + destinationURL + "\"</script><title>Page Redirection</title><style type=\"text/css\">hr{border:solid;border-width:3;color:#efefef;} p {overflow:overlay;}</style></head><body style='background-color: #f1f1f1;margin: 0;'><div style='font-family: \"Segoe UI\" ,sans-serif;width: 70%;max-width: 1200px;margin: 0em auto;font-size: 16pt;background-color: #fdfdfd;padding: 4em 8em;color: #4e4e4e;'><h1 style='font-weight: lighter;font-size: 50pt;'>Page Redirection</h1><hr><p>If you are not redirected automatically, follow this <a href='"
                                   + destinationURL + "'>link.</a></p><p style='text-align:right'>- LamestWebserver (LameOS)</p></div></body>";
                }
                else
                {
                    return "<head><script type=\"text/javascript\">onload = function(){var f=document.createElement('form');f.setAttribute('method','POST');f.setAttribute('action','"
                                    + destinationURL +
                                    "');f.setAttribute('enctype','application/x-www-form-urlencoded');var i=document.createElement('input');i.setAttribute('type','hidden');i.setAttribute('name','ssid');i.setAttribute('value','"
                                    + sessionData.ssid +
                                    "');f.appendChild(i);document.body.appendChild(f);f.submit();document.body.remove(f);}</script><title>Page Redirection</title><style type=\"text/css\">hr{border:solid;border-width:3;color:#efefef;} p {overflow:overlay;}</style></head><body style='background-color: #f1f1f1;margin: 0;'><div style='font-family: \"Segoe UI\" ,sans-serif;width: 70%;max-width: 1200px;margin: 0em auto;font-size: 16pt;background-color: #fdfdfd;padding: 4em 8em;color: #4e4e4e;'><h1 style='font-weight: lighter;font-size: 50pt;'>Page Redirection</h1><hr><p>If you are not redirected automatically, follow this <a href='#' onclick=\"var f=document.createElement('form');f.setAttribute('method','POST');f.setAttribute('action','"
                                    + destinationURL +
                                    "');f.setAttribute('enctype','application/x-www-form-urlencoded');var i=document.createElement('input');i.setAttribute('type','hidden');i.setAttribute('name','ssid');i.setAttribute('value','"
                                    + sessionData.ssid +
                                    "');f.appendChild(i);document.body.appendChild(f);f.submit();document.body.remove(f);\">link.</a></p><p style='text-align:right'>- LamestWebserver (LameOS)</p></div></body>";
                }
            }
            else
            {
                if (sessionData == null)
                {
                    return "<head><meta http-equiv=\"refresh\" content=\"1; url = "
                                   + destinationURL + "\"><script type=\"text/javascript\">window.location.href = \""
                                   + destinationURL + "\"</script><title>Page Redirection</title><style type=\"text/css\">hr{border:solid;border-width:3;color:#efefef;} p {overflow:overlay;}</style></head><body style='background-color: #f1f1f1;margin: 0;'><div style='font-family: \"Segoe UI\" ,sans-serif;width: 70%;max-width: 1200px;margin: 0em auto;font-size: 16pt;background-color: #fdfdfd;padding: 4em 8em;color: #4e4e4e;'><h1 style='font-weight: lighter;font-size: 50pt;'>Page Redirection</h1><hr><p>If you are not redirected automatically, follow this <a href='"
                                   + destinationURL + "'>link.</a></p><p style='text-align:right'>- LamestWebserver (LameOS)</p></div></body>";
                }
                else
                {
                    return "<head><script type=\"text/javascript\">onload = "
                                    + ScriptCollection.getPageReferalWithFullPOSTInMilliseconds(sessionData, new object[] { destinationURL, 0 }) + 
                                    "</script><title>Page Redirection</title><style type=\"text/css\">hr{border:solid;border-width:3;color:#efefef;} p {overflow:overlay;}</style></head><body style='background-color: #f1f1f1;margin: 0;'><div style='font-family: \"Segoe UI\" ,sans-serif;width: 70%;max-width: 1200px;margin: 0em auto;font-size: 16pt;background-color: #fdfdfd;padding: 4em 8em;color: #4e4e4e;'><h1 style='font-weight: lighter;font-size: 50pt;'>Page Redirection</h1><hr><p>If you are not redirected automatically, follow this <a href='#' onclick=\""
                                    + ScriptCollection.getPageReferalWithFullPOSTInMilliseconds(sessionData, new object[] { destinationURL, 0 }) +
                                    "\">link.</a></p><p style='text-align:right'>- LamestWebserver (LameOS)</p></div></body>";
                }
            }
        }

        /// <summary>
        /// quickly generates a redirecting html page that waits a few milliseconds and displays a message
        /// </summary>
        /// <param name="destinationURL">the desired url to reach</param>
        /// <param name="message">the message to display</param>
        /// <param name="milliseconds">the amount of milliseconds to wait</param>
        /// <param name="sessionData">the current SessionData</param>
        /// <param name="copyPOST">shall the POST-Values be copied?</param>
        /// <returns>the page as string</returns>
        public static string generateRedirectInMillisecondsCode(string destinationURL, string message, int milliseconds, SessionData sessionData = null, bool copyPOST = false)
        {
            if (!copyPOST)
            {
                if (sessionData == null)
                {
                    return "<head><meta http-equiv=\"refresh\" content=\"" + Math.Round((float)milliseconds / 1000f) + "; url = "
                                   + destinationURL + "\"><script type=\"text/javascript\">setTimeout(function() { window.location.href = \""
                                   + destinationURL + "\";}, "
                                   + milliseconds + ");</script><title>Page Redirection</title><style type=\"text/css\">hr{border:solid;border-width:3;color:#efefef;} p {overflow:overlay;}</style></head><body style='background-color: #f1f1f1;margin: 0;'><div style='font-family: \"Segoe UI\" ,sans-serif;width: 70%;max-width: 1200px;margin: 0em auto;font-size: 16pt;background-color: #fdfdfd;padding: 4em 8em;color: #4e4e4e;'><h2 style='font-weight: lighter;font-size: 40pt;'>"
                                   + message + "</h2><hr><p><i style='color:#404040;'>If you are not redirected automatically, follow this <a href='"
                                   + destinationURL + "'>link.</a></i></p></div></body>";
                }
                else
                {
                    return "<head><script type=\"text/javascript\">onload = function(){var f=document.createElement('form');f.setAttribute('method','POST');f.setAttribute('action','"
                                    + destinationURL +
                                    "');f.setAttribute('enctype','application/x-www-form-urlencoded');var i=document.createElement('input');i.setAttribute('type','hidden');i.setAttribute('name','ssid');i.setAttribute('value','"
                                    + sessionData.ssid +
                                    "');f.appendChild(i);document.body.appendChild(f);setTimeout(function() {f.submit();document.body.remove(f);}," + milliseconds + ");}</script><title>Page Redirection</title><style type=\"text/css\">hr{border:solid;border-width:3;color:#efefef;} p {overflow:overlay;}</style></head><body style='background-color: #f1f1f1;margin: 0;'><div style='font-family: \"Segoe UI\" ,sans-serif;width: 70%;max-width: 1200px;margin: 0em auto;font-size: 16pt;background-color: #fdfdfd;padding: 4em 8em;color: #4e4e4e;'><p><h2 style='font-weight: lighter;font-size: 40pt;'>"
                                    + message + "</h2></p><hr><p><i style='color:#404040;'>If you are not redirected automatically, follow this <a href='#' onclick=\"var f=document.createElement('form');f.setAttribute('method','POST');f.setAttribute('action','"
                                    + destinationURL +
                                    "');f.setAttribute('enctype','application/x-www-form-urlencoded');var i=document.createElement('input');i.setAttribute('type','hidden');i.setAttribute('name','ssid');i.setAttribute('value','"
                                    + sessionData.ssid +
                                    "');f.appendChild(i);document.body.appendChild(f);setTimeout(function() {f.submit();document.body.remove(f);}," + milliseconds + ");\">link.</a></i></p></div></body>";
                }
            }
            else
            {
                if (sessionData == null)
                {
                    return "<head><meta http-equiv=\"refresh\" content=\"" + Math.Round((float)milliseconds / 1000f) + "; url = "
                                   + destinationURL + "\"><script type=\"text/javascript\">setTimeout(function() { window.location.href = \""
                                   + destinationURL + "\";}, "
                                   + milliseconds + ");</script><title>Page Redirection</title><style type=\"text/css\">hr{border:solid;border-width:3;color:#efefef;} p {overflow:overlay;}</style></head><body style='background-color: #f1f1f1;margin: 0;'><div style='font-family: \"Segoe UI\" ,sans-serif;width: 70%;max-width: 1200px;margin: 0em auto;font-size: 16pt;background-color: #fdfdfd;padding: 4em 8em;color: #4e4e4e;'><h2 style='font-weight: lighter;font-size: 40pt;'>"
                                   + message + "</h2><hr><p><i style='color:#404040;'>If you are not redirected automatically, follow this <a href='"
                                   + destinationURL + "'>link.</a></i></p></div></body>";
                }
                else
                {
                    return "<head><script type=\"text/javascript\">onload = "
                                    + ScriptCollection.getPageReferalWithFullPOSTInMilliseconds(sessionData, new object[] { destinationURL, milliseconds }) +
                                    "</script><title>Page Redirection</title><style type=\"text/css\">hr{border:solid;border-width:3;color:#efefef;} p {overflow:overlay;}</style></head><body style='background-color: #f1f1f1;margin: 0;'><div style='font-family: \"Segoe UI\" ,sans-serif;width: 70%;max-width: 1200px;margin: 0em auto;font-size: 16pt;background-color: #fdfdfd;padding: 4em 8em;color: #4e4e4e;'><p><h2 style='font-weight: lighter;font-size: 40pt;'>"
                                    + message + "</h2></p><hr><p><i style='color:#404040;'>If you are not redirected automatically, follow this <a href='#' onclick=\""
                                    + ScriptCollection.getPageReferalWithFullPOSTInMilliseconds(sessionData, new object[] { destinationURL, milliseconds }) +
                                    "\">link.</a></i></p></div></body>";
                }
            }
        }
    }
}
