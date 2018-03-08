using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Linq;
using System.Web;
using LamestWebserver.Collections;

namespace LamestWebserver
{
    /// <summary>
    /// Represents a decoded HTTP Packet or is used for packing data into a HTTP Packet for sending
    /// </summary>
    public class HttpRequest
    {
        /// <summary>
        /// The HTTP Version of the Request
        /// </summary>
        public string Version = "HTTP/1.1";

        /// <summary>
        /// if the request packet contains a modified date it is contained in here
        /// </summary>
        public DateTime? ModifiedDate = null;

        /// <summary>
        /// The contents of the request package
        /// </summary>
        public string RequestUrl = "";

        /// <summary>
        /// The cookies, that were set in the request
        /// </summary>
        public List<KeyValuePair<string, string>> Cookies = null;

        /// <summary>
        /// HEAD variables set or mentioned in the request
        /// </summary>
        public AVLTree<string, string> VariablesHttpHead = new AVLTree<string, string>();
        
        /// <summary>
        /// POST variables set or mentioned in the request
        /// </summary>
        public AVLTree<string, string> VariablesHttpPost = new AVLTree<string, string>();

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
        /// The current TcpClient which is used for communicating.
        /// </summary>
        public TcpClient TcpClient;

        private int _contentLength = 0;

        /// <summary>
        /// Is true if the browser just sent a request with declared but missing POST information
        /// </summary>
        public bool IsIncompleteRequest = false;

        /// <summary>
        /// describes the range of bytes there are requested
        /// item1 = begin
        /// item2 = end
        /// is null when all bytes are requested
        /// </summary>
        public Tuple<int, int> Range = null;

        private HttpRequest()
        {
        }

        /// <summary>
        /// The default constructor for a HTTP Request from string.
        /// if the version is null then please ignore the packet and wait for the next one to contain the POST values. this method will automatically stitch these packets together.
        /// </summary>
        /// <param name="input">the packet from the client decoded to string</param>
        /// <param name="lastPacket">the string contents of the last packet (Chrome POST packets are split in two packets)</param>
        /// <param name="stream">the stream at which the packet arrived (only used for sessionData)</param>
        /// <returns>the corresponding HTTP Packet</returns>
        public static HttpRequest Constructor(ref string input, string lastPacket, Stream stream)
        {
            HttpRequest h = new HttpRequest();

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

                    h.RequestUrl = Uri.UnescapeDataString(linput[i].Substring(4, index - 4));

                    for (int k = 0; k < h.RequestUrl.Length - 1; k++)
                    {
                        if (h.RequestUrl[k] == '?')
                        {
                            string add = h.RequestUrl.Substring(k + 1);

                            if (add[add.Length - 1] == ' ')
                                add = add.Remove(add.Length - 1);

                            h.RequestUrl = h.RequestUrl.Remove(k);
                            add = add.Replace('+', ' ');

                            for (int it = 0; it < add.Length; it++)
                            {
                                if (add[it] == '&')
                                {
                                    h.VariablesHttpHead.Add(add.Substring(0, it), "");
                                    add = add.Remove(0, it + 1);
                                    it = -1;
                                }
                                else if (add[it] == '#')
                                {
                                    if (it > 0)
                                        h.VariablesHttpHead.Add(add.Substring(0, it), "");

                                    add = "";

                                    break;
                                }
                            }

                            h.VariablesHttpHead.Add(add, "");
                            break;
                        }
                    }

                    h.RequestUrl = h.RequestUrl.TrimEnd('?');

                    var variables = h.VariablesHttpHead.Keys;

                    foreach (var variable in variables)
                    {
                        h.VariablesHttpHead.Remove(variable);

                        string newKey = variable;
                        string newValue = "";
                        
                        if (!newKey.Any())
                            continue;

                        for (int j = 0; j < variable.Length; j++)
                        {
                            if (variable[j] == '=')
                            {
                                if (j + 1 < variable.Length)
                                {
                                    newValue = variable.Substring(j + 1);
                                    newKey = variable.Substring(0, j);
                                }
                                break;
                            }
                        }

                        if (newKey.Any() && (newKey.Last() == '=' || newKey.Last() == '&'))
                        {
                            newKey = newKey.Remove(newKey.Length - 1);
                        }

                        newKey = HttpUtility.UrlDecode(newKey);
                        newValue = HttpUtility.UrlDecode(newValue);

                        h.VariablesHttpHead.Add(newKey, newValue);
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

                    h.RequestUrl = Uri.UnescapeDataString(linput[i].Substring(5, index - 5));

                    for (int k = 0; k < h.RequestUrl.Length - 1; k++)
                    {
                        if (h.RequestUrl[k] == '?')
                        {
                            string add = h.RequestUrl.Substring(k + 1);

                            if (add[add.Length - 1] == ' ')
                                add = add.Remove(add.Length - 1);

                            h.RequestUrl = h.RequestUrl.Remove(k);
                            add = add.Replace('+', ' ');

                            for (int it = 0; it < add.Length; it++)
                            {
                                if (add[it] == '&')
                                {
                                    h.VariablesHttpHead.Add(add.Substring(0, it), "");
                                    add = add.Remove(0, it + 1);
                                    it = -1;
                                }
                                else if (add[it] == '#')
                                {
                                    if (it > 0)
                                        h.VariablesHttpHead.Add(add.Substring(0, it), "");

                                    add = "";

                                    break;
                                }
                            }

                            h.VariablesHttpHead.Add(add, "");
                            break;
                        }
                    }

                    h.RequestUrl = h.RequestUrl.TrimEnd('?');

                    var variables = h.VariablesHttpHead.Keys;

                    foreach (var variable in variables)
                    {
                        h.VariablesHttpHead.Remove(variable);

                        string newKey = variable;
                        string newValue = "";

                        if (!newKey.Any())
                            continue;

                        for (int j = 0; j < variable.Length; j++)
                        {
                            if (variable[j] == '=')
                            {
                                if (j + 1 < variable.Length)
                                {
                                    newValue = variable.Substring(j + 1);
                                    newKey = variable.Substring(0, j);
                                }
                                break;
                            }
                        }

                        if (newKey.Any() && (newKey.Last() == '=' || newKey.Last() == '&'))
                        {
                            newKey = newKey.Remove(newKey.Length - 1);
                        }

                        if (!newKey.Any() && !newValue.Any())
                            continue;

                        newKey = HttpUtility.UrlDecode(newKey);
                        newValue = HttpUtility.UrlDecode(newValue);

                        h.VariablesHttpHead.Add(newKey, newValue);
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
                                    // Retrieve POST values and build keyValue pairs with "" as value (will be extracted from the key later)
                                    var kvpairs = from string key in linput[k].Replace('+', ' ').Split('&') select new KeyValuePair<string, string>(key, "");

                                    foreach (var variable in kvpairs)
                                        h.VariablesHttpPost.Add(variable);
                                }

                                goto SEARCHINGFORPOSTBODY_DONE;
                            }
                        }
                    }

