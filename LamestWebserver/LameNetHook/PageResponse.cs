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
    }
}
