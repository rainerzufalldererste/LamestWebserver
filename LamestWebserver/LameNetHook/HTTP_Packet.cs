using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

namespace LameNetHook
{
    public class HTTP_Packet
    {
        public string[] Months = new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"};
        public string version = "HTTP/1.1";
        public string status = "200 OK";
        public string date/* = DateTime.Now.DayOfWeek.ToString().Substring(0,3) + ", " + DateTime.Now.Day + " " + Months[DateTime.Now.Month] + " " + DateTime.Now.Year + " " + 
            DateTime.Now.Hour + ":" + DateTime.Now.Minute + ":" + DateTime.Now.Second + " GMT"*/; //Tue, 21 Apr 2015 22:51:19 GMT
        public string modified = DateTime.Now.ToString();
        public int contentLength = 0;
        public string contentType = "text/html";
        public string data = "<body>i am empty :(</body>";

        public List<string> additionalHEAD = new List<string>();
        public List<string> valuesHEAD = new List<string>();
        public List<string> additionalPOST = new List<string>();
        public List<string> valuesPOST = new List<string>();

        public bool short_ = true;
        public HTTP_Type type;

        public static Dictionary<EndPoint, string> unfinishedPackets = new Dictionary<EndPoint, string>();

        public string getPackage()
        {
            string ret = "";

            ret += version + " " + status + "\r\n";
            ret += "Host: localhost\r\n";
            ret += "Date: " + date + "\r\n"; //do we need that?!
            ret += "Server: LamestWebserver (LameOS)\r\n";
            
            //ret += "Last-Modified: " + modified + "\r\n"; //do we need that?!
            ret += "Content-Type: text/html; charset=UTF-8\r\n";//"Content-Length: " + contentLenght + "\r\n";
            ret += "Content-Length: " + contentLength + (short_?"\r\n\r\n":"\r\n\r\n\r\n");
            //ret += "Keep-Alive: timeout=10, max=100\r\n";
            //ret += "Connection: Keep-Alive\r\n";
            //ret += "Content-Type: " + contentType + "; charset=UTF-8\r\n\r\n";
            ret += data;

            return ret;
        }

        public HTTP_Packet()
        {
            //default constructor
            date = DateTime.Now.DayOfWeek.ToString().Substring(0,3) + ", " + DateTime.Now.Day + " " + Months[DateTime.Now.Month] + " " + DateTime.Now.Year + " " + 
                DateTime.Now.Hour + ":" + DateTime.Now.Minute + ":" + DateTime.Now.Second + " GMT"; //Tue, 21 Apr 2015 22:51:19 GMT
        }


