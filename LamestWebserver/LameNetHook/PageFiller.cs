using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LameNetHook
{
    public abstract class PageFiller
    {
        /// <summary>
        /// the URL, this page reads from before parsing into
        /// </summary>
        public readonly string URL;

        /// <summary>
        /// Replace the HREFs on this Page to include the sessionID
        /// </summary>
        protected bool replaceHREFs = true;

        public PageFiller(string URL)
        {
            this.URL = URL;
        }

        public void register()
        {
            Master.callAddFunctionEvent(URL, this.getContents);
        }

        private string getContents(SessionData sessionData)
        {
            string ret = "";

            try
            {
                ret = System.IO.File.ReadAllText(sessionData.path + "\\" + URL);

                processData(sessionData, ref ret);

                processInsertions(ref ret, sessionData);

                if (replaceHREFs)
                    processHREFs(ref ret, sessionData);
            }
            catch(Exception e)
            {
                ret = "<b>An Error occured while processing the output</b><br>" + e.ToString().Replace("\r\n", "<br>");
            }

            return ret;
        }

        private void processInsertions(ref string ret, SessionData sessionData)
        {
            // <ISSID> to a hidden input containing the SSID
            ret = ret.Replace("<ISSID>","<input type='hidden' name='ssid' value='" + sessionData.ssid + "'>");

            // <SSID> to the SSID
            ret = ret.Replace("<SSID>", sessionData.ssid);

            // <HREF(xyz)> to a link to xyz containing the SSID
            for (int i = 2; i < ret.Length - 9; i++)
            {
                if (ret[i - 2] == '<' && ret[i - 1] == 'a' && ret.Substring(i, 6) == " HREF(")
                {
                    for (int j = i + 7; j < ret.Length - 1; j++)
                    {
                        if(ret[j] == ')')
                        {
                            string href = ret.Substring(i + 6, (j + 1) - i - 7);
                            ret = ret.Remove(i, (j + 1) - i);
                            ret = ret.Insert(i, " href=\"#\" onclick=\"var f=document.createElement('form');f.setAttribute('method','POST');f.setAttribute('action','" + href +
                                "');f.setAttribute('enctype','text/html');var i=document.createElement('input');i.setAttribute('type','hidden');i.setAttribute('name','ssid');i.setAttribute('value','" + sessionData.ssid +
                                "');f.appendChild(i);document.body.appendChild(f);f.submit();document.body.remove(f);\"");

                            i = j + 1;
                            break;
                        }
                    }
                }
            }
        }

        private void processHREFs(ref string ret, SessionData sessionData)
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
