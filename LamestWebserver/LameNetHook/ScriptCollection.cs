using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LameNetHook
{
    public static class ScriptCollection
    {
        public delegate string scriptFuction(SessionData sessionData, object[] arguments);

        public static string getPageReloadInMilliseconds(SessionData sessionData, object[] millisecondsAsInt)
        {
            if (millisecondsAsInt.Length != 1)
                throw new ArgumentException("the argument has to be an object[1] containing one integer number");

            if (sessionData == null || string.IsNullOrWhiteSpace(sessionData.ssid))
                return "setTimeout(function() { window.location = window.location; }," + int.Parse(millisecondsAsInt[0].ToString()) + ");";

            string ret = "setTimeout(function(){var f=document.createElement('form');f.setAttribute('method','POST');f.setAttribute('action',window.location);f.setAttribute('enctype','application/x-www-form-urlencoded');var i;";
            
            for (int i = 0; i < sessionData.varsPOST.Count; i++)
            {
                ret += "i=document.createElement('input');i.setAttribute('type','hidden');i.setAttribute('name','" 
                        + sessionData.varsPOST[i] + "');i.setAttribute('value','"
                        + sessionData.valuesPOST[i] + "');f.appendChild(i);";
            }

            ret += "document.body.appendChild(f);f.submit();document.body.remove(f);}, " + int.Parse(millisecondsAsInt[0].ToString()) + ");";

            return ret;
        }

        public static string getPageReferalToXInMilliseconds(SessionData sessionData, object[] arguments)
        {
            if (arguments.Length != 2)
                throw new ArgumentException("the argument has to be an object[2] containing one string and one integer number");

            if (sessionData == null || string.IsNullOrWhiteSpace(sessionData.ssid))
                return "setTimeout(function() { window.location = '" + arguments[0] + "'; }," + int.Parse(arguments[1].ToString()) + ");";

            string ret = "setTimeout(function(){var f=document.createElement('form');f.setAttribute('method','POST');f.setAttribute('action','" 
                        + arguments[0] + "');f.setAttribute('enctype','application/x-www-form-urlencoded');var i=document.createElement('input');i.setAttribute('type','hidden');i.setAttribute('name','ssid');i.setAttribute('value','"
                        + sessionData.ssid + "');f.appendChild(i);document.body.appendChild(f);f.submit();document.body.remove(f);}, " 
                        + int.Parse(arguments[1].ToString()) + ");";

            return ret;
        }

        public static string getPageReferalToX(SessionData sessionData, object[] arguments)
        {
            if (arguments.Length != 1)
                throw new ArgumentException("the argument has to be an object[1] containing one string");

            if (sessionData == null || string.IsNullOrWhiteSpace(sessionData.ssid))
                return "window.location = '" + arguments[0] + ";'";

            string ret = "var f=document.createElement('form');f.setAttribute('method','POST');f.setAttribute('action','"
                        + arguments[0] + "');f.setAttribute('enctype','application/x-www-form-urlencoded');var i=document.createElement('input');i.setAttribute('type','hidden');i.setAttribute('name','ssid');i.setAttribute('value','"
                        + sessionData.ssid + "');f.appendChild(i);document.body.appendChild(f);f.submit();document.body.remove(f);";

            return ret;
        }
    }
}
