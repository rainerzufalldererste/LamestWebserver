using LamestWebserver.Core;
using LamestWebserver.Serialization;
using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace LamestWebserver.WebServices
{
    /// <summary>
    /// A WebServiceServer makes a WebServiceHandler available to Remote Machines.
    /// </summary>
    public class WebServiceServer : ServerCore
    {
        /// <summary>
        /// The internal WebRequestHandler of this WebServiceServer.
        /// </summary>
        public readonly WebServiceHandler RequestHandler;

        /// <summary>
        /// The size that is read from the networkStream for each request.
        /// </summary>
        public static int RequestMaxPacketSize = 1024 * 128;

        public WebServiceServer(int port = 8310) : this(WebServiceHandler.CurrentServiceHandler.Instance, port) { }

        /// <summary>
        /// Starts a WebServiceServer at a given port using a specified WebRequestHandler to resolve requests.
        /// </summary>
        /// <param name="webRequestHandler">The WebRequestHandler to resolve requests with.</param>
        /// <param name="port">The Port to listen on.</param>
        public WebServiceServer(WebServiceHandler webRequestHandler, int port = 8310) : base(port)
        {
            if (webRequestHandler == null)
                throw new NullReferenceException(nameof(webRequestHandler));
            
            RequestHandler = webRequestHandler;
            Start();
        }

        /// <inheritdoc />
        protected override void HandleClient(TcpClient tcpClient, NetworkStream networkStream)
        {
            Encoding enc = Encoding.UTF8;
            byte[] msg;
            int bytes = 0;
            Stopwatch stopwatch = new Stopwatch();

            while(Running)
            {
                msg = new byte[RequestMaxPacketSize];

                try
                {
                    if (stopwatch.IsRunning)
                        stopwatch.Reset();

                    bytes = networkStream.Read(msg, 0, RequestMaxPacketSize);
                    stopwatch.Start();
                }
                catch (ThreadAbortException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    if (Running)
                    {
                        if (e.InnerException != null && e.InnerException is SocketException)
                        {
                            if (((SocketException)e.InnerException).SocketErrorCode == SocketError.TimedOut)
                            {
                                try
                                {
                                    string remoteEndPoint = tcpClient.Client.RemoteEndPoint.ToString();

                                    tcpClient.Client.Shutdown(SocketShutdown.Both);
                                    tcpClient.Close();

                                    Logger.LogTrace($"The connection to {remoteEndPoint} has been closed ordinarily after the timeout has been reached.", stopwatch);
                                }
                                catch { };

                                break;
                            }
                            else
                            {
                                string remoteEndPoint = "<unknown remote endpoint>";

                                try
                                {
                                    remoteEndPoint = tcpClient.Client.RemoteEndPoint.ToString();

                                    tcpClient.Client.Shutdown(SocketShutdown.Both);
                                    tcpClient.Close();

                                    Logger.LogTrace($"The connection to {remoteEndPoint} has been closed ordinarily after a SocketException occured. ({((SocketException)e.InnerException).SocketErrorCode})", stopwatch);

                                    break;
                                }
                                catch { }

                                Logger.LogTrace($"A SocketException occured with {remoteEndPoint}. ({((SocketException)e.InnerException).SocketErrorCode})", stopwatch);

                                break;
                            }
                        }

                        Logger.LogError("An exception occured in the WebService client handler:  " + e.SafeToString(), stopwatch);

                        try
                        {
                            tcpClient.Client.Shutdown(SocketShutdown.Both);
                            tcpClient.Close();

                            Logger.LogTrace($"The connection to {tcpClient.Client.RemoteEndPoint} has been closed ordinarily.", stopwatch);
                        }
                        catch { };
                    }

                    break;
                }

                if (bytes == 0)
                {
                    break;
                }

                try
                {
                    WebServiceRequest request = Serializer.ReadJsonDataInMemory<WebServiceRequest>(enc.GetString(msg, 0, bytes));
                    request.IsRemoteRequest = true;

                    WebServiceResponse response = null;

                    try
                    {
                        response = RequestHandler.Request(request);
                    }
                    catch(Exception e)
                    {
                        response = WebServiceResponse.Exception(e);
                    }

                    byte[] buffer = enc.GetBytes(Serializer.WriteJsonDataInMemory(response));

                    networkStream.Write(buffer, 0, buffer.Length);
                }
                catch (ThreadAbortException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    Logger.LogError("Error in WebServiceServer Client Handler: " + e.SafeToString(), stopwatch);
                }
            }
        }
    }
}
