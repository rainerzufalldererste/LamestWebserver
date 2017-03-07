using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Web;

namespace LamestWebserver
{
    /// <summary>
    /// Represents a decoded HTTP Packet or is used for packing data into a HTTP Packet for sending
    /// </summary>
    public class HttpPacket
    {
        /// <summary>
        /// An expression used for DateTime.ToString to parse into correct HTTP DateFormat
        /// </summary>
        public const string HtmlDateFormat = "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'";
        
        /// <summary>
        /// The HTTP Version of the Response
        /// </summary>
        public string Version = "HTTP/1.1";

        /// <summary>
        /// The HTTP Status Code and Status of the Response
        /// </summary>
        public string Status = "200 OK";

        /// <summary>
        /// The current date in HTTP DateFormat
        /// </summary>
        public string Date; //Tue, 21 Apr 2015 22:51:19 GMT

        /// <summary>
        /// if the request packet contains a modified date it is contained in here
        /// </summary>
        public DateTime? ModifiedDate = null;

        private int _contentLength = 0;

        /// <summary>
        /// The content-type of the response
        /// </summary>
        public string ContentType = "text/html";

        /// <summary>
        /// the binary data contained in the request
        /// Also sets the contentLength.
        /// </summary>
        public byte[] BinaryData { get { return _binaryData; } set { _binaryData = value; _contentLength = _binaryData.Length; } }
        private byte[] _binaryData;

        /// <summary>
        /// The contents of the request package
        /// </summary>
        public string RequestUrl = "";

        /// <summary>
        /// The cookies, that were set in the request or shall be set in the response
        /// </summary>
        public List<KeyValuePair<string, string>> Cookies = null;

        /// <summary>
        /// the host attribute of the http package
        /// </summary>
        public string Host = "localhost";

        /// <summary>
        /// HEAD variables set or mentioned in the request
        /// </summary>
        public List<string> VariablesHEAD = new List<string>();

        /// <summary>
        /// the values of the set HEAD values
        /// </summary>
        public List<string> ValuesHEAD = new List<string>();
        
        /// <summary>
        /// POST variables set or mentioned in the request
        /// </summary>
        public List<string> VariablesPOST = new List<string>();
        
        /// <summary>
        /// the values of the set HEAD values
        /// </summary>
        public List<string> ValuesPOST = new List<string>();

        /// <summary>
        /// Is the sent package a upgradeRequest to a WebSocket?
        /// </summary>
        public bool IsWebsocketUpgradeRequest = false;

        /// <summary>
        /// the HTTP type of the request (GET, POST)
        /// </summary>
        public HttpType HttpType;

        /// <summary>
        /// Retrieves the raw request code.
        /// </summary>
        public string RawRequest { get; protected set; } = null;

        /// <summary>
        /// The current stream which is used for communicating.
        /// </summary>
        public Stream Stream;

        /// <summary>
        /// returns the contents of the complete package to be sent via tcp to the client 
        /// </summary>
        /// <param name="enc">a UTF8Encoding</param>
        /// <returns>the contents as byte array</returns>
        public byte[] GetPackage(UTF8Encoding enc)
        {
            StringBuilder sb = new StringBuilder();
            
            sb.Append(Version + " " + Status + "\r\n");
            sb.Append("Host: " + Host + "\r\n");
            sb.Append("Date: " + Date + "\r\n");
            sb.Append("Server: LamestWebserver (LameOS)\r\n");

            if (Cookies != null && Cookies.Count > 0)
            {
                sb.Append("Set-Cookie: ");

                for (int i = 0; i < Cookies.Count; i++)
                {
                    sb.Append(Cookies[i].Key + "=" + Cookies[i].Value + "; Path=/");

                    if (i + 1 < Cookies.Count)
                        sb.Append("\n");
                    else
                        sb.Append("\r\n");
                }
            }

            if(ModifiedDate.HasValue)
            {
                sb.Append("Last-Modified: " + ModifiedDate.Value.ToString(HtmlDateFormat) + "\r\n");
            }

            sb.Append("Connection: Keep-Alive\r\n");

            if (ContentType != null)
                sb.Append("Content-Type: " + ContentType + "; charset=UTF-8\r\n");

            sb.Append("Content-Length: " + _contentLength + "\r\n\r\n");

            byte[] ret0 = enc.GetBytes(sb.ToString());
            byte[] ret = new byte[ret0.Length + BinaryData.Length];

            Array.Copy(ret0, ret, ret0.Length);
            Array.Copy(BinaryData, 0, ret, ret0.Length, BinaryData.Length);

            return ret;
        }

        /// <summary>
        /// the default constructor for a HTTP Response
        /// </summary>
        public HttpPacket()
        {
            Date = DateTime.Now.ToString(HtmlDateFormat);
        }

        /// <summary>
        /// The default constructor for a HTTP Request from string.
        /// if the version is null then please ignore the packet and wait for the next one to contain the POST values. this method will automatically stitch these packets together.
        /// </summary>
        /// <param name="input">the packet from the client decoded to string</param>
        /// <param name="endp">the ipendpoint of the client for strange chrome POST hacks</param>
        /// <param name="lastPacket">the string contents of the last packet (Chrome POST packets are split in two packets)</param>
        /// <param name="stream">the stream at which the packet arrived (only used for sessionData)</param>
        /// <returns>the corresponding HTTP Packet</returns>
        public static HttpPacket Constructor(ref string input, EndPoint endp, string lastPacket, Stream stream)
        {
            HttpPacket h = new HttpPacket();

            h.RawRequest = input;
            h.Stream = stream;
            
            string[] linput = null;

            linput = input.Split(new string[] { "\r\n" }, StringSplitOptions.None);

            bool found = false;

            for (int i = 0; i < linput.Length; i++)
            {

                if(linput[i].Length > 4 && linput[i].Substring(0,"GET ".Length) == "GET ")
                {
                    h.HttpType = HttpType.Get;
                    int index = 4;

                    for (int j = 4; j < linput[i].Length; j++)
                    {
                        if (linput[i][j] == ' ')
                        {
                            index = j;
                            break;
                        }
                    }

                    h.RequestUrl = linput[i].Substring(4, index - 4);

                    for (int k = 0; k < h.RequestUrl.Length - 1; k++)
                    {
                        if (h.RequestUrl[k] == '?')
                        {
                            string add = h.RequestUrl.Substring(k + 1);

                            if (add[add.Length - 1] == ' ')
                                add = add.Remove(add.Length - 1);

                            h.RequestUrl = h.RequestUrl.Remove(k);
                            add = add.Replace('+', ' ');

                            for (int it = 0; it < add.Length - 1; it++)
                            {
                                if(add[it] == '&')
                                {
                                    h.VariablesHEAD.Add(add.Substring(0, it));
                                    h.ValuesHEAD.Add("");
                                    add = add.Remove(0, it + 1);
                                    it = 0;
                                }
                            }

                            h.VariablesHEAD.Add(add);
                            h.ValuesHEAD.Add("");
                        }
                    }

                    for (int j = 0; j < h.VariablesHEAD.Count; j++)
                    {
                        for (int k = 0; k < h.VariablesHEAD[j].Length; k++)
                        {
                            if (h.VariablesHEAD[j][k] == '=')
                            {
                                if (k + 1 < h.VariablesHEAD[j].Length)
                                {
                                    h.ValuesHEAD[j] = h.VariablesHEAD[j].Substring(k + 1);
                                    h.VariablesHEAD[j] = h.VariablesHEAD[j].Substring(0, k);
                                }
                            }
                        }

                        if(h.VariablesHEAD[j][h.VariablesHEAD[j].Length - 1] == '=' || h.VariablesHEAD[j][h.VariablesHEAD[j].Length - 1] == '&')
                        {
                            h.VariablesHEAD[j] = h.VariablesHEAD[j].Remove(h.VariablesHEAD[j].Length - 1);
                        }

                        h.ValuesHEAD[j] = HttpUtility.UrlDecode(h.ValuesHEAD[j]);
                        h.VariablesHEAD[j] = HttpUtility.UrlDecode(h.VariablesHEAD[j]);
                    }

                    h.Version = linput[i].Substring(index + 1);
                    found = true;

                    return GetCookiesAndModified(h, linput);
                }
                else if(linput[i].StartsWith("POST "))
                {
                    h.HttpType = HttpType.Post;
                    int index = 5;

                    for (int j = 5; j < linput[i].Length; j++)
                    {
                        if (linput[i][j] == ' ')
                        {
                            index = j;
                            break;
                        }
                    }

                    h.RequestUrl = linput[i].Substring(4, index - 4);

                    for (int k = 0; k < h.RequestUrl.Length - 1; k++)
                    {
                        if (h.RequestUrl[k] == '?')
                        {
                            string add = h.RequestUrl.Substring(k + 1);

                            if (add[add.Length - 1] == ' ')
                                add = add.Remove(add.Length - 1);

                            h.RequestUrl = h.RequestUrl.Remove(k);
                            add = add.Replace('+', ' ');

                            for (int it = 0; it < add.Length - 1; it++)
                            {
                                if (add[it] == '&')
                                {
                                    h.VariablesHEAD.Add(add.Substring(0, it));
                                    h.ValuesHEAD.Add("");
                                    add = add.Remove(0, it + 1);
                                    it = 0;
                                }
                            }

                            h.VariablesHEAD.Add(add);
                            h.ValuesHEAD.Add("");
                        }
                    }

                    for (int j = 0; j < h.VariablesHEAD.Count; j++)
                    {
                        for (int k = 0; k < h.VariablesHEAD[j].Length; k++)
                        {
                            if(h.VariablesHEAD[j][k] == '=')
                            {
                                if (k + 1 < h.VariablesHEAD[j].Length)
                                {
                                    h.ValuesHEAD[j] = h.VariablesHEAD[j].Substring(k + 1);
                                    h.VariablesHEAD[j] = h.VariablesHEAD[j].Substring(0, k);
                                }
                            }
                        }

                        if (h.VariablesHEAD[j][h.VariablesHEAD[j].Length - 1] == '=' || h.VariablesHEAD[j][h.VariablesHEAD[j].Length - 1] == '&')
                        {
                            h.VariablesHEAD[j] = h.VariablesHEAD[j].Remove(h.VariablesHEAD[j].Length - 1);
                        }

                        h.ValuesHEAD[j] = HttpUtility.UrlDecode(h.ValuesHEAD[j]);
                        h.VariablesHEAD[j] = HttpUtility.UrlDecode(h.VariablesHEAD[j]);
                    }

                    h.Version = linput[i].Substring(index + 1);
                    found = true;

                    for (int j = i; j < linput.Length; j++)
                    {
                        if(j > 0)
                        {
                            if(linput[j] != "" && linput[j-1] == "")
                            {
                                for (int k = j; k < linput.Length; k++)
                                {
                                    string[] s = linput[k].Replace('+',' ').Split('&');

                                    for (int l = 0; l < s.Length; l++)
                                    {
                                        h.ValuesPOST.Add("");
                                    }

                                    h.VariablesPOST.AddRange(s);
                                }

                                goto SEARCHINGFORPOSTBODY_DONE;
                            }
                        }
                    }

                    SEARCHINGFORPOSTBODY_DONE:

                    for (int j = 0; j < h.VariablesPOST.Count; j++)
                    {
                        for (int k = 0; k < h.VariablesPOST[j].Length; k++)
                        {
                            if (h.VariablesPOST[j][k] == '=')
                            {
                                if (k + 1 < h.VariablesPOST[j].Length)
                                {
                                    h.ValuesPOST[j] = h.VariablesPOST[j].Substring(k + 1);
                                    h.VariablesPOST[j] = h.VariablesPOST[j].Substring(0, k);
                                }
                            }
                        }

                        if (h.VariablesPOST[j][h.VariablesPOST[j].Length - 1] == '=' || h.VariablesPOST[j][h.VariablesPOST[j].Length - 1] == '&')
                        {
                            h.VariablesPOST[j] = h.VariablesPOST[j].Remove(h.VariablesPOST[j].Length - 1);
                        }

                        h.ValuesPOST[j] = HttpUtility.UrlDecode(h.ValuesPOST[j]);
                        h.VariablesPOST[j] = HttpUtility.UrlDecode(h.VariablesPOST[j]);
                    }

                    // Crazy hack for Chrome POST packets
                    if(h.VariablesPOST.Count == 0)
                    {
                        // is there a content-length?
                        bool contlfound = false;

                        // search for it
                        for (int j = i + 1; j < linput.Length; j++)
                        {
                            if(linput[j].Length >= 16 && linput[j].Substring(0, 16) == "Content-Length: ")
                            {
                                if(int.TryParse(linput[j].Substring(16), out h._contentLength))
                                    contlfound = true;
                                break;
                            }
                        }

                        if(contlfound && h._contentLength > 0 && linput[linput.Length - 1] == "" && linput[linput.Length - 2] == "")
                        {
                            return new HttpPacket() { Version = null };
                        }
                    }

                    return GetCookiesAndModified(h, linput);
                }
            }

            if (!found)
            {
                // Crazy chrome POST hack
                // Resolve Endpoint and append input to that input, then delete

                if (lastPacket != null)
                    input = lastPacket + input;
                else
                    return GetCookiesAndModified(h, linput);

                return Constructor(ref input, endp, null, stream);
            }

            return h;
        }

        private const string ifmodifiedsince = "If-Modified-Since: ",
            cookie = "Cookie: ";

        private static HttpPacket GetCookiesAndModified(HttpPacket packet, string[] linput)
        {
            for (int i = 0; i < linput.Length; i++)
            {
                if(!packet.ModifiedDate.HasValue && linput[i].Length > ifmodifiedsince.Length && linput[i].Substring(0, ifmodifiedsince.Length) == ifmodifiedsince)
                {
                    DateTime mod;

                    if (DateTime.TryParse(linput[i].Substring(ifmodifiedsince.Length), out mod))
                        packet.ModifiedDate = mod;
                }
                else if(packet.Cookies == null && linput[i].Length > cookie.Length && linput[i].Substring(0, cookie.Length) == cookie)
                {
                    List<KeyValuePair<string, string>> cookies = new List<KeyValuePair<string, string>>();

                    string[] pairs = linput[i].Substring(cookie.Length).Split(';');

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

                    packet.Cookies = cookies;
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
    public enum HttpType
    {
        /// <summary>
        /// A GET Request
        /// </summary>
        Get,

        /// <summary>
        /// A POST Request containing values
        /// </summary>
        Post
    }
}