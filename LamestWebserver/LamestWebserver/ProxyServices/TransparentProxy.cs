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
    public interface ITransparentProxy
    {
        int ProxyServerPort { get; }

        IPEndPoint Gateway { get; }

        void Stop();
    }

    public class TransparentProxy : ITransparentProxy
    {
        private byte[] _responseIfNotAvailable;
        private int _timeout;
        private readonly bool _loggingEnabled = false;
        private bool _active = true;
        private TcpListener listener;
        private int _gatewayTimeout;

        public int ProxyServerPort { get; }

        public IPEndPoint Gateway { get; }

        public void Stop()
        {

        }

        public TransparentProxy(IPEndPoint gateway, int proxyServerPort, byte[] response = null, bool loggingEnabled = false, int timeout = 15000, int gatewayTimeout = 250)
        {
            Gateway = gateway;
            ProxyServerPort = proxyServerPort;
            _responseIfNotAvailable = response;
            _timeout = timeout;
            _loggingEnabled = loggingEnabled;
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
                    listener = new TcpListener(ProxyServerPort);
                    listener.Start();
                    failed = 0;

                    while (_active)
                    {
                        try
                        {
                            var task = listener.AcceptTcpClientAsync();
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

