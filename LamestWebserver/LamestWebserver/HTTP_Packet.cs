﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Web;

namespace LamestWebserver
{
    public class HTTP_Packet
    {
        public const string htmldateformat = "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'";

        public string[] Months = new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"};
        public string version = "HTTP/1.1";
        public string status = "200 OK";
        public string date/* = DateTime.Now.DayOfWeek.ToString().Substring(0,3) + ", " + DateTime.Now.Day + " " + Months[DateTime.Now.Month] + " " + DateTime.Now.Year + " " + 
            DateTime.Now.Hour + ":" + DateTime.Now.Minute + ":" + DateTime.Now.Second + " GMT"*/; //Tue, 21 Apr 2015 22:51:19 GMT
        public DateTime? modified = null;
        public int contentLength = 0;
        public string contentType = "text/html";
        public string data = "<body>i am empty :(</body>";
        public List<KeyValuePair<string, string>> cookie = null;
        public string host = "localhost";

        public List<string> additionalHEAD = new List<string>();
        public List<string> valuesHEAD = new List<string>();
        public List<string> additionalPOST = new List<string>();
        public List<string> valuesPOST = new List<string>();

        public const bool short_ = true;
        public HTTP_Type type;

        public static Dictionary<EndPoint, string> unfinishedPackets = new Dictionary<EndPoint, string>();

        public string getPackage()
        {
            string ret = "";

            ret += version + " " + status + "\r\n";
            ret += "Host: " + host + "\r\n";
            ret += "Date: " + date + "\r\n"; //do we need that?!
            ret += "Server: LamestWebserver (LameOS)\r\n";

            if (cookie != null)
            {
                ret += "Set-Cookie: ";

                for (int i = 0; i < cookie.Count; i++)
                {
                    ret += cookie[i].Key + "= " + cookie[i].Value + "; path=/; secure; HttpOnly";

                    if (i + 1 < cookie.Count)
                        ret += "\n";
                    else
                        ret += "\r\n";
                }
            }

            if(modified.HasValue)
            {
                ret += "Last-Modified: " + modified.Value.ToString(htmldateformat) + "\r\n";
            }

            ret += "Connection: Keep-Alive\r\n";

            ret += "Content-Type: " + contentType + "; charset=UTF-16\r\n";//"Content-Length: " + contentLenght + "\r\n";
            ret += "Content-Length: " + contentLength + (short_?"\r\n\r\n":"\r\n\r\n\r\n");
            //ret += "Keep-Alive: timeout=10, max=100\r\n";
            //ret += "Content-Type: " + contentType + "; charset=UTF-8\r\n\r\n";
            ret += data;

            return ret;
        }

        public HTTP_Packet()
        {
            //default constructor
            //date = DateTime.Now.DayOfWeek.ToString().Substring(0,3) + ", " + DateTime.Now.Day + " " + Months[DateTime.Now.Month] + " " + DateTime.Now.Year + " " + 
            //    DateTime.Now.Hour + ":" + DateTime.Now.Minute + ":" + DateTime.Now.Second + " GMT"; //Tue, 21 Apr 2015 22:51:19 GMT

            date = DateTime.Now.ToString(htmldateformat);
        }


        public static HTTP_Packet Constructor(ref string input, EndPoint endp)
        {
            HTTP_Packet h = new HTTP_Packet();
            
            string[] linput = null;

            linput = input.Split(new string[] { "\r\n" }, StringSplitOptions.None);

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
                            add = add.Replace('+', ' '); // HttpUtility.HtmlDecode(linput[k]);

                            for (int it = 0; it < add.Length - 1; it++)
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

                        if(h.additionalHEAD[j][h.additionalHEAD[j].Length - 1] == '=')
                        {
                            h.additionalHEAD[j] = h.additionalHEAD[j].Remove(h.additionalHEAD[j].Length - 1);
                        }

                        h.valuesHEAD[j] = HttpUtility.UrlDecode(h.valuesHEAD[j]);
                        h.additionalHEAD[j] = HttpUtility.UrlDecode(h.additionalHEAD[j]);
                    }

                    h.version = linput[i].Substring(index + 1);
                    found = true;

                    return getCookiesAndModified(h, linput);
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
                            add = add.Replace('+', ' '); //HttpUtility.HtmlDecode(linput[k]);

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

                        if (h.additionalHEAD[j][h.additionalHEAD[j].Length - 1] == '=')
                        {
                            h.additionalHEAD[j] = h.additionalHEAD[j].Remove(h.additionalHEAD[j].Length - 1);
                        }

                        h.valuesHEAD[j] = HttpUtility.UrlDecode(h.valuesHEAD[j]);
                        h.additionalHEAD[j] = HttpUtility.UrlDecode(h.additionalHEAD[j]);
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
                                    string[] s = /*HttpUtility.HtmlDecode(*/linput[k].Replace('+',' ')/*)*/.Split('&');

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

                        if (h.additionalPOST[j][h.additionalPOST[j].Length - 1] == '=')
                        {
                            h.additionalPOST[j] = h.additionalPOST[j].Remove(h.additionalPOST[j].Length - 1);
                        }

                        h.valuesPOST[j] = HttpUtility.UrlDecode(h.valuesPOST[j]);
                        h.additionalPOST[j] = HttpUtility.UrlDecode(h.additionalPOST[j]);
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

                    return getCookiesAndModified(h, linput);
                }
            }

            if (!found)
            {
                // Chris: Crazy chrome POST hack
                // Chris: Resolve Endpoint and append input to that input, then delete

                System.Threading.Thread.Sleep(2);

                string oldinput = unfinishedPackets[endp];

                if (oldinput != null)
                {
                    unfinishedPackets.Remove(endp);
                    input = oldinput + input;
                }

                return Constructor(ref input, endp);
            }

            return h;
        }

        const string ifmodifiedsince = "If-Modified-Since: ",
            cookie_ = "Cookie: "/*,
            ifunmodifiedsince = "If-Unmodified-Since: "*/;

        private static HTTP_Packet getCookiesAndModified(HTTP_Packet packet, string[] linput)
        {
            for (int i = 0; i < linput.Length; i++)
            {
                if(!packet.modified.HasValue && linput[i].Length > ifmodifiedsince.Length && linput[i].Substring(0, ifmodifiedsince.Length) == ifmodifiedsince)
                {
                    DateTime mod;

                    if (DateTime.TryParse(linput[i].Substring(ifmodifiedsince.Length), out mod))
                        packet.modified = mod;
                }
                else if(packet.cookie == null && linput[i].Length > cookie_.Length && linput[i].Substring(0, cookie_.Length) == cookie_)
                {
                    List<KeyValuePair<string, string>> cookies = new List<KeyValuePair<string, string>>();

                    string[] pairs = linput[i].Substring(cookie_.Length).Split(';');

                    for (int j = 0; j < pairs.Length; j++)
                    {
                        pairs[j] = pairs[j].Trim();

                        if (pairs[j].Contains("="))
                        {
                            for (int k = 0; k < pairs[j].Length; k++)
                            {
                                if (pairs[j][k] == '=')
                                {
                                    if (k < pairs[j].Length - 1)
                                    {
                                        cookies.Add(new KeyValuePair<string, string>(pairs[j].Substring(0, k).Trim(), pairs[j].Substring(k + 1).Trim()));
                                    }
                                    else
                                    {
                                        cookies.Add(new KeyValuePair<string, string>(pairs[j].Substring(0, k).Trim(), ""));
                                    }

                                    break;
                                }
                            }
                        }
                        else
                        {
                            cookies.Add(new KeyValuePair<string, string>(pairs[j], ""));
                        }
                    }

                    packet.cookie = cookies;
                }
            }

            return packet;
        }
    }

    public enum HTTP_Type
    {
        GET, POST
    }
}