using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Web;

namespace LamestWebserver
{
    /// <summary>
    /// Represents a decoded HTTP Packet or is used for packing data into a HTTP Packet for sending
    /// </summary>
    public class HTTP_Packet
    {
        /// <summary>
        /// An expression used for DateTime.ToString to parse into correct HTTP DateFormat
        /// </summary>
        public const string htmldateformat = "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'";
        
        /// <summary>
        /// The HTTP Version of the Response
        /// </summary>
        public string version = "HTTP/1.1";

        /// <summary>
        /// The HTTP Status Code and Status of the Response
        /// </summary>
        public string status = "200 OK";

        /// <summary>
        /// The current date in HTTP DateFormat
        /// </summary>
        public string date; //Tue, 21 Apr 2015 22:51:19 GMT

        /// <summary>
        /// if the request packet contains a modified date it is contained in here
        /// </summary>
        public DateTime? modified = null;

        private int contentLength = 0;

        /// <summary>
        /// The content-type of the response
        /// </summary>
        public string contentType = "text/html";

        /// <summary>
        /// the binary data contained in the request
        /// Also sets the contentLength.
        /// </summary>
        public byte[] binaryData { get { return _binaryData; } set { _binaryData = value; contentLength = _binaryData.Length; } }
        private byte[] _binaryData;

        /// <summary>
        /// The contents of the request package
        /// </summary>
        public string requestData;

        /// <summary>
        /// The cookies, that were set in the request or shall be set in the response
        /// </summary>
        public List<KeyValuePair<string, string>> cookies = null;

        /// <summary>
        /// the host attribute of the http package
        /// </summary>
        public string host = "localhost";

        /// <summary>
        /// HEAD variables set or mentioned in the request
        /// </summary>
        public List<string> additionalHEAD = new List<string>();

        /// <summary>
        /// the values of the set HEAD values
        /// </summary>
        public List<string> valuesHEAD = new List<string>();
        
        /// <summary>
        /// POST variables set or mentioned in the request
        /// </summary>
        public List<string> additionalPOST = new List<string>();
        
        /// <summary>
        /// the values of the set HEAD values
        /// </summary>
        public List<string> valuesPOST = new List<string>();

        private const bool isShortPackageLineFeeds = true;

        /// <summary>
        /// Is the sent package a upgradeRequest to a WebSocket?
        /// </summary>
        public bool IsWebsocketUpgradeRequest = false;

        /// <summary>
        /// the HTTP type of the request (GET, POST)
        /// </summary>
        public HTTP_Type type;

        /// <summary>
        /// returns the contents of the complete package to be sent via tcp to the client 
        /// </summary>
        /// <param name="enc">a UTF8Encoding</param>
        /// <returns>the contents as byte array</returns>
        public byte[] getPackage(UTF8Encoding enc)
        {
            string rets = "";

            rets += version + " " + status + "\r\n";
            rets += "Host: " + host + "\r\n";
            rets += "Date: " + date + "\r\n"; //do we need that?!
            rets += "Server: LamestWebserver (LameOS)\r\n";

            if (cookies != null)
            {
                rets += "Set-Cookie: ";

                for (int i = 0; i < cookies.Count; i++)
                {
                    rets += cookies[i].Key + "=" + cookies[i].Value + "; Path=/";

                    if (i + 1 < cookies.Count)
                        rets += "\n";
                    else
                        rets += "\r\n";
                }
            }

            if(modified.HasValue)
            {
                rets += "Last-Modified: " + modified.Value.ToString(htmldateformat) + "\r\n";
            }

            rets += "Connection: Keep-Alive\r\n";

            if(contentType != null)
                rets += "Content-Type: " + contentType + "; charset=UTF-8\r\n";

            rets += "Content-Length: " + contentLength + (isShortPackageLineFeeds?"\r\n\r\n":"\r\n\r\n\r\n");
            //ret += "Keep-Alive: timeout=10, max=100\r\n";
            //ret += "Content-Type: " + contentType + "; charset=UTF-8\r\n\r\n";

            byte[] ret0 = enc.GetBytes(rets);
            byte[] ret = new byte[ret0.Length + binaryData.Length];

            Array.Copy(ret0, ret, ret0.Length);
            Array.Copy(binaryData, 0, ret, ret0.Length, binaryData.Length);

            return ret;
        }

        /// <summary>
        /// the default constructor for a HTTP Response
        /// </summary>
        public HTTP_Packet()
        {
            //default constructor
            date = DateTime.Now.ToString(htmldateformat);
        }

        /// <summary>
        /// The default constructor for a HTTP Request from string.
        /// if the version is null then please ignore the packet and wait for the next one to contain the POST values. this method will automatically stitch these packets together.
        /// </summary>
        /// <param name="input">the packet from the client decoded to string</param>
        /// <param name="endp">the ipendpoint of the client for strange chrome POST hacks</param>
        /// <param name="lastPacket">the string contents of the last packet (Chrome POST packets are split in two packets)</param>
        /// <returns>the corresponding HTTP Packet</returns>
        public static HTTP_Packet Constructor(ref string input, EndPoint endp, string lastPacket)
        {
            HTTP_Packet h = new HTTP_Packet();
            
            string[] linput = null;

            linput = input.Split(new string[] { "\r\n" }, StringSplitOptions.None);

            bool found = false;

            for (int i = 0; i < linput.Length; i++)
            {

                if(linput[i].Length > 4 && linput[i].Substring(0,"GET ".Length) == "GET ")
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

                    h.requestData = linput[i].Substring(4, index - 4);

                    for (int k = 0; k < h.requestData.Length - 1; k++)
                    {
                        if (h.requestData[k] == '?')
                        {
                            string add = h.requestData.Substring(k + 1);

                            if (add[add.Length - 1] == ' ')
                                add = add.Remove(add.Length - 1);

                            h.requestData = h.requestData.Remove(k);
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

                    h.requestData = linput[i].Substring(4, index - 4);

                    for (int k = 0; k < h.requestData.Length - 1; k++)
                    {
                        if (h.requestData[k] == '?')
                        {
                            string add = h.requestData.Substring(k + 1);

                            if (add[add.Length - 1] == ' ')
                                add = add.Remove(add.Length - 1);

                            h.requestData = h.requestData.Remove(k);
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
                            if(linput[j].Length >= 16 && linput[j].Substring(0, 16) == "Content-Length: ")
                            {
                                if(int.TryParse(linput[j].Substring(16), out h.contentLength))
                                    contlfound = true;
                                break;
                            }
                        }

                        if(contlfound && h.contentLength > 0 && linput[linput.Length - 1] == "" && linput[linput.Length - 2] == "")
                        {
                            return new HTTP_Packet() { version = null };
                        }
                    }

                    return getCookiesAndModified(h, linput);
                }
            }

            if (!found)
            {
                // Chris: Crazy chrome POST hack
                // Chris: Resolve Endpoint and append input to that input, then delete

                if (lastPacket != null)
                    input = lastPacket + input;
                else
                    return getCookiesAndModified(h, linput);

                return Constructor(ref input, endp, null);
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
                else if(packet.cookies == null && linput[i].Length > cookie_.Length && linput[i].Substring(0, cookie_.Length) == cookie_)
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

                    packet.cookies = cookies;
                }
                else if(!packet.IsWebsocketUpgradeRequest && linput[i] == "Upgrade: websocket")
                {
                    packet.IsWebsocketUpgradeRequest = true;
                }
            }

            return packet;
        }
    }

    /// <summary>
    /// The different kinds of HTTP Requests we allow
    /// </summary>
    public enum HTTP_Type
    {
        /// <summary>
        /// A GET Request
        /// </summary>
        GET,

        /// <summary>
        /// A POST Request containing values
        /// </summary>
        POST
    }
}