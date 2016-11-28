using Fleck;
using Fleck.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LamestWebserver
{
    public class WebSocketManagementOvertakeFlagException : Exception
    {
    }

    public class WebSocketCommunicationHandler : IURLIdentifyable
    {
        public string URL { get; private set; }

        public WebSocketCommunicationHandler(string URL)
        {
            this.URL = URL;
            _OnMessage = message => callOnMessage(message, WebSocketHandlerProxy.currentProxy);
            _OnClose = () => OnDisconnect(WebSocketHandlerProxy.currentProxy);
            _OnPing = data => WebSocketHandlerProxy.currentProxy.RespondPong(data);
            _OnPong = x => { };

            register();
        }

        internal Action<string> _OnMessage { get; set; }
        internal Action _OnClose { get; set; }
        internal Action<byte[]> _OnBinary { get; set; }
        internal Action<byte[]> _OnPing { get; set; }
        internal Action<byte[]> _OnPong { get; set; }

        public event Action<string, WebSocketHandlerProxy> OnMessage = delegate { };
        public event Action OnRespond = delegate { };
        public event Action<WebSocketHandlerProxy> OnConnect = delegate { };
        public event Action<WebSocketHandlerProxy> OnDisconnect = delegate { };

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

        internal void callOnResponded()
        {
            OnRespond();
        }

        internal void callOnConnect(WebSocketHandlerProxy webSocketHandlerProxy)
        {
            OnConnect(webSocketHandlerProxy);
        }

        internal void callOnDisconnect(WebSocketHandlerProxy webSocketHandlerProxy)
        {
            OnDisconnect(webSocketHandlerProxy);
        }
    }

    public class WebSocketHandlerProxy
    {
        public static TimeSpan MaximumLastMessageTime = TimeSpan.FromSeconds(2d);

        private NetworkStream networkStream;
        private WebSocketCommunicationHandler handler;
        private ComposableHandler websocketHandler;

        public bool isActive { get; private set; } = true;

        public DateTime lastMessageReceived = DateTime.UtcNow;
        public DateTime lastMessageSent = DateTime.UtcNow;

        [ThreadStatic] internal static WebSocketHandlerProxy currentProxy;

        internal WebSocketHandlerProxy(NetworkStream stream, WebSocketCommunicationHandler handler, ComposableHandler websocketHandler)
        {
            this.networkStream = stream;
            this.handler = handler;
            this.websocketHandler = websocketHandler;

            currentProxy = this;
            handleConnection();
        }

        public void Respond(string Message)
        {
            if (networkStream == null || !isActive)
                return;

            try
            {
                byte[] buffer = websocketHandler.TextFrame.Invoke(Message);
                networkStream.WriteAsync(buffer, 0, buffer.Length);
                lastMessageSent = DateTime.UtcNow;
                handler.callOnResponded();
            }
            catch (ObjectDisposedException)
            {
                isActive = false;
                return;
            }
        }

        public void RespondPong(byte[] bytes)
        {
            if (networkStream == null || !isActive)
                return;

            try
            {
                byte[] buffer = websocketHandler.PongFrame.Invoke(bytes);
                networkStream.WriteAsync(buffer, 0, buffer.Length);
                lastMessageSent = DateTime.UtcNow;
            }
            catch (ObjectDisposedException)
            {
                isActive = false;
                return;
            }
        }

        public void RespondPing(byte[] bytes)
        {
            if (networkStream == null || !isActive)
                return;

            try
            {
                byte[] buffer = websocketHandler.PingFrame.Invoke(bytes);
                networkStream.WriteAsync(buffer, 0, buffer.Length);
                lastMessageSent = DateTime.UtcNow;
            }
            catch (ObjectDisposedException)
            {
                isActive = false;
                return;
            }
        }

        public void RespondBinary(byte[] bytes)
        {
            if (networkStream == null || !isActive)
                return;

            try
            {
                byte[] buffer = websocketHandler.BinaryFrame.Invoke(bytes);
                networkStream.WriteAsync(buffer, 0, buffer.Length);
                lastMessageSent = DateTime.UtcNow;
                handler.callOnResponded();
            }
            catch (ObjectDisposedException)
            {
                isActive = false;
                return;
            }
        }

        private void handleConnection()
        {
            handler.callOnConnect(this);

            byte[] currentBuffer = new byte[4096];
            byte[] trimmedBuffer;
            int responseNum = 0;

            while (true)
            {
                var token = new CancellationTokenSource(10000).Token;
                responseNum++;
                int innerResponseNum = responseNum;

                token.Register(() =>
                {
                    if (isActive && responseNum == innerResponseNum)
                    {
                        networkStream.Close();
                        isActive = false;
                        networkStream = null;
                    }
                });

                try
                {
                    if (networkStream == null)
                        break;

                    var byteCount = networkStream.ReadAsync(currentBuffer, 0, 4096, token);
                    byteCount.Wait();

                    if (byteCount.IsCanceled || networkStream == null)
                        break;

                    if (byteCount.Result == 0)
                        break;

                    lastMessageReceived = DateTime.UtcNow;

                    trimmedBuffer = new byte[byteCount.Result];

                    Array.Copy(currentBuffer, trimmedBuffer, trimmedBuffer.Length);

                    websocketHandler.Receive(trimmedBuffer);
                }
                catch (ThreadAbortException)
                {
                    break;
                }
                catch (WebSocketManagementOvertakeFlagException)
                {
                    return;
                }
                catch (Exception e)
                {
                }
            }

            ConnectionClosed();
            isActive = false;
        }

        public bool ReadAsync()
        {
            byte[] currentBuffer = new byte[4096], trimmedBuffer;
            var token = new CancellationTokenSource(10000).Token;
            token.Register(() =>
            {
                if (isActive)
                {
                    networkStream.Close();
                    isActive = false;
                    networkStream = null;
                }
            });

            try
            {
                if (networkStream == null)
                    return false;

                var byteCount = networkStream.ReadAsync(currentBuffer, 0, 4096, token);
                byteCount.Wait();

                if (byteCount.IsCanceled || networkStream == null)
                    return false;

                if (byteCount.Result == 0)
                    return false;

                lastMessageReceived = DateTime.UtcNow;

                trimmedBuffer = new byte[byteCount.Result];

                Array.Copy(currentBuffer, trimmedBuffer, trimmedBuffer.Length);

                websocketHandler.Receive(trimmedBuffer);
            }
            catch (ThreadAbortException e)
            {
                isActive = false;
                throw e;
            }
            catch (WebSocketManagementOvertakeFlagException e)
            {
                throw new InvalidOperationException("WebSocketHandlerProxy.ReadAsync does not support SocketManagementOvertakeFlagExceptions.", e);
            }
            catch (Exception)
            {
            }

            return true;
        }

        public void ConnectionClosed()
        {
            handler.callOnDisconnect(this);
        }
    }
}
