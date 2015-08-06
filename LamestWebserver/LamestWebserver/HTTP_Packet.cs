using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LamestWebserver
{
    public class HTTP_Packet
    {
        public List<string> Months = new List<string>(){ "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"};
        public string version = "HTTP/1.1";
        public string status = "200 OK";
        public string date/* = DateTime.Now.DayOfWeek.ToString().Substring(0,3) + ", " + DateTime.Now.Day + " " + Months[DateTime.Now.Month] + " " + DateTime.Now.Year + " " + 
            DateTime.Now.Hour + ":" + DateTime.Now.Minute + ":" + DateTime.Now.Second + " GMT"*/; //Tue, 21 Apr 2015 22:51:19 GMT
        public string modified = DateTime.Now.ToString();
        public int contentLength = 0;
        public string contentType = "text/html";
        public string data = "<body>i am empty :(</body>";
        public List<string> additional = new List<string>();
        public bool short_ = false;


        public string getPackage()
        {
            string ret = "";

            ret += version + " " + status + "\r\n";
            ret += "Host: localhost\r\n";
            ret += "Date: " + date + "\r\n"; //do we need that?!
            ret += "Server: LamestWebserver (LamOS)\r\n";
            
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
            List<string> linput = new List<string>();
            int lindex = 0;

            for (int i = 0; i < input.Length - 1; i++)
            {
                if(input.Substring(i,2) == "\r\n")
                {
                    if(i - lindex - 1 > 0)
                    {
                        linput.Add(input.Substring(lindex, i - lindex));
                    }
                    lindex = i + 2;
                }
            }

            bool found = false;

            for (int i = 0; i < linput.Count; i++)
            {

                if(linput[i].Substring(0,"GET ".Length) == "GET ")
                {
                    int index = 4;

                    for(int j = 4; j < linput[i].Length; j++)
                    {
                        if(linput[i][j] == ' ')
                        {
                            index = j;
                            break;
                        }
                    }

                    data = linput[i].Substring(3, index - 3);

                    for (int k = 0; k < data.Length - 1; k++)
                    {
                        if(data[k] == '?')
                        {
                            string add = data.Substring(k + 1);
                            data = data.Remove(k);
                            add = add.Replace("%20", " ").Replace("%22", "\"");

                            for(int it = 0; it < add.Length - 1; it++)
                            {
                                if(add[it] == '&')
                                {
                                    additional.Add(add.Substring(0, it));
                                    add = add.Remove(0, it + 1);
                                }
                            }

                            additional.Add(add);
                        }
                    }

                    version = linput[i].Substring(index + 1);
                    found = true;

                    return;
                }
            }

            if (!found)
                version = "";
        }
    }
}
