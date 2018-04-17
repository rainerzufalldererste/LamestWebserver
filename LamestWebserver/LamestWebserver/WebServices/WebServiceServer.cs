using System;
using System.Net.Sockets;

namespace LamestWebserver.WebServices
{
    public class WebServiceServer : ServerCore
    {
        public readonly WebServiceHandler RequestHandler;

        public WebServiceServer(WebServiceHandler webRequestHandler, int port = 8310) : base(port)
        {
            if (webRequestHandler == null)
                throw new NullReferenceException(nameof(webRequestHandler));
            
            RequestHandler = webRequestHandler;
        }

        protected override void HandleClient(TcpClient tcpClient, NetworkStream networkStream)
        {
            throw new NotImplementedException();
        }
    }
}
