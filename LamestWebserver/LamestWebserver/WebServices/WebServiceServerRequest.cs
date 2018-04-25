using LamestWebserver.Core;
using LamestWebserver.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace LamestWebserver.WebServices
{
    /// <summary>
    /// Contains Functionality to send requests to remote WebServiceServers.
    /// </summary>
    public static class WebServiceServerRequest
    {
        /// <summary>
        /// The maximum size of the response from the remote WebServiceServer.
        /// </summary>
        public static int MaxResponseSize = 1024 * 1024;

        /// <summary>
        /// Requests a WebServiceRequest at a remote WebServiceServer.
        /// </summary>
        /// <param name="request">The request to send.</param>
        /// <param name="ipEndPoint">The IPEndpoint of the remote WebServiceServer.</param>
        /// <returns>Returns a WebServiceResponse from the Remote WebServiceServer.</returns>
        public static WebServiceResponse Request(WebServiceRequest request, IPEndPoint ipEndPoint)
        {
            try
            {
                byte[] bytes = Encoding.UTF8.GetBytes(Serializer.WriteJsonDataInMemory(request));
                byte[] response = new byte[MaxResponseSize];

                TcpClient client = new TcpClient();
                client.NoDelay = true;
                client.Connect(ipEndPoint);

                NetworkStream networkStream = client.GetStream();
                networkStream.Write(bytes, 0, bytes.Length);

                int length = networkStream.Read(response, 0, response.Length);

                client.Close();

                return Serializer.ReadJsonDataInMemory<WebServiceResponse>(Encoding.UTF8.GetString(response, 0, length));
            }
            catch (Exception e)
            {
                Logger.LogExcept(e);

                throw e;
            }
        }
    }
}