        public static HTTP_Packet Constructor(string input, EndPoint endp)
        {
            HTTP_Packet h = new HTTP_Packet();

            /*List<string>*/
            string[] linput = null;
            //new List<string>();
            
            /*int lindex = 0;

            for (int i = 0; i < input.Length - 1; i++)
            {
                if(input.Substring(i,2) == "\r\n" || i + 1 >= input.Length)
                {
                    if(i - lindex - 1 > 0)
                    {
                        linput.Add(input.Substring(lindex, i - lindex));
                    }
                    lindex = i + 2;
                }
            }*/

            linput = input.Replace("\0", "").Split(new string[] { "\r\n" }, StringSplitOptions.None);

            bool found = false;

            for (int i = 0; i < linput.Length; i++)
            {

                if(linput[i].Substring(0,"GET ".Length) == "GET ")
                {
                    h.type = HTTP_Type.GET;
                    int index = 4;

                    for (int j = 4; j < linput[i].Length; j++)
                    {
                        if (linput[i][j] == ' ')
                        {
                            index = j;
                            break;
                        }
                    }

                    h.data = linput[i].Substring(4, index - 4);

                    for (int k = 0; k < h.data.Length - 1; k++)
                    {
                        if (h.data[k] == '?')
                        {
                            string add = h.data.Substring(k + 1);

                            if (add[add.Length - 1] == ' ')
                                add = add.Remove(add.Length - 1);

                            h.data = h.data.Remove(k);
                            add = add.Replace("%20", " ").Replace("%22", "\"");

                            for(int it = 0; it < add.Length - 1; it++)
                            {
                                if(add[it] == '&')
                                {
                                    h.additionalHEAD.Add(add.Substring(0, it));
                                    h.valuesHEAD.Add("");
                                    add = add.Remove(0, it + 1);
                                    it = 0;
                                }
                            }

                            h.additionalHEAD.Add(add);
                            h.valuesHEAD.Add("");
                        }
                    }

                    for (int j = 0; j < h.additionalHEAD.Count; j++)
                    {
                        for (int k = 0; k < h.additionalHEAD[j].Length; k++)
                        {
                            if (h.additionalHEAD[j][k] == '=')
                            {
                                if (k + 1 < h.additionalHEAD[j].Length)
                                {
                                    h.valuesHEAD[j] = h.additionalHEAD[j].Substring(k + 1);
                                    h.additionalHEAD[j] = h.additionalHEAD[j].Substring(0, k);
                                }
                            }
                        }

                        if(h.additionalHEAD[i][h.additionalHEAD[i].Length - 1] == '=')
                        {
                            h.additionalHEAD[i].Remove(h.additionalHEAD[i].Length - 1);
                        }
                    }

                    h.version = linput[i].Substring(index + 1);
                    found = true;

                    return h;
                }
                else if(linput[i].Substring(0, "POST ".Length) == "POST ")
                {
                    h.type = HTTP_Type.POST;
                    int index = 5;

                    for (int j = 5; j < linput[i].Length; j++)
                    {
                        if (linput[i][j] == ' ')
                        {
                            index = j;
                            break;
                        }
                    }

                    h.data = linput[i].Substring(4, index - 4);

                    for (int k = 0; k < h.data.Length - 1; k++)
                    {
                        if (h.data[k] == '?')
                        {
                            string add = h.data.Substring(k + 1);

                            if (add[add.Length - 1] == ' ')
                                add = add.Remove(add.Length - 1);

                            h.data = h.data.Remove(k);
                            add = add.Replace("%20", " ").Replace("%22", "\"");

                            for (int it = 0; it < add.Length - 1; it++)
                            {
                                if (add[it] == '&')
                                {
                                    h.additionalHEAD.Add(add.Substring(0, it));
                                    h.valuesHEAD.Add("");
                                    add = add.Remove(0, it + 1);
                                    it = 0;
                                }
                            }

                            h.additionalHEAD.Add(add);
                            h.valuesHEAD.Add("");
                        }
                    }

                    for (int j = 0; j < h.additionalHEAD.Count; j++)
                    {
                        for (int k = 0; k < h.additionalHEAD[j].Length; k++)
                        {
                            if(h.additionalHEAD[j][k] == '=')
                            {
                                if (k + 1 < h.additionalHEAD[j].Length)
                                {
                                    h.valuesHEAD[j] = h.additionalHEAD[j].Substring(k + 1);
                                    h.additionalHEAD[j] = h.additionalHEAD[j].Substring(0, k);
                                }
                            }
                        }

                        if (h.additionalHEAD[i][h.additionalHEAD[i].Length - 1] == '=')
                        {
                            h.additionalHEAD[i].Remove(h.additionalHEAD[i].Length - 1);
                        }
                    }

                    h.version = linput[i].Substring(index + 1);
                    found = true;

                    for (int j = i; j < linput.Length; j++)
                    {
                        if(j > 0)
                        {
                            if(linput[j] != "" && linput[j-1] == "")
                            {
                                for (int k = j; k < linput.Length; k++)
                                {
                                    string[] s = linput[k].Replace("%20", " ").Replace("%22", "\"").Split('&');

                                    for (int l = 0; l < s.Length; l++)
                                    {
                                        h.valuesPOST.Add("");
                                    }

                                    h.additionalPOST.AddRange(s);
                                }

                                goto SEARCHINGFORPOSTBODY_DONE;
                            }
                        }
                    }

                    SEARCHINGFORPOSTBODY_DONE:

                    for (int j = 0; j < h.additionalPOST.Count; j++)
                    {
                        for (int k = 0; k < h.additionalPOST[j].Length; k++)
                        {
                            if (h.additionalPOST[j][k] == '=')
                            {
                                if (k + 1 < h.additionalPOST[j].Length)
                                {
                                    h.valuesPOST[j] = h.additionalPOST[j].Substring(k + 1);
                                    h.additionalPOST[j] = h.additionalPOST[j].Substring(0, k);
                                }
                            }
                        }
                    }

                    // Chris: Crazy hack for Chrome POST packets
                    if(h.additionalPOST.Count == 0)
                    {
                        // Chris: is there a content-length?
                        bool contlfound = false;

                        // Chris: search for it
                        for (int j = i + 1; j < linput.Length; j++)
                        {
                            if(linput[j].Substring(0, 16) == "Content-Length: ")
                            {
                                if(int.TryParse(linput[j].Substring(16), out h.contentLength))
                                    contlfound = true;
                                break;
                            }
                        }

                        if(contlfound && h.contentLength > 0 && linput[linput.Length - 1] == "" && linput[linput.Length - 2] == "")
                        {
                            unfinishedPackets.Add(endp, input);
                            return new HTTP_Packet() { version = "POST_PACKET_INCOMING" };
                        }
                    }

                    return h;
                }
            }

            if (!found)
            {
                // Chris: Crazy chrome POST hack
                // Chris: Resolve Endpoint and append input to that input, then delete

                string oldinput = unfinishedPackets[endp];

                if (oldinput != null)
                {
                    unfinishedPackets.Remove(endp);
                    oldinput += input;
                }

                return Constructor(oldinput, endp);
            }

            return h;
        }
    }

    public enum HTTP_Type
    {
        GET, POST
    }
}
