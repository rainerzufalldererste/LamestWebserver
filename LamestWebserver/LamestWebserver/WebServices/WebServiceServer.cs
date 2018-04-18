using LamestWebserver.Core;
using LamestWebserver.Serialization;
using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace LamestWebserver.WebServices
{
    public class WebServiceServer : ServerCore
    {
        public readonly WebServiceHandler RequestHandler;

        /// <summary>
        /// The size that is read from the networkStream for each request.
        /// </summary>
        public static int RequestMaxPacketSize = 1024 * 128;

        public WebServiceServer(WebServiceHandler webRequestHandler, int port = 8310) : base(port)
        {
            if (webRequestHandler == null)
                throw new NullReferenceException(nameof(webRequestHandler));
            
            RequestHandler = webRequestHandler;
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
