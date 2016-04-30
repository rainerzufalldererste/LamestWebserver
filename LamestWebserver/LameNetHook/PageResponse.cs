using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LameNetHook
{
    public abstract class PageResponse
    {
        public string URL { get; protected set; }

        public PageResponse(string URL, bool register = true)
        {
            this.URL = URL;

            if(register)
                Master.callAddFunctionEvent(URL, getContents);
        }

        protected abstract string getContents(SessionData sessionData);
    }

    public static class InstantPageResponse
    {
        public static void addInstantPageResponse(string URL, Master.getContents code)
        {
            Master.callAddFunctionEvent(URL, code);
        }

        public static void generateRedirect(string originURL, string destinationURL)
        {
            addInstantPageResponse(originURL, 
                (SessionData sessionData) => {
                    return "<head><meta http-equiv=\"refresh\" content=\"1; url = "
                        + destinationURL + "\"><script type=\"text/javascript\">window.location.href = \""
                        + destinationURL + "\"</script><title>Page Redirection</title><style type=\"text/css\">hr{border:solid;border-width:5;color:#FDCD48;'><p style='overflow:overlay;}</style></head><body style='background-color:#f0f0f0;background-image: url(\"/server/error.png\");background-repeat:repeat;background-size:125px;'><div style='font-family:\"Segoe UI\",sans-serif;width:70%;max-width:800px;margin:5em auto;padding:50px;background-color:#fff;border-radius: 1em;padding-top:22px;padding-bottom:22px;border:solid;border-color:#FDD248;border-width:1;'><h1>Page Redirection</h1><hr><p>If you are not redirected automatically, follow this <a href='"
                        + destinationURL + "'>link.</a></p><p style='text-align:right'>- LamestWebserver (LameOS)</p></div></body>";
                });
        }
    }
}
