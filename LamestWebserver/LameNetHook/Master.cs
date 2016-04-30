using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LameNetHook
{
    public static class Master
    {
        public delegate string getContents(SessionData data);

        public delegate void addFunction(string hash, getContents function);
        public static event addFunction addFunctionEvent;

        internal static void callAddFunctionEvent(string hashname, getContents getc)
        {
            addFunctionEvent(hashname, getc);
        }

        public static string getErrorMsg(string title, string message)
        {
            return "<head><title>" + title 
                + "</title><style type=\"text/css\">hr{border:solid;border-width:5;color:#FDCD48;'><p style='overflow:overlay;}</style></head><body style='background-color:#f0f0f0;background-image: url(\"/server/error.png\");background-repeat:repeat;background-size:125px;'><div style='font-family:\"Segoe UI\",sans-serif;width:70%;max-width:800px;margin:5em auto;padding:50px;background-color:#fff;border-radius: 1em;padding-top:22px;padding-bottom:22px;border:solid;border-color:#FDD248;border-width:1;'><h1>"
                + title + "</h1><hr>" + message.Replace("\n","<br>") + "<p style='text-align:right'>- LamestWebserver (LameOS)</p></div></body>";
        }
    }
}
