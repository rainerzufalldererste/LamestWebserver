using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using LamestWebserver.Collections;
using LamestWebserver.Core;
using LamestWebserver.Synchronization;

namespace LamestWebserver
{
    public abstract class ServerCore : IDisposable
    {
        internal readonly Singleton<ThreadedWorker> WorkerThreads = new Singleton<ThreadedWorker>(() => new ThreadedWorker());
        
        /// <summary>
        /// The default time to wait until a NetworkStream is being closed because of a no data coming in.
        /// </summary>
        public static int DefaultReadTimeout = 5000;

        /// <summary>
        /// When stopping the Server the timeout to use before quiting the worker threads forcefully.
        /// </summary>
        public static int? ServerShutdownClientHandlerForceQuitTimeout = null;

        /// <summary>
        /// The Port, the server is listening at.
        /// </summary>
        public readonly int Port;

        internal readonly SynchronizedValue<bool> Running = new SynchronizedValue<bool>(true);

        private readonly TcpListener _tcpListener;
        private readonly Thread _tcpListenerThread;
        private readonly int _readTimeout = DefaultReadTimeout;
        private readonly Mutex _networkStreamsMutex = new Mutex();
        private readonly AVLTree<int, Stream> _streams = new AVLTree<int, Stream>();
        private Task<TcpClient> _tcpReceiveTask;

        public ServerCore(int port)
        {
            this.Port = port;
            this._tcpListener = new TcpListener(IPAddress.Any, port);

            _tcpListenerThread = new Thread(new ThreadStart(HandleTcpListener));
        }

        /// <inheritdoc />
        ~ServerCore()
        {
            try
            {
                Stop();
            }
            catch { }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Stop();
        }

        /// <summary>
        /// Stops the Server from running.
        /// </summary>
        public virtual void Stop()
        {
            Running.Value = false;

            try
            {
                _tcpListener.Stop();
            }
            catch { }

            // Give the _mThread Thread time to finish.
            Thread.Sleep(1);

            if (_tcpListenerThread.ThreadState != ThreadState.Stopped)
            {
                try
                {
                    Master.ForceQuitThread(_tcpListenerThread);
                }
                catch { }
            }

            try
            {
                _tcpReceiveTask.Dispose();
            }
            catch { }

            _networkStreamsMutex.WaitOne();

            foreach (KeyValuePair<int, Stream> stream in _streams)
            {
                try
                {
                    stream.Value.Close();
                }
                catch { }
            }

            _networkStreamsMutex.ReleaseMutex();

            WorkerThreads.Instance.Stop(ServerShutdownClientHandlerForceQuitTimeout);
        }

        protected virtual void Start()
        {
            _tcpListenerThread.Start();
        }

        internal void ClearStreams()
        {
            _networkStreamsMutex.WaitOne();

            foreach (Stream stream in _streams.Values)
            {
                if (stream != null)
                {
                    try
                    {
                        stream.Close();
                    }
                    catch { }
                }
            }

            _streams.Clear();

            _networkStreamsMutex.ReleaseMutex();
        }

        private void HandleTcpListener()
        {
            try
            {
                _tcpListener.Start();
                Logger.LogInformation($"{nameof(WebServer)} {nameof(TcpListener)} successfully started on port {Port}.");
            }
            catch (Exception e)
            {
                Logger.LogDebugExcept("The TcpListener couldn't be started. The Port is probably blocked.", e);

                return;
            }

            while (Running)
            {
                try
                {
                    _tcpReceiveTask = _tcpListener.AcceptTcpClientAsync();
                    _tcpReceiveTask.Wait();
                    TcpClient tcpClient = _tcpReceiveTask.Result;
                    tcpClient.ReceiveTimeout = _readTimeout;
                    tcpClient.NoDelay = true;
                    Logger.LogTrace("Client Connected: " + tcpClient.Client.RemoteEndPoint.ToString());

                    WorkerThreads.Instance.EnqueueJob((Action)(() => { CallHandleClient(tcpClient); }));
                    Logger.LogTrace("Enqueued Client Handler for " + tcpClient.Client.RemoteEndPoint.ToString() + ".");
                }
                catch (ThreadAbortException)
                {
                    break;
                }
                catch (Exception e)
                {
                    Logger.LogDebugExcept("The TcpListener failed.", e);
                }
            }
        }

        private void CallHandleClient(object obj)
        {
            if (obj == null)
                Logger.LogExcept(new NullReferenceException(nameof(obj)));

            if (!(obj is TcpClient))
                Logger.LogExcept($"'{nameof(CallHandleClient)}' was called with an object of incorrect Type. '{nameof(TcpClient)}' expected.");

            TcpClient tcpClient = (TcpClient)obj;
            NetworkStream networkStream = tcpClient.GetStream();

            try
            {
                CurrentThreadStream = networkStream;
                HandleClient(tcpClient, networkStream);
            }
            catch (ThreadAbortException)
            {
            }
            catch (Exception e)
            {
                try
                {
                    Logger.LogExcept(e);
                }
                catch { }
            }
            finally
            {
                try
                {
                    CurrentThreadStream?.Close();
                }
                catch { }
            }
        }

        protected abstract void HandleClient(TcpClient tcpClient, NetworkStream networkStream);

        /// <summary>
        /// 
        /// </summary>
        protected Stream CurrentThreadStream
        {
            get
            {
                _networkStreamsMutex.WaitOne();
                Stream ret = _streams[Thread.CurrentThread.ManagedThreadId];
                _networkStreamsMutex.ReleaseMutex();

                return ret;
            }

            set
            {
                _networkStreamsMutex.WaitOne();

                Stream lastStream = _streams[Thread.CurrentThread.ManagedThreadId];

                if (lastStream != null)
                {
                    try
                    {
                        lastStream.Dispose();
                    }
                    catch { };
                }

                _streams.Add(Thread.CurrentThread.ManagedThreadId, value);
                _networkStreamsMutex.ReleaseMutex();
            }
        }

        /// <summary>
        /// Source: http://stackoverflow.com/questions/570098/in-c-how-to-check-if-a-tcp-port-is-available
        /// </summary>
        /// <param name="port">The TCP-Port to check for</param>
        /// <returns>true if unused</returns>
        public static bool TcpPortIsUnused(int port)
        {
            // Evaluate current system tcp connections. This is the same information provided
            // by the netstat command line application, just in .Net strongly-typed object
            // form.  We will look through the list, and if our port we would like to use
            // in our TcpClient is occupied, we will set isAvailable to false.
            System.Net.NetworkInformation.IPGlobalProperties ipGlobalProperties = System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties();
            IPEndPoint[] tcpConnInfoArray = ipGlobalProperties.GetActiveTcpListeners();

            foreach (IPEndPoint endpoint in tcpConnInfoArray)
            {
                if (endpoint.Port == port)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
