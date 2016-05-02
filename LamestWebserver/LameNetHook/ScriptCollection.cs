using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LameNetHook
{
    public static class ScriptCollection
    {
        public delegate string scriptFuction(SessionData sessionData, object arguments);

        public static string getPageReloadAtMilliseconds(SessionData sessionData, object millisecondsAsInt)
        {
            if (sessionData == null || string.IsNullOrWhiteSpace(sessionData.ssid))
                return "setTimeout(function() { window.location = window.location; }," + millisecondsAsInt + ");";

            string ret = "setTimeout(function(){var f=document.createElement('form');f.setAttribute('method','POST');f.setAttribute('action',window.location);f.setAttribute('enctype','application/x-www-form-urlencoded');var i;";
            
            for (int i = 0; i < sessionData.varsPOST.Count; i++)
            {
                ret += "i=document.createElement('input');i.setAttribute('type','hidden');i.setAttribute('name','" 
                        + sessionData.varsPOST[i] + "');i.setAttribute('value','"
                        + sessionData.valuesPOST[i] + "');f.appendChild(i);";
            }

            ret += "document.body.appendChild(f);f.submit();document.body.remove(f);}, " + millisecondsAsInt + ");";

            return ret;
        }
    }
}
