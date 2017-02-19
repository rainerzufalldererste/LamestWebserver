using Fleck.Handlers;
using System;
using System.IO;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using LamestWebserver.Synchronization;

namespace LamestWebserver
{
    public class WebSocketManagementOvertakeFlagException : Exception
    {
    }

    public class WebSocketCommunicationHandler : IURLIdentifyable
    {
        public static TimeSpan DefaultMaximumLastMessageTime = TimeSpan.FromSeconds(2d);
        public readonly TimeSpan MaximumLastMessageTime = DefaultMaximumLastMessageTime;

        public string URL { get; private set; }

        public WebSocketCommunicationHandler(string URL, TimeSpan? maximumLastMessageTime = null)
        {
            this.URL = URL;

            if(maximumLastMessageTime.HasValue)
                MaximumLastMessageTime = maximumLastMessageTime.Value;

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

        protected event Action<string, WebSocketHandlerProxy> OnMessage = delegate { };
        protected event Action OnResponded = delegate { };
        protected event Action<WebSocketHandlerProxy> OnConnect = delegate { };
        protected event Action<WebSocketHandlerProxy> OnDisconnect = delegate { };

        private Random random = new Random();

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
            int tries = 0;

            RETRY:

            try
            {
                OnMessage(message, proxy);
            }
            catch (MutexRetryException e)
            {
                if (tries > 10)
                    throw e;

                ServerHandler.LogMessage("MutexRetryException in Websocket - retrying...");

                tries++;
                Thread.Sleep(random.Next(5 * tries));
                goto RETRY;
            }
        }

