using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LamestWebserver.Core;

namespace LamestWebserver.ProxyServices
{
    /// <summary>
    /// The interface for a Transparent Proxy
    /// </summary>
    public interface ITransparentProxy
    {
        /// <summary>
        /// The port at which the proxy will be available at
        /// </summary>
        int ProxyServerPort { get; }

        /// <summary>
        /// the ipendpoint of the replicated service
        /// </summary>
        IPEndPoint Gateway { get; }

        /// <summary>
        /// Stops the transparent proxy
        /// </summary>
        void Stop();
    }

    /// <summary>
    /// A transparent proxy to redistribute other services locally or under different ports
    /// </summary>
    public class TransparentProxy : ITransparentProxy
    {
        private readonly byte[] _responseIfNotAvailable;
        private readonly int _timeout;
        private bool _active = true;
        private TcpListener _listener;
        private readonly int _gatewayTimeout;
        private readonly int _packetSize;

        /// <inheritdoc />
        public int ProxyServerPort { get; }

        /// <inheritdoc />
        public IPEndPoint Gateway { get; }

        /// <inheritdoc />
        public void Stop()
        {
            _active = false;
        }

        /// <summary>
        /// Constructs a new TranspartentProxy.
        /// </summary>
        /// <param name="gateway">the IPEndpoint of the replicated service</param>
        /// <param name="proxyServerPort">the port at which this service will be available at</param>
        /// <param name="response">the default response if the service is not available</param>
        /// <param name="timeout">the timeout at which to drop the connection to a client</param>
        /// <param name="gatewayTimeout">the timeout at which to expect the replicated service to be not available</param>
        /// <param name="packetSize">the size of a single packet that is forwarded</param>
        /// <exception cref="InvalidOperationException">Throws an exception if the port is currently blocked</exception>
        public TransparentProxy(IPEndPoint gateway, int proxyServerPort, byte[] response = null, int timeout = 15000, int gatewayTimeout = 250, int packetSize = 2048)
        {
            Gateway = gateway;
            ProxyServerPort = proxyServerPort;
            _responseIfNotAvailable = response;
            _timeout = timeout;
            _gatewayTimeout = gatewayTimeout;
            _packetSize = packetSize;

            if (!WebServer.TcpPortIsUnused(proxyServerPort))
                throw new InvalidOperationException("The TCP-Port " + proxyServerPort + " is currently blocked by another Application.");

            Thread t = new Thread(Listen);
            t.Start();
        }

        private void Listen()
        {
            int failed = 0;

            while (_active)
            {
                try
                {
                    _listener = new TcpListener(ProxyServerPort);
                    _listener.Start();
                    failed = 0;

                    while (_active)
                    {
                        try
                        {
                            var listener = _listener.AcceptTcpClientAsync();
                            listener.Wait();
                            Logger.LogInformation($"Transparent Proxy: Client Connected from {listener.Result.Client.RemoteEndPoint.ToString()}.");
                            Thread t = new Thread(HandleClient);
                            t.Start(listener.Result);
                        }
                        catch (Exception)
                        {
                            failed++;

                            if (failed > 50)
                                break;
                        }
                    }
                }
                catch (IOException)
                {
                    failed++;

                    if (failed > 50)
                    {
                        return;
                    }
                }
            }
        }

        private void HandleClient(object result)
        {
            TcpClient client = (TcpClient) result;
            client.NoDelay = true;
            NetworkStream stream = client.GetStream();
            stream.ReadTimeout = _timeout;
            
            while (_active)
            {
                try
                {
                    byte[] inputBuffer = new byte[_packetSize];

                    int count = stream.Read(inputBuffer, 0, _packetSize);

                    if (count == 0)
                        continue;

                    Logger.LogInformation($"Transparent Proxy: Accepted Request ({count} bytes)");

                    Thread t = new Thread(() =>
                    {
                        try
                        {
                            TcpClient tcpC = new TcpClient();
                            var proxyTask = tcpC.ConnectAsync(Gateway.Address, Gateway.Port);

                            if (proxyTask.Wait(_gatewayTimeout))
                            {
                                NetworkStream nws = tcpC.GetStream();
                                nws.ReadTimeout = _gatewayTimeout;
                                nws.WriteTimeout = _timeout;

                                nws.Write(inputBuffer, 0, count);

                                byte[] gateWayBuffer = new byte[_packetSize];

                                int readCount = nws.Read(gateWayBuffer, 0, _packetSize);

                                if (readCount > 0)
                                {
                                    stream.Write(gateWayBuffer, 0, readCount);
                                    Logger.LogInformation($"Transparent Proxy: Delivered Response ({readCount} bytes)");
                                }
                            }
                            else
                            {
                                try
                                {
                                    stream.Write(_responseIfNotAvailable, 0, _responseIfNotAvailable.Length);
                                    Logger.LogError("Transparent Proxy: Resource Not Available. (Connection Failure)");
                                }
                                catch (ObjectDisposedException)
                                {
                                    return;
                                }
                                catch (IOException)
                                {
                                    return;
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Logger.LogError($"Transparent Proxy: Resource Not Available. ({e.Message})");
                        }
                    });

                    t.Start();
                }
                catch (IOException)
                {
                    break;
                }
                catch (Exception)
                {
                    break;
                }
            }

            //stream.Dispose();
        }
    }
}

