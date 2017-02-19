using System;

namespace LamestWebserver.UI
{
    public static class ScriptCollection
    {
        public delegate string scriptFuction(AbstractSessionIdentificator sessionData, object[] arguments);

        public static string getPageReloadWithFullPOSTInMilliseconds(AbstractSessionIdentificator sessionData, object[] millisecondsAsInt)
        {
            if (millisecondsAsInt.Length != 1)
                throw new ArgumentException("the argument has to be an object[1] containing one integer number");

            if (sessionData == null || string.IsNullOrWhiteSpace(sessionData.Ssid))
                return "setTimeout(function() { window.location = window.location; }," + int.Parse(millisecondsAsInt[0].ToString()) + ");";

            string ret = "setTimeout(function(){var f=document.createElement('form');f.setAttribute('method','POST');f.setAttribute('action',window.location);f.setAttribute('enctype','application/x-www-form-urlencoded');var i;";

            if (sessionData is SessionData)
            {
                for (int i = 0; i < ((SessionData)sessionData).HttpPostParameters.Count; i++)
                {
                    ret += "i=document.createElement('input');i.setAttribute('type','hidden');i.setAttribute('name','"
                           + ((SessionData)sessionData).HttpPostParameters[i].Replace("\n", "\\n") + "');i.setAttribute('value','"
                           + ((SessionData)sessionData).HttpPostValues[i].Replace("\n", "\\n") + "');f.appendChild(i);";
                }
            }

            ret += "document.body.appendChild(f);f.submit();document.body.remove(f);}, " + int.Parse(millisecondsAsInt[0].ToString()) + ");";

            return ret;
        }

        public static string getPageReloadInMilliseconds(AbstractSessionIdentificator sessionData, object[] millisecondsAsInt)
        {
            if (millisecondsAsInt.Length != 1)
                throw new ArgumentException("the argument has to be an object[1] containing one integer number");

            if (sessionData == null || string.IsNullOrWhiteSpace(sessionData.Ssid))
                return "setTimeout(function() { window.location = window.location; }," + int.Parse(millisecondsAsInt[0].ToString()) + ");";

            string ret = "setTimeout(function(){var f=document.createElement('form');f.setAttribute('method','POST');f.setAttribute('action',window.location);f.setAttribute('enctype','application/x-www-form-urlencoded');var i=document.createElement('input');i.setAttribute('type','hidden');i.setAttribute('name','ssid');i.setAttribute('value','"
                        + sessionData.Ssid + "');f.appendChild(i);document.body.appendChild(f);f.submit();document.body.remove(f);}, "
                        + int.Parse(millisecondsAsInt[0].ToString()) + ");";

            return ret;
        }

        public static string getPageReferalToXInMilliseconds(AbstractSessionIdentificator sessionData, object[] arguments)
        {
            if (arguments.Length != 2)
                throw new ArgumentException("the argument has to be an object[2] containing one string and one integer number");

            if (sessionData == null || string.IsNullOrWhiteSpace(sessionData.Ssid))
                return "setTimeout(function() { window.location = '" + arguments[0] + "'; }," + int.Parse(arguments[1].ToString()) + ");";

            string ret = "setTimeout(function(){var f=document.createElement('form');f.setAttribute('method','POST');f.setAttribute('action','" 
                        + arguments[0] + "');f.setAttribute('enctype','application/x-www-form-urlencoded');var i=document.createElement('input');i.setAttribute('type','hidden');i.setAttribute('name','ssid');i.setAttribute('value','"
                        + sessionData.Ssid + "');f.appendChild(i);document.body.appendChild(f);f.submit();document.body.remove(f);}, " 
                        + int.Parse(arguments[1].ToString()) + ");";

            return ret;
        }

        public static string getPageReferalToX(AbstractSessionIdentificator sessionData, object[] arguments)
        {
            if (arguments.Length != 1)
                throw new ArgumentException("the argument has to be an object[1] containing one string");

            if (sessionData == null || string.IsNullOrWhiteSpace(sessionData.Ssid))
                return "window.location = '" + arguments[0] + "';";

            string ret = "onload = function() {var f=document.createElement('form');f.setAttribute('method','POST');f.setAttribute('action','"
                        + arguments[0] + "');f.setAttribute('enctype','application/x-www-form-urlencoded');var i=document.createElement('input');i.setAttribute('type','hidden');i.setAttribute('name','ssid');i.setAttribute('value','"
                        + sessionData.Ssid + "');f.appendChild(i);document.body.appendChild(f);f.submit();document.body.remove(f);};";

            return ret;
        }

        public static string getPageReferalWithFullPOSTInMilliseconds(AbstractSessionIdentificator sessionData, object[] arguments)
        {
            if (arguments.Length != 2)
                throw new ArgumentException("the argument has to be an object[2] containing one string and one integer number");

            if (sessionData == null || string.IsNullOrWhiteSpace(sessionData.Ssid))
                return "onload = setTimeout(function() { window.location = '" + arguments[0] + "'; }," + int.Parse(arguments[1].ToString()) + ");";

            string ret = "onload = setTimeout(function(){var f=document.createElement('form');f.setAttribute('method','POST');f.setAttribute('action','"
                + arguments[0]
                + "');f.setAttribute('enctype','application/x-www-form-urlencoded');var i;";

            if (sessionData is SessionData)
            {
                for (int i = 0; i < ((SessionData) sessionData).HttpPostParameters.Count; i++)
                {
                    ret += "i=document.createElement('input');i.setAttribute('type','hidden');i.setAttribute('name','"
                           + ((SessionData) sessionData).HttpPostParameters[i].Replace("\n", "\\n") + "');i.setAttribute('value','"
                           + ((SessionData) sessionData).HttpPostValues[i].Replace("\n", "\\n") + "');f.appendChild(i);";
                }
            }

            ret += "document.body.appendChild(f);f.submit();document.body.remove(f);}, " + int.Parse(arguments[1].ToString()) + ");";

            return ret;
        }
    }
}
