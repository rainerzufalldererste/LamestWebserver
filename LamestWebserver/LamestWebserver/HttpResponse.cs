using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LamestWebserver
{
    /// <summary>
    /// Contains tools to build HTTP-Responses.
    /// </summary>
    public class HttpResponse
    {
        /// <summary>
        /// An expression used for DateTime.ToString to parse into correct HTTP DateFormat
        /// </summary>
        public const string HtmlDateFormat = "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'";

        /// <summary>
        /// The current date in HTTP DateFormat
        /// </summary>
        public string Date; //Tue, 21 Apr 2015 22:51:19 GMT

        /// <summary>
        /// The HTTP Version of the Response
        /// </summary>
        public string Version = "HTTP/1.1";

        /// <summary>
        /// The HTTP Status Code and Status of the Response
        /// </summary>
        public string Status = "200 OK";

        private int _contentLength = 0;

        /// <summary>
        /// describes the range of bytes this package sends
        /// item1 = begin
        /// item2 = end
        /// is null when all bytes are requested
        /// </summary>
        public Tuple<int, int> Range = null;

        /// <summary>
        /// The content-type of the response
        /// </summary>
        public string ContentType = "text/html";

        /// <summary>
        /// The cookies, that shall be set in the client browser
        /// </summary>
        public List<KeyValuePair<string, string>> Cookies = null;

        /// <summary>
        /// The modified date of the file (if any)
        /// </summary>
        public DateTime? ModifiedDate = null;


        /// <summary>
        /// the binary data contained in the request
        /// Also sets the contentLength.
        /// </summary>
        public byte[] BinaryData { get { return _binaryData; } set { _binaryData = value; _contentLength = _binaryData.Length; } }
        private byte[] _binaryData;

        /// <summary>
        /// Creates a new HttpResponse
        /// </summary>
        private HttpResponse(HttpRequest requestPacket)
        {
            Date = DateTime.Now.ToString(HtmlDateFormat);

            if(requestPacket != null)
            {
                Range = requestPacket.Range;
                
                if(Range != null)
                    Status = "206 Partial Content";
            }
        }

        public HttpResponse(HttpRequest requestPacket, byte[] binaryData) : this(requestPacket)
        {
            BinaryData = binaryData;
        }

        public HttpResponse(HttpRequest requestPacket, string responseString) : this(requestPacket)
        {
            if (responseString != null)
                BinaryData = Encoding.UTF8.GetBytes(responseString);
        }

        /// <summary>
        /// Returns the contents of the complete package to be sent via tcp to the client 
        /// </summary>
        /// <returns>the contents as byte array</returns>
        public byte[] GetPackage()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(Version + " " + Status + "\r\n");
            sb.Append("Date: " + Date + "\r\n");
            sb.Append("Server: LamestWebserver (" + Environment.OSVersion.ToString() + ")\r\n");

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

            if (ModifiedDate.HasValue)
            {
                sb.Append("Last-Modified: " + ModifiedDate.Value.ToString(HtmlDateFormat) + "\r\n");
            }

            sb.Append("Connection: Keep-Alive\r\n");

            if (ContentType != null)
                sb.Append("Content-Type: " + ContentType + "; charset=UTF-8\r\n");

            if(Range != null)
            {
                if(Range.Item2 >= _contentLength) 
                {
                    return new HttpResponse(null) {
                        Status = "416 Requested Range Not Satisfiable",
                        BinaryData = Encoding.UTF8.GetBytes(Master.GetErrorMsg(
                        "416 Requested Range Not Satisfiable",
                        "<p>The Requested byte range cannot be delivered due to illegal range Parameters.</p>" +
                        "</div></p>"))
                    }.GetPackage();
                }
                
                sb.Append("Content-Range: bytes " + Range.Item1 + "-" + Range.Item2 + "/" + _contentLength + "\r\n");
                
                int rangeSize = Range.Item2 - Range.Item1 + 1;
                byte[] byteRange = new Byte[rangeSize];
                Array.Copy(BinaryData, Range.Item1, byteRange, 0, rangeSize);
                BinaryData = byteRange;
            }

            sb.Append("Content-Length: " + _contentLength + "\r\n\r\n");

            byte[] ret0 = Encoding.UTF8.GetBytes(sb.ToString());

            if (BinaryData != null)
            {
                byte[] ret = new byte[ret0.Length + BinaryData.Length];

                Array.Copy(ret0, ret, ret0.Length);
                Array.Copy(BinaryData, 0, ret, ret0.Length, BinaryData.Length);

                return ret;
            }
            else
            {
                return ret0;
            }
        }
    }
}
