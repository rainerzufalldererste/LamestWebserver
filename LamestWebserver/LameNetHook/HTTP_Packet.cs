using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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


        public HTTP_Packet(string input)
        {
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
                    type = HTTP_Type.GET;
                    int index = 4;

                    for (int j = 4; j < linput[i].Length; j++)
                    {
                        if (linput[i][j] == ' ')
                        {
                            index = j;
                            break;
                        }
                    }

                    data = linput[i].Substring(4, index - 4);

                    for (int k = 0; k < data.Length - 1; k++)
                    {
                        if (data[k] == '?')
                        {
                            string add = data.Substring(k + 1);

                            if (add[add.Length - 1] == ' ')
                                add = add.Remove(add.Length - 1);

                            data = data.Remove(k);
                            add = add.Replace("%20", " ").Replace("%22", "\"");

                            for(int it = 0; it < add.Length - 1; it++)
                            {
                                if(add[it] == '&')
                                {
                                    additionalHEAD.Add(add.Substring(0, it));
                                    valuesHEAD.Add("");
                                    add = add.Remove(0, it + 1);
                                    it = 0;
                                }
                            }

                            additionalHEAD.Add(add);
                            valuesHEAD.Add("");
                        }
                    }

                    for (int j = 0; j < additionalHEAD.Count; j++)
                    {
                        for (int k = 0; k < additionalHEAD[j].Length; k++)
                        {
                            if (additionalHEAD[j][k] == '=')
                            {
                                if (k + 1 < additionalHEAD[j].Length)
                                {
                                    valuesHEAD[j] = additionalHEAD[j].Substring(k + 1);
                                    additionalHEAD[j] = additionalHEAD[j].Substring(0, k);
                                }
                            }
                        }

                        if(additionalHEAD[i][additionalHEAD[i].Length - 1] == '=')
                        {
                            additionalHEAD[i].Remove(additionalHEAD[i].Length - 1);
                        }
                    }

                    version = linput[i].Substring(index + 1);
                    found = true;

                    return;
                }
                else if(linput[i].Substring(0, "POST ".Length) == "POST ")
                {
                    type = HTTP_Type.POST;
                    int index = 5;

                    for (int j = 5; j < linput[i].Length; j++)
                    {
                        if (linput[i][j] == ' ')
                        {
                            index = j;
                            break;
                        }
                    }

                    data = linput[i].Substring(4, index - 4);

                    for (int k = 0; k < data.Length - 1; k++)
                    {
                        if (data[k] == '?')
                        {
                            string add = data.Substring(k + 1);

                            if (add[add.Length - 1] == ' ')
                                add = add.Remove(add.Length - 1);

                            data = data.Remove(k);
                            add = add.Replace("%20", " ").Replace("%22", "\"");

                            for (int it = 0; it < add.Length - 1; it++)
                            {
                                if (add[it] == '&')
                                {
                                    additionalHEAD.Add(add.Substring(0, it));
                                    valuesHEAD.Add("");
                                    add = add.Remove(0, it + 1);
                                    it = 0;
                                }
                            }

                            additionalHEAD.Add(add);
                            valuesHEAD.Add("");
                        }
                    }

                    for (int j = 0; j < additionalHEAD.Count; j++)
                    {
                        for (int k = 0; k < additionalHEAD[j].Length; k++)
                        {
                            if(additionalHEAD[j][k] == '=')
                            {
                                if (k + 1 < additionalHEAD[j].Length)
                                {
                                    valuesHEAD[j] = additionalHEAD[j].Substring(k + 1);
                                    additionalHEAD[j] = additionalHEAD[j].Substring(0, k);
                                }
                            }
                        }

                        if (additionalHEAD[i][additionalHEAD[i].Length - 1] == '=')
                        {
                            additionalHEAD[i].Remove(additionalHEAD[i].Length - 1);
                        }
                    }

                    version = linput[i].Substring(index + 1);
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
                                        valuesPOST.Add("");
                                    }

                                    additionalPOST.AddRange(s);
                                }

                                goto SEARCHINGFORPOSTBODY_DONE;
                            }
                        }
                    }

                    SEARCHINGFORPOSTBODY_DONE:

                    for (int j = 0; j < additionalPOST.Count; j++)
                    {
                        for (int k = 0; k < additionalPOST[j].Length; k++)
                        {
                            if (additionalPOST[j][k] == '=')
                            {
                                if (k + 1 < additionalPOST[j].Length)
                                {
                                    valuesPOST[j] = additionalPOST[j].Substring(k + 1);
                                    additionalPOST[j] = additionalPOST[j].Substring(0, k);
                                }
                            }
                        }
                    }

                    return;
                }
            }

            if (!found)
                version = "";
        }
    }

    public enum HTTP_Type
    {
        GET, POST
    }
}
