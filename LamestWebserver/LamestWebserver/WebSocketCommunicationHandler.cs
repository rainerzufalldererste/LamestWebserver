using Fleck;
using Fleck.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace LamestWebserver
{
    public class WebSocketCommunicationHandler : IURLIdentifyable
    {
        public string URL { get; private set; }

        public WebSocketCommunicationHandler(string URL)
        {
            this.URL = URL;
            _OnMessage = message => callOnMessage(message, WebSocketHandlerProxy.currentProxy);
            _OnMessage = message => callOnMessage(message, WebSocketHandlerProxy.currentProxy);

            register();
        }

        internal Action<string> _OnMessage { get; set; }
        internal Action _OnClose { get; set; }
        internal Action<byte[]> _OnBinary { get; set; }
        internal Action<byte[]> _OnPing { get; set; }
        internal Action<byte[]> _OnPong { get; set; }

        public event Action<string, WebSocketHandlerProxy> OnMessage = delegate { };
        public event Action OnRespond = delegate { };

        protected void register()
        {
            Master.AddWebsocketHandler(this);
        }

        protected void unregister()
        {
            Master.RemoveWebsocketHandler(this.URL);
        }

        internal void callOnMessage(string message, WebSocketHandlerProxy proxy)
        {
            OnMessage(message, proxy);
        }

        internal void callOnRespond()
        {
            OnRespond();
        }
    }

    public class WebSocketHandlerProxy
    {
        private NetworkStream networkStream;
        private WebSocketCommunicationHandler handler;
        private ComposableHandler websocketHandler;

        [ThreadStatic]
        internal static WebSocketHandlerProxy currentProxy;

        public WebSocketHandlerProxy(NetworkStream stream, WebSocketCommunicationHandler handler, ComposableHandler websocketHandler)
        {
            this.networkStream = stream;
            this.handler = handler;
            this.websocketHandler = websocketHandler;

            currentProxy = this;
            handleConnection();
        }

        public virtual void Respond(string Message)
        {
            byte[] buffer = websocketHandler.TextFrame.Invoke(Message);
            networkStream.WriteAsync(buffer, 0, buffer.Length);
            handler.callOnRespond();
        }

        private void handleConnection()
        {
            byte[] currentBuffer = new byte[4096];
            byte[] trimmedBuffer;

            while (true)
            {
                var byteCount = networkStream.ReadAsync(currentBuffer, 0, 4096);
                byteCount.Wait();
                trimmedBuffer = new byte[byteCount.Result];

                Array.Copy(currentBuffer, trimmedBuffer, trimmedBuffer.Length);

                websocketHandler.Receive(trimmedBuffer);
            }
        }
    }
}