        internal void callOnResponded()
        {
            OnResponded();
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
        private NetworkStream networkStream;
        private WebSocketCommunicationHandler handler;
        private ComposableHandler websocketHandler;

        public bool IsActive { get; private set; } = true;

        public DateTime LastMessageReceived = DateTime.UtcNow;
        public DateTime LastMessageSent = DateTime.UtcNow;

        private int exceptedTries = 0;
        
        public ushort Port;

        [ThreadStatic] internal static WebSocketHandlerProxy currentProxy;

        internal WebSocketHandlerProxy(NetworkStream stream, WebSocketCommunicationHandler handler, ComposableHandler websocketHandler, ushort port)
        {
            this.networkStream = stream;
            this.handler = handler;
            this.websocketHandler = websocketHandler;
            this.Port = port;

            currentProxy = this;
            handleConnection();
        }

        public async void Respond(string Message)
        {
            if (networkStream == null || !IsActive)
                return;

            try
            {
                byte[] buffer = websocketHandler.TextFrame.Invoke(Message);
                await networkStream.WriteAsync(buffer, 0, buffer.Length);
                LastMessageSent = DateTime.UtcNow;
                handler.callOnResponded();
                exceptedTries = 0;
            }
            catch (ObjectDisposedException)
            {
                IsActive = false;
                return;
            }
            catch (ThreadAbortException e)
            {
                throw e;
            }
            catch (IOException)
            {
                IsActive = false;

                try
                {
                    networkStream.Dispose();
                }
                finally
                {
                    networkStream = null;
                }

                /*
                exceptedTries++;

                if (exceptedTries > 2)
                    isActive = false;*/
            }
            catch (Exception e)
            {
                ServerHandler.LogMessage("Critical Exception in Websocket Responding:\n" + e);
            }
        }

        public void RespondPong(byte[] bytes)
        {
            if (networkStream == null || !IsActive)
                return;

            try
            {
                byte[] buffer = websocketHandler.PongFrame.Invoke(bytes);
                networkStream.WriteAsync(buffer, 0, buffer.Length);
                LastMessageSent = DateTime.UtcNow;
                exceptedTries = 0;
            }
            catch (ObjectDisposedException)
            {
                IsActive = false;
                return;
            }
            catch (ThreadAbortException e)
            {
                throw e;
            }
            catch (IOException)
            {
                IsActive = false;

                try
                {
                    networkStream.Dispose();
                }
                finally
                {
                    networkStream = null;
                }

                /*
                exceptedTries++;

                if (exceptedTries > 2)
                    isActive = false;*/
            }
            catch (Exception e)
            {
                ServerHandler.LogMessage("Critical Exception in Websocket Responding:\n" + e);
            }
        }

        public void RespondPing(byte[] bytes)
        {
            if (networkStream == null || !IsActive)
                return;

            try
            {
                byte[] buffer = websocketHandler.PingFrame.Invoke(bytes);
                networkStream.WriteAsync(buffer, 0, buffer.Length);
                LastMessageSent = DateTime.UtcNow;
                exceptedTries = 0;
            }
            catch (ObjectDisposedException)
            {
                IsActive = false;
                return;
            }
            catch (ThreadAbortException e)
            {
                throw e;
            }
            catch (IOException)
            {
                IsActive = false;

                try
                {
                    networkStream.Dispose();
                }
                finally
                {
                    networkStream = null;
                }

                /*
                exceptedTries++;

                if (exceptedTries > 2)
                    isActive = false;*/
            }
            catch (Exception e)
            {
                ServerHandler.LogMessage("Critical Exception in Websocket Responding:\n" + e);
            }
        }

        public void RespondBinary(byte[] bytes)
        {
            if (networkStream == null || !IsActive)
                return;

            try
            {
                byte[] buffer = websocketHandler.BinaryFrame.Invoke(bytes);
                networkStream.WriteAsync(buffer, 0, buffer.Length);
                LastMessageSent = DateTime.UtcNow;
                handler.callOnResponded();
                exceptedTries = 0;
            }
            catch (ObjectDisposedException)
            {
                IsActive = false;
                return;
            }
            catch (ThreadAbortException e)
            {
                throw e;
            }
            catch (IOException)
            {
                IsActive = false;

                try
                {
                    networkStream.Dispose();
                }
                finally
                {
                    networkStream = null;
                }

                /*
                exceptedTries++;

                if (exceptedTries > 2)
                    isActive = false;*/
            }
            catch (Exception e)
            {
                ServerHandler.LogMessage("Critical Exception in Websocket Responding:\n" + e);
            }
        }

        private void handleConnection()
        {
            handler.callOnConnect(this);

            byte[] currentBuffer = new byte[4096];

            while (true)
            {
                try
                {
                    if (networkStream == null)
                        return;

                    var byteCount = networkStream.ReadAsync(currentBuffer, 0, 4096);

                    if(
#if DEBUG
                    byteCount.Wait(24000)
#else
                    byteCount.Wait(15000)
#endif
                       )
                    {
                        if (byteCount.IsCanceled || networkStream == null)
                            break;

                        if (byteCount.Result == 0)
                            break;

                        LastMessageReceived = DateTime.UtcNow;

                        var trimmedBuffer = new byte[byteCount.Result];

                        Array.Copy(currentBuffer, trimmedBuffer, trimmedBuffer.Length);

                        websocketHandler.Receive(trimmedBuffer);

                        exceptedTries = 0;
                    }
                    else
                    {
                        break;
                    }
                }
                catch (ThreadAbortException)
                {
                    break;
                }
                catch (WebSocketManagementOvertakeFlagException)
                {
                    return;
                }
                catch (IOException e)
                {
                    ServerHandler.LogMessage("Exception in WebSocket. The connection might already have closed.\n" + e);
                    break;
                }
            }

            IsActive = false;
            ConnectionClosed();
        }

        public bool ReadAsync()
        {
            byte[] currentBuffer = new byte[4096];

            try
            {
                if (networkStream == null)
                    return false;

                var byteCount = networkStream.ReadAsync(currentBuffer, 0, 4096);

                if (
#if DEBUG
                    byteCount.Wait(240000)
#else
                    byteCount.Wait(15000)
#endif
                       )
                {
                    if (byteCount.IsCanceled || networkStream == null)
                        return false;

                    if (byteCount.Result == 0)
                        return false;

                    LastMessageReceived = DateTime.UtcNow;

                    var trimmedBuffer = new byte[byteCount.Result];

                    Array.Copy(currentBuffer, trimmedBuffer, trimmedBuffer.Length);

                    websocketHandler.Receive(trimmedBuffer);

                    exceptedTries = 0;
                }
                else
                {
                    IsActive = false;
                    networkStream = null;
                    return false;
                }
            }
            catch (ThreadAbortException e)
            {
                IsActive = false;
                throw e;
            }
            catch (WebSocketManagementOvertakeFlagException e)
            {
                throw new InvalidOperationException("WebSocketHandlerProxy.ReadAsync does not support SocketManagementOvertakeFlagExceptions.", e);
            }
            catch (Exception e)
            {
                ServerHandler.LogMessage("Exception in WebSocket. The connection might already have closed.\n" + e);
                IsActive = false;
                networkStream = null;
                return false;
            }

            return true;
        }

        internal NetworkStream GetNetworkStream()
        {
            return networkStream;
        }

        public void ConnectionClosed()
        {
            handler.callOnDisconnect(this);
        }
    }
}
