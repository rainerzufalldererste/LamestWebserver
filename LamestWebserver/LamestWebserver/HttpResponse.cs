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
        /// describes the range of bytes there are requested
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
        public HttpResponse()
        {
            Date = DateTime.Now.ToString(HtmlDateFormat);
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

            sb.Append("Content-Length: " + _contentLength + "\r\n\r\n");

            byte[] ret0 = Encoding.UTF8.GetBytes(sb.ToString());
            byte[] ret = new byte[ret0.Length + BinaryData.Length];

            Array.Copy(ret0, ret, ret0.Length);
            Array.Copy(BinaryData, 0, ret, ret0.Length, BinaryData.Length);

            return ret;
        }
    }
}