                    SEARCHINGFORPOSTBODY_DONE:

                    var postVariables = h.VariablesHttpPost.Keys;

                    foreach (string variable in postVariables)
                    {
                        string newKey = variable;
                        string newValue = "";

                        h.VariablesHttpPost.Remove(variable);

                        if (!newKey.Any())
                            continue;

                        for (int k = 0; k < variable.Length; k++)
                        {
                            if (variable[k] == '=')
                            {
                                if (k + 1 < variable.Length)
                                {
                                    newValue = variable.Substring(k + 1);
                                    newKey = variable.Substring(0, k);
                                }
                                break;
                            }
                        }

                        if (newKey.Any() && (newKey.Last() == '=' || newKey.Last() == '&'))
                        {
                            newKey = newKey.Remove(newKey.Length - 1);
                        }

                        if (!newKey.Any() && !newValue.Any())
                            continue;

                        newKey = HttpUtility.UrlDecode(newKey);
                        newValue = HttpUtility.UrlDecode(newValue);

                        h.VariablesHttpPost.Add(newKey, newValue);
                    }

                    // Crazy hack for Chrome POST packets
                    if(h.VariablesHttpPost.Count == 0)
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
                            return new HttpRequest() { IsIncompleteRequest = true };
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

                return Constructor(ref input, null, stream);
            }

            return h;
        }

        private const string ifmodifiedsince = "If-Modified-Since: ",
            cookie = "Cookie: ";

        private static HttpRequest GetCookiesAndModified(HttpRequest packet, string[] linput)
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
                else if(packet.Range == null && linput[i].StartsWith("Range: bytes="))
                {
                   string[] beginEndPair = linput[i].Substring(("Range: bytes=").Length).Split('-');
                   int val1, val2;
                   if(beginEndPair.Length > 1 && int.TryParse(beginEndPair[0],out val1) && int.TryParse(beginEndPair[1],out val2))
                   {
                        packet.Range = new Tuple<int, int>(val1, val2);
                   }
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