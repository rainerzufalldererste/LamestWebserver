using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LameNetHook
{
    public abstract class PageFiller
    {
        public readonly string URL;

        /// <summary>
        /// Replace the HREFs on this Page to include the sessionID
        /// </summary>
        protected bool replaceHREFs = true;

        public PageFiller(string URL)
        {
            this.URL = URL;
        }

        public void register(string hashname = null)
        {
            if (hashname == null)
                hashname = URL;

            Master.callAddFunctionEvent(hashname, this.getContents);
        }

        private string getContents(SessionData sessionData)
        {
            string ret = "";

            try
            {
                ret = System.IO.File.ReadAllText(sessionData.path + "\\" + URL);

                processData(sessionData, ref ret);

                processInsertions(ref ret);

                if (replaceHREFs)
                    processHREFs(ref ret);
            }
            catch(Exception e)
            {
                ret = "<b>An Error occured while processing the output</b><br>" + e.ToString().Replace("\r\n", "<br>");
            }

            return ret;
        }

        private void processInsertions(ref string ret)
        {
            // TODO: <ISSID> to a hidden input containing the SSID
            // TODO: <SSID> to the SSID
        }

        private void processHREFs(ref string ret)
        {
            // TODO: href="#" untouched
            // TODO: href="somelink.html?123=bla" even with onclick="xyz" to contain the ssid in post
        }

        public abstract void processData(SessionData sessionData, ref string output);

        public void setValue(string key, string value, ref string output)
        {
            if (key == null)
                return;

            int length = (6 + key.Length);

            for (int i = 0; i < output.Length - length; i++)
            {
                if(output.Substring(i,length) == "<? " + key  + " ?>")
                {
                    output = output.Remove(i, length);

                    if(value != null)
                        output = output.Insert(i, value);

                    return;
                }
            }
        }
    }
}
