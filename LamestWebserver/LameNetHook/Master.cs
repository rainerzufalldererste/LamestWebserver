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
    }
}
