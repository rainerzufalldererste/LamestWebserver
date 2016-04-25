using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LameRessources
{
    public abstract class PageFiller
    {
        public readonly string URL;

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
                ret = System.IO.File.ReadAllText(URL);
                processData(sessionData, ref ret);
            }
            catch(Exception e)
            {
                ret = "<b>An Error occured while processing the output</b><br>" + e.Message.Replace("\n","<br>") + "<br>" + e.Source.Replace("\n","<br>") + "<br>" + e.TargetSite.ToString().Replace("\n","<br>");
            }

            return ret;
        }

        public abstract void processData(SessionData sessionData, ref string output);

        public void placeValue(string key, string value, ref string output)
        {
            int length = (7 + key.Length);

            for (int i = 0; i < output.Length - length; i++)
            {
                if(output.Substring(i,length) == "<? '" + key  + "' >")
                {
                    output.Remove(i, length);
                    output.Insert(i, value);
                    return;
                }
            }
        }
    }
}
