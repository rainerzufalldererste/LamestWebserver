using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LamestWebserver
{
    public abstract class PageResponse
    {
        public string URL { get; protected set; }

        public PageResponse(string URL, bool register = true)
        {
            this.URL = URL;

            if(register)
                Master.addFuntionToServer(URL, getContents);
        }

        protected void removeFromServer()
        {
            Master.removeFunctionFromServer(URL);
        }

        protected abstract string getContents(SessionData sessionData);
    }

    public abstract class SyncronizedPageResponse : PageResponse
    {
        private UsableMutex mutex = new UsableMutex();

        public SyncronizedPageResponse(string URL, bool register = true) : base(URL, false)
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

        protected override abstract string getContents(SessionData sessionData);
    }

    public abstract class ElementResponse
    {
        public string URL { get; protected set; }

        public ElementResponse(string URL, bool register = true)
        {
            this.URL = URL;

            if (register)
                Master.addFuntionToServer(URL, getContents);
        }

        protected void removeFromServer()
        {
            Master.removeFunctionFromServer(URL);
        }

        private string getContents(SessionData sessionData)
        {
            return getElement(sessionData) * sessionData;
        }

        protected abstract HElement getElement(SessionData sessionData);
    }

    public abstract class SyncronizedElementResponse
    {
        public string URL { get; protected set; }
        private UsableMutex mutex = new UsableMutex();

        public SyncronizedElementResponse(string URL, bool register = true)
        {
            this.URL = URL;

            if (register)
                Master.addFuntionToServer(URL, getContents);
        }

        protected void removeFromServer()
        {
            Master.removeFunctionFromServer(URL);
        }

        private string getContents(SessionData sessionData)
        {
            using (mutex.Lock())
            {
                return getElement(sessionData) * sessionData;
            }
        }

        protected abstract HElement getElement(SessionData sessionData);
    }

    public static class InstantPageResponse
    {
        private static System.Threading.Mutex mutex = new System.Threading.Mutex();
        private static List<string> oneTimePageResponses = new List<string>(); // TODO: Replace with AVLTree

        /// <summary>
        /// the maximum amount of oneTimePageResponses. if the count of them exceeds this number the fist ones will be removed. 0 for no limit.
        /// </summary>
        public static int maximumOneTimePageResponses = 2048;

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
            string hash = SessionContainer.generateHash();

            if (instantlyRemove)
            {
                mutex.WaitOne();

                if (maximumOneTimePageResponses > 0)
                {
                    if (oneTimePageResponses.Count + 1 > maximumOneTimePageResponses)
                    {
                        Master.removeFunctionFromServer(oneTimePageResponses[0]);
                        oneTimePageResponses.RemoveAt(0);
                    }
                }

                oneTimePageResponses.Add(hash);

                mutex.ReleaseMutex();
            }

            Master.addFuntionToServer(hash, (SessionData sessionData) => 
                {
                    if(instantlyRemove)
                        Master.removeFunctionFromServer(hash);
                    return code.Invoke(sessionData);
                });

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
        /// <param name="instantlyRemove">runtime code should instantly remove these - constructors should not remove, since then they'll be gone the next compile</param>
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
        /// <param name="instantlyRemove">runtime code should instantly remove these - constructors should not remove, since then they'll be gone the next compile</param>
        /// </summary>
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
        /// <param name="instantlyRemove">runtime code should instantly remove these - constructors should not remove, since then they'll be gone the next compile</param>
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
        /// <param name="instantlyRemove">runtime code should instantly remove these - constructors should not remove, since then they'll be gone the next compile</param>
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
        /// <param name="instantlyRemove">runtime code should instantly remove these - constructors should not remove, since then they'll be gone the next compile</param>
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

        public static string generateRedirectCode(string destinationURL, SessionData sessionData = null, bool copyPOST = false)
        {
            if(!copyPOST)
            {
                if (sessionData == null)
                {
                    return "<head><meta http-equiv=\"refresh\" content=\"1; url = "
                                   + destinationURL + "\"><script type=\"text/javascript\">window.location.href = \""
                                   + destinationURL + "\"</script><title>Page Redirection</title><style type=\"text/css\">hr{border:solid;border-width:5;color:#FDCD48;'><p style='overflow:overlay;}</style></head><body style='background-color:#f0f0f0;background-image: url(\"/server/error.png\");background-repeat:repeat;background-size:125px;'><div style='font-family:\"Segoe UI\",sans-serif;width:70%;max-width:800px;margin:5em auto;padding:50px;background-color:#fff;border-radius: 1em;padding-top:22px;padding-bottom:22px;border:solid;border-color:#FDD248;border-width:1;'><h1>Page Redirection</h1><hr><p>If you are not redirected automatically, follow this <a href='"
                                   + destinationURL + "'>link.</a></p><p style='text-align:right'>- LamestWebserver (LameOS)</p></div></body>";
                }
                else
                {
                    return "<head><script type=\"text/javascript\">onload = function(){var f=document.createElement('form');f.setAttribute('method','POST');f.setAttribute('action','"
                                    + destinationURL +
                                    "');f.setAttribute('enctype','application/x-www-form-urlencoded');var i=document.createElement('input');i.setAttribute('type','hidden');i.setAttribute('name','ssid');i.setAttribute('value','"
                                    + sessionData.ssid +
                                    "');f.appendChild(i);document.body.appendChild(f);f.submit();document.body.remove(f);}</script><title>Page Redirection</title><style type=\"text/css\">hr{border:solid;border-width:5;color:#FDCD48;'><p style='overflow:overlay;}</style></head><body style='background-color:#f0f0f0;background-image: url(\"/server/error.png\");background-repeat:repeat;background-size:125px;'><div style='font-family:\"Segoe UI\",sans-serif;width:70%;max-width:800px;margin:5em auto;padding:50px;background-color:#fff;border-radius: 1em;padding-top:22px;padding-bottom:22px;border:solid;border-color:#FDD248;border-width:1;'><h1>Page Redirection</h1><hr><p>If you are not redirected automatically, follow this <a href='#' onclick=\"var f=document.createElement('form');f.setAttribute('method','POST');f.setAttribute('action','"
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
                                   + destinationURL + "\"</script><title>Page Redirection</title><style type=\"text/css\">hr{border:solid;border-width:5;color:#FDCD48;'><p style='overflow:overlay;}</style></head><body style='background-color:#f0f0f0;background-image: url(\"/server/error.png\");background-repeat:repeat;background-size:125px;'><div style='font-family:\"Segoe UI\",sans-serif;width:70%;max-width:800px;margin:5em auto;padding:50px;background-color:#fff;border-radius: 1em;padding-top:22px;padding-bottom:22px;border:solid;border-color:#FDD248;border-width:1;'><h1>Page Redirection</h1><hr><p>If you are not redirected automatically, follow this <a href='"
                                   + destinationURL + "'>link.</a></p><p style='text-align:right'>- LamestWebserver (LameOS)</p></div></body>";
                }
                else
                {
                    return "<head><script type=\"text/javascript\">onload = "
                                    + ScriptCollection.getPageReferalWithFullPOSTInMilliseconds(sessionData, new object[] { destinationURL, 0 }) + 
                                    "</script><title>Page Redirection</title><style type=\"text/css\">hr{border:solid;border-width:5;color:#FDCD48;'><p style='overflow:overlay;}</style></head><body style='background-color:#f0f0f0;background-image: url(\"/server/error.png\");background-repeat:repeat;background-size:125px;'><div style='font-family:\"Segoe UI\",sans-serif;width:70%;max-width:800px;margin:5em auto;padding:50px;background-color:#fff;border-radius: 1em;padding-top:22px;padding-bottom:22px;border:solid;border-color:#FDD248;border-width:1;'><h1>Page Redirection</h1><hr><p>If you are not redirected automatically, follow this <a href='#' onclick=\""
                                    + ScriptCollection.getPageReferalWithFullPOSTInMilliseconds(sessionData, new object[] { destinationURL, 0 }) +
                                    "\">link.</a></p><p style='text-align:right'>- LamestWebserver (LameOS)</p></div></body>";
                }
            }
        }

        public static string generateRedirectInMillisecondsCode(string destinationURL, string message, int milliseconds, SessionData sessionData = null, bool copyPOST = false)
        {
            if (!copyPOST)
            {
                if (sessionData == null)
                {
                    return "<head><meta http-equiv=\"refresh\" content=\"" + Math.Round((float)milliseconds / 1000f) + "; url = "
                                   + destinationURL + "\"><script type=\"text/javascript\">setTimeout(function() { window.location.href = \""
                                   + destinationURL + "\";}, "
                                   + milliseconds + ");</script><title>Page Redirection</title><style type=\"text/css\">hr{border:solid;border-width:5;color:#FDCD48;'><p style='overflow:overlay;}</style></head><body style='background-color:#f0f0f0;background-image: url(\"/server/error.png\");background-repeat:repeat;background-size:125px;'><div style='font-family:\"Segoe UI\",sans-serif;width:70%;max-width:800px;margin:5em auto;padding:50px;background-color:#fff;border-radius: 1em;padding-top:22px;padding-bottom:22px;border:solid;border-color:#FDD248;border-width:1;'><h2>"
                                   + message + "</h2><hr><p><i style='color:#404040;'>If you are not redirected automatically, follow this <a href='"
                                   + destinationURL + "'>link.</a></i></p></div></body>";
                }
                else
                {
                    return "<head><script type=\"text/javascript\">onload = function(){var f=document.createElement('form');f.setAttribute('method','POST');f.setAttribute('action','"
                                    + destinationURL +
                                    "');f.setAttribute('enctype','application/x-www-form-urlencoded');var i=document.createElement('input');i.setAttribute('type','hidden');i.setAttribute('name','ssid');i.setAttribute('value','"
                                    + sessionData.ssid +
                                    "');f.appendChild(i);document.body.appendChild(f);setTimeout(function() {f.submit();document.body.remove(f);}," + milliseconds + ");}</script><title>Page Redirection</title><style type=\"text/css\">hr{border:solid;border-width:5;color:#FDCD48;'><p style='overflow:overlay;}</style></head><body style='background-color:#f0f0f0;background-image: url(\"/server/error.png\");background-repeat:repeat;background-size:125px;'><div style='font-family:\"Segoe UI\",sans-serif;width:70%;max-width:800px;margin:5em auto;padding:50px;background-color:#fff;border-radius: 1em;padding-top:22px;padding-bottom:22px;border:solid;border-color:#FDD248;border-width:1;'><p><h2>"
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
                                   + milliseconds + ");</script><title>Page Redirection</title><style type=\"text/css\">hr{border:solid;border-width:5;color:#FDCD48;'><p style='overflow:overlay;}</style></head><body style='background-color:#f0f0f0;background-image: url(\"/server/error.png\");background-repeat:repeat;background-size:125px;'><div style='font-family:\"Segoe UI\",sans-serif;width:70%;max-width:800px;margin:5em auto;padding:50px;background-color:#fff;border-radius: 1em;padding-top:22px;padding-bottom:22px;border:solid;border-color:#FDD248;border-width:1;'><h2>"
                                   + message + "</h2><hr><p><i style='color:#404040;'>If you are not redirected automatically, follow this <a href='"
                                   + destinationURL + "'>link.</a></i></p></div></body>";
                }
                else
                {
                    return "<head><script type=\"text/javascript\">onload = "
                                    + ScriptCollection.getPageReferalWithFullPOSTInMilliseconds(sessionData, new object[] { destinationURL, milliseconds }) +
                                    "</script><title>Page Redirection</title><style type=\"text/css\">hr{border:solid;border-width:5;color:#FDCD48;'><p style='overflow:overlay;}</style></head><body style='background-color:#f0f0f0;background-image: url(\"/server/error.png\");background-repeat:repeat;background-size:125px;'><div style='font-family:\"Segoe UI\",sans-serif;width:70%;max-width:800px;margin:5em auto;padding:50px;background-color:#fff;border-radius: 1em;padding-top:22px;padding-bottom:22px;border:solid;border-color:#FDD248;border-width:1;'><p><h2>"
                                    + message + "</h2></p><hr><p><i style='color:#404040;'>If you are not redirected automatically, follow this <a href='#' onclick=\""
                                    + ScriptCollection.getPageReferalWithFullPOSTInMilliseconds(sessionData, new object[] { destinationURL, milliseconds }) +
                                    "\">link.</a></i></p></div></body>";
                }
            }
        }
    }
}
