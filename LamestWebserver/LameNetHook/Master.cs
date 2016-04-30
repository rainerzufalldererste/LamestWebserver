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
            return "<title>" + title 
                + "</title><body style='background-color:#f0f0f2;background-image: url(\"/server/error.png\");background-repeat:repeat;background-size: 8%;'><div style='font-family:sans-serif;width:600px;margin:5em auto;padding:50px;background-color:#fff;border-radius: 1em;padding-top:22px;padding-bottom:22px;'><h1>"
                + title + "</h1><hr><p style='overflow:overlay;'>" + message.Replace("\n","<br>") + "</p><br><p style='text-align:right'>- LamestWebserver (LameOS)</p></div></body>";
        }
    }
}
