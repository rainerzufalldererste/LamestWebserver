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

                if (replaceHREFs)
                    processHREFs(ref ret, sessionData);

                processInsertions(ref ret, sessionData);
            }
            catch(Exception e)
            {
                ret = Master.getErrorMsg("Exception in PageFiller '" + URL + "'", "<b>An Error occured while processing the output</b><br>" + e.ToString());
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

            for (int i = 0; i < ret.Length - 1; i++)
            {
                if((ret[i] == '<' && ret[i + 1] == 'a') || (i > 5 && ret.Substring(i-6,7) == "<button"))
                {
                    int state = 0;
                    int hrefPos = -1, onclickPos = -1;
                    int linkStartPos = -1, onclickStartPos = -1;
                    int linkEndPos = -1, onclickEndPos = -1;
                    char stringEndChar = '\0';

                    // search for href
                    for (int j = i + 3; j < ret.Length - 5; j++)
                    {
                        if(state == 0 && j < ret.Length - 5 && ret.Substring(j, 4) == "href" && hrefPos == -1)
                        {
                            j += 3;
                            hrefPos = j;
                            state = 1;
                        }
                        else if(state == 1 && ret[j] == '=')
                        {
                            state = 2;
                        }
                        else if(state == 2 && (ret[j] == '\'' || ret[j] == '\"'))
                        {
                            state = 3;
                            stringEndChar = ret[j];
                            linkStartPos = j + 1;

                            if (j + 1 < ret.Length && ret[j + 1] == '#')
                            {
                                goto CONTINUE_SEARCH_FOR_LINK_TAG;
                            }

                            j++;
                        }
                        else if(state == 3 && j > linkStartPos + 1 && ret[j] == stringEndChar)
                        {
                            state = 0;
                            linkEndPos = j - 1;
                        }
                        else if(state == 0 && j < ret.Length - 5 && ret.Substring(j, 7) == "onclick" && onclickPos == -1)
                        {
                            state = -1;
                            j += 6;
                        }
                        else if(state == -1 && ret[j] == '=')
                        {
                            state = -2;
                        }
                        else if(state == -2 && (ret[j] == '\'' || ret[j] == '\"'))
                        {
                            stringEndChar = ret[j];
                            state = -3;
                            onclickStartPos = j + 1;
                        }
                        else if(state == -3 && ret[j] == stringEndChar)
                        {
                            onclickEndPos = j - 1;
                            state = 0;
                        }
                        else if(ret[j] == '>')
                        {
                            if(linkStartPos > -1 && linkEndPos > -1)
                            {
                                ret = ret.Remove(linkStartPos - 1, 1);
                                ret = ret.Insert(linkStartPos - 1, "\"");

                                ret = ret.Remove(linkEndPos + 1, 1);
                                ret = ret.Insert(linkEndPos + 1, "\"");

                                if (onclickStartPos > -1 && onclickEndPos > -1)
                                {
                                    ret = ret.Remove(onclickStartPos - 1, 1);
                                    ret = ret.Insert(onclickStartPos - 1, "\"");

                                    ret = ret.Remove(onclickEndPos + 1, 1);
                                    ret = ret.Insert(onclickEndPos + 1, "\"");

                                    string hash = SessionContainer.getHash();
                                    string add = ";var f_"
                                        + hash + "=document.createElement('form');f_"
                                        + hash + ".setAttribute('method','POST');f_"
                                        + hash + ".setAttribute('action','"
                                        + ret.Substring(linkStartPos, linkEndPos - linkStartPos + 1) + "');f_"
                                        + hash + ".setAttribute('enctype','text/html');var i_"
                                        + hash + "=document.createElement('input');i_"
                                        + hash + ".setAttribute('type','hidden');i_"
                                        + hash + ".setAttribute('name','ssid');i_"
                                        + hash + ".setAttribute('value','"
                                        + sessionData.ssid + "');f_"
                                        + hash + ".appendChild(i_"
                                        + hash + ");document.body.appendChild(f_"
                                        + hash + ");f_"
                                        + hash + ".submit();document.body.remove(f_"
                                        + hash + ");";

                                    if (onclickStartPos > linkStartPos)
                                    {
                                        ret = ret.Insert(onclickEndPos + 1, add);
                                        j += add.Length;

                                        ret = ret.Remove(linkStartPos, linkEndPos - linkStartPos + 1);
                                        ret = ret.Insert(linkStartPos, "#");
                                        j -= (linkEndPos - 1);
                                    }
                                    else
                                    {
                                        ret = ret.Remove(linkStartPos, linkEndPos - linkStartPos + 1);
                                        ret = ret.Insert(linkStartPos, "#");
                                        j -= (linkEndPos - 1);
                                        
                                        ret = ret.Insert(onclickEndPos + 1, add);
                                        j += add.Length;
                                    }
                                }
                                else
                                {
                                    string add = "#\" onclick =\"var f=document.createElement('form');f.setAttribute('method','POST');f.setAttribute('action','"
                                        + ret.Substring(linkStartPos, linkEndPos - linkStartPos + 1)
                                        + "');f.setAttribute('enctype','text/html');var i=document.createElement('input');i.setAttribute('type','hidden');i.setAttribute('name','ssid');i.setAttribute('value','"
                                        + sessionData.ssid
                                        + "');f.appendChild(i);document.body.appendChild(f);f.submit();document.body.remove(f);";

                                    ret = ret.Remove(linkStartPos, linkEndPos - linkStartPos + 1);
                                    j -= linkEndPos;
                                    ret = ret.Insert(linkStartPos, add);
                                    j += add.Length;
                                }
                            }

                            i = j;
                            goto CONTINUE_SEARCH_FOR_LINK_TAG;
                        }
                    }
                    CONTINUE_SEARCH_FOR_LINK_TAG:;
                }
            }
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
