using Fleck.Handlers;
using System;
using System.IO;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using LamestWebserver.Synchronization;

namespace LamestWebserver
{
    /// <summary>
    /// An Exception used to Tell the outer Thread that the Websocket controll has been taken over by another thread.
    /// </summary>
    public class WebSocketManagementOvertakeFlagException : Exception
    {
    }

    /// <summary>
    /// A raw Communication Handler for WebSocket Connections, representing a response scheme for WebSocket Requests
    /// </summary>
    public class WebSocketCommunicationHandler : IURLIdentifyable
    {

        /// <inheritdoc />
        public string URL { get; private set; }

        /// <summary>
        /// Constructs and registers a new CommunicationHandler for Websockets
        /// </summary>
        /// <param name="URL"></param>
        public WebSocketCommunicationHandler(string URL)
        {
            this.URL = URL;

            _OnMessage = message => CallOnMessage(message, WebSocketHandlerProxy.CurrentProxy);
            _OnClose = () => OnDisconnect(WebSocketHandlerProxy.CurrentProxy);
            _OnPing = data => WebSocketHandlerProxy.CurrentProxy.RespondPong(data);
            _OnPong = x => { };

            Register();
        }

        internal Action<string> _OnMessage { get; set; }
        internal Action _OnClose { get; set; }
        internal Action<byte[]> _OnBinary { get; set; }
        internal Action<byte[]> _OnPing { get; set; }
        internal Action<byte[]> _OnPong { get; set; }

        /// <summary>
        /// The event to execute whenever a new Message has been received
        /// </summary>
        protected event Action<string, WebSocketHandlerProxy> OnMessage = delegate { };
        
        /// <summary>
        /// The event to execute whenever a new Message has been sent
        /// </summary>
        protected event Action OnResponded = delegate { };

        /// <summary>
        /// The event to execute whenever a client connected
        /// </summary>
        protected event Action<WebSocketHandlerProxy> OnConnect = delegate { };

        /// <summary>
        /// The event to execute whenever a client disconnected
        /// </summary>
        protected event Action<WebSocketHandlerProxy> OnDisconnect = delegate { };

        private readonly Random _random = new Random();

        /// <summary>
        /// Registers the current handler at the servers
        /// </summary>
        protected void Register()
        {
            Master.AddWebsocketHandler(this);
        }

        /// <summary>
        /// Unregisters the current handler at the servers
        /// </summary>
        protected void Unregister()
        {
            Master.RemoveWebsocketHandler(this.URL);
        }

        internal void CallOnMessage(string message, WebSocketHandlerProxy proxy)
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
                Thread.Sleep(_random.Next(5 * tries));
                goto RETRY;
            }
        }

        internal void CallOnResponded()
        {
            OnResponded();
        }

        internal void CallOnConnect(WebSocketHandlerProxy webSocketHandlerProxy)
        {
            OnConnect(webSocketHandlerProxy);
        }

        internal void CallOnDisconnect(WebSocketHandlerProxy webSocketHandlerProxy)
        {
            OnDisconnect(webSocketHandlerProxy);
        }
    }

    /// <summary>
    /// A WebSocketHandlerProxy represents a single clients connection to a WebSocketCommunicationHandler
    /// </summary>
    public class WebSocketHandlerProxy
    {
        private NetworkStream _networkStream;
        private readonly WebSocketCommunicationHandler _handler;
        private readonly ComposableHandler _websocketHandler;

        /// <summary>
        /// Is the client still active
        /// </summary>
        public bool IsActive { get; private set; } = true;

        /// <summary>
        /// When did we receive the last message from the client
        /// </summary>
        public DateTime LastMessageReceived = DateTime.UtcNow;

        /// <summary>
        /// When did we send the last message to the client
        /// </summary>
        public DateTime LastMessageSent = DateTime.UtcNow;
        
        /// <summary>
        /// At which port did the client connect to the server
        /// </summary>
        public readonly ushort Port;

        [ThreadStatic] internal static WebSocketHandlerProxy CurrentProxy;

        internal WebSocketHandlerProxy(NetworkStream stream, WebSocketCommunicationHandler handler, ComposableHandler websocketHandler, ushort port)
        {
            this._networkStream = stream;
            this._handler = handler;
            this._websocketHandler = websocketHandler;
            this.Port = port;

            CurrentProxy = this;
            HandleConnection();
        }

        /// <summary>
        /// Sends a message to the client
        /// </summary>
        /// <param name="message">the message to send</param>
        public async void Respond(string message)
        {
            if (_networkStream == null || !IsActive)
                return;

            try
            {
                byte[] buffer = _websocketHandler.TextFrame.Invoke(message);
                await _networkStream.WriteAsync(buffer, 0, buffer.Length);
                LastMessageSent = DateTime.UtcNow;
                _handler.CallOnResponded();
            }
            catch (ObjectDisposedException)
            {
                IsActive = false;
                return;
            }
            catch (ThreadAbortException)
            {
                throw;
            }
            catch (IOException)
            {
                IsActive = false;

                try
                {
                    _networkStream.Dispose();
                }
                finally
                {
                    _networkStream = null;
                }
            }
            catch (Exception e)
            {
                ServerHandler.LogMessage("Critical Exception in Websocket Responding:\n" + e);
            }
        }

        /// <summary>
        /// Responds a Pong to the client
        /// </summary>
        /// <param name="bytes">the contained bytes</param>
        public void RespondPong(byte[] bytes)
        {
            if (_networkStream == null || !IsActive)
                return;

            try
            {
                byte[] buffer = _websocketHandler.PongFrame.Invoke(bytes);
                _networkStream.WriteAsync(buffer, 0, buffer.Length);
                LastMessageSent = DateTime.UtcNow;
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
                    _networkStream.Dispose();
                }
                finally
                {
                    _networkStream = null;
                }
            }
            catch (Exception e)
            {
                ServerHandler.LogMessage("Critical Exception in Websocket Responding:\n" + e);
            }
        }

        /// <summary>
        /// Responds a Ping to the client
        /// </summary>
        /// <param name="bytes">the contained bytes</param>
        public void RespondPing(byte[] bytes)
        {
            if (_networkStream == null || !IsActive)
                return;

            try
            {
                byte[] buffer = _websocketHandler.PingFrame.Invoke(bytes);
                _networkStream.WriteAsync(buffer, 0, buffer.Length);
                LastMessageSent = DateTime.UtcNow;
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
                    _networkStream.Dispose();
                }
                finally
                {
                    _networkStream = null;
                }
            }
            catch (Exception e)
            {
                ServerHandler.LogMessage("Critical Exception in Websocket Responding:\n" + e);
            }
        }

        /// <summary>
        /// Responds binary data to the client
        /// </summary>
        /// <param name="bytes">the bytes to send</param>
        public void RespondBinary(byte[] bytes)
        {
            if (_networkStream == null || !IsActive)
                return;

            try
            {
                byte[] buffer = _websocketHandler.BinaryFrame.Invoke(bytes);
                _networkStream.WriteAsync(buffer, 0, buffer.Length);
                LastMessageSent = DateTime.UtcNow;
                _handler.CallOnResponded();
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
                    _networkStream.Dispose();
                }
                finally
                {
                    _networkStream = null;
                }
            }
            catch (Exception e)
            {
                ServerHandler.LogMessage("Critical Exception in Websocket Responding:\n" + e);
            }
        }

        private void HandleConnection()
        {
            _handler.CallOnConnect(this);

            byte[] currentBuffer = new byte[4096];

            while (true)
            {
                try
                {
                    if (_networkStream == null)
                        return;

                    var byteCount = _networkStream.ReadAsync(currentBuffer, 0, 4096);

                    if(
#if DEBUG
                    byteCount.Wait(24000)
#else
                    byteCount.Wait(15000)
#endif
                       )
                    {
                        if (byteCount.IsCanceled || _networkStream == null)
                            break;

                        if (byteCount.Result == 0)
                            break;

                        LastMessageReceived = DateTime.UtcNow;

                        var trimmedBuffer = new byte[byteCount.Result];

                        Array.Copy(currentBuffer, trimmedBuffer, trimmedBuffer.Length);

                        _websocketHandler.Receive(trimmedBuffer);
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

        /// <summary>
        /// Reads asynchronously from the client stream
        /// </summary>
        /// <returns>true if the client responded anything</returns>
        public bool ReadAsync()
        {
            byte[] currentBuffer = new byte[4096];

            try
            {
                if (_networkStream == null)
                    return false;

                var byteCount = _networkStream.ReadAsync(currentBuffer, 0, 4096);

                if (
#if DEBUG
                    byteCount.Wait(240000)
#else
                    byteCount.Wait(15000)
#endif
                       )
                {
                    if (byteCount.IsCanceled || _networkStream == null)
                        return false;

                    if (byteCount.Result == 0)
                        return false;

                    LastMessageReceived = DateTime.UtcNow;

                    var trimmedBuffer = new byte[byteCount.Result];

                    Array.Copy(currentBuffer, trimmedBuffer, trimmedBuffer.Length);

                    _websocketHandler.Receive(trimmedBuffer);
                }
                else
                {
                    IsActive = false;
                    _networkStream = null;
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
                _networkStream = null;
                return false;
            }

            return true;
        }

        internal NetworkStream GetNetworkStream()
        {
            return _networkStream;
        }

        /// <summary>
        /// Triggers the disconnected event in the handler
        /// </summary>
        protected void ConnectionClosed()
        {
            _handler.CallOnDisconnect(this);
        }
    }
}
