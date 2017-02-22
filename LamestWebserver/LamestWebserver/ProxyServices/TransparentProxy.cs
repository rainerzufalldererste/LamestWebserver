using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
        /// <exception cref="InvalidOperationException">Throws an exception if the port is currently blocked</exception>
        public TransparentProxy(IPEndPoint gateway, int proxyServerPort, byte[] response = null, int timeout = 15000, int gatewayTimeout = 250)
        {
            Gateway = gateway;
            ProxyServerPort = proxyServerPort;
            _responseIfNotAvailable = response;
            _timeout = timeout;
            _gatewayTimeout = gatewayTimeout;

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
                            var task = _listener.AcceptTcpClientAsync();
                            Thread t = new Thread(HandleClient);
                            task.Wait();
                            t.Start(task.Result);
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
            NetworkStream stream = client.GetStream();

            while (_active)
            {
                try
                {
                    byte[] inputBuffer = new byte[4096];

                    var task = stream.ReadAsync(inputBuffer, 0, 4096);

                    if (!task.Wait(_timeout))
                        return;
                    else
                    {
                        TcpClient tcpC = new TcpClient();
                        var proxyTask = tcpC.ConnectAsync(Gateway.Address, Gateway.Port);

                        if (proxyTask.Wait(_gatewayTimeout))
                        {
                            NetworkStream nws = tcpC.GetStream();

                            nws.Write(inputBuffer, 0, task.Result);

                            byte[] gateWayBuffer = new byte[4096];

                            var readTask = nws.ReadAsync(gateWayBuffer, 0, 4096);

                            if (readTask.Wait(_gatewayTimeout))
                            {
                                stream.Write(gateWayBuffer, 0, readTask.Result);
                            }
                            else
                            {
                                try
                                {
                                    stream.WriteAsync(_responseIfNotAvailable, 0, _responseIfNotAvailable.Length);
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
                        else
                        {
                            try
                            {
                                stream.WriteAsync(_responseIfNotAvailable, 0, _responseIfNotAvailable.Length);
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
                }
                catch (IOException)
                {
                    break;
                }
            }

            stream.Dispose();
        }
    }
}

