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
    /// <summary>
    /// A ServerCore provides functionality for accepting Connections on a TCP port.
    /// </summary>
    public abstract class ServerCore : IDisposable
    {
        /// <summary>
        /// Defines the handling of Server Threads.
        /// </summary>
        public enum EThreadingType
        {
            /// <summary>
            /// A fixed pool of threads will execute tasks.
            /// </summary>
            WorkerThreads,

            /// <summary>
            /// Tasks will be launched as separate threads.
            /// </summary>
            ThreadSpawner
        }

        internal readonly Singleton<ThreadedWorker> WorkerThreads = new Singleton<ThreadedWorker>(() => new ThreadedWorker());
        internal readonly SynchronizedValue<bool> Running = new SynchronizedValue<bool>(true);

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

        /// <summary>
        /// Defines the behavior of this Server when executing tasks.
        /// </summary>
        public readonly EThreadingType ThreadingType = EThreadingType.WorkerThreads;
        
        private readonly TcpListener _tcpListener;
        private readonly Thread _tcpListenerThread;
        private readonly int _readTimeout = DefaultReadTimeout;
        private readonly UsableMutexSlim _networkStreamsMutex = new UsableMutexSlim();
        private readonly AVLTree<int, Stream> _streams = new AVLTree<int, Stream>();
        private Task<TcpClient> _tcpReceiveTask;
        private SynchronizedCollection<Thread, List<Thread>> _clientHandlerThreads = new SynchronizedCollection<Thread, List<Thread>>();

        /// <summary>
        /// Creates a new Server that is set-up to listen to a specified TCP port. Call `Start` to start the listener thread.
        /// </summary>
        /// <param name="port">The port to listen on.</param>
        public ServerCore(int port) : this(IPAddress.Any, port) { }

        /// <summary>
        /// Creates a new Server that is set-up to listen to a specified TCP port on a specified IPAddress. Call `Start` to start the listener thread.
        /// </summary>
        /// <param name="localAddress">The local address to listen on.</param>
        /// <param name="port">The port to listen on.</param>
        /// <param name="threadingType">The threading behaviour of this server.</param>
        public ServerCore(IPAddress localAddress, int port, EThreadingType threadingType = EThreadingType.WorkerThreads)
        {
            if (!TcpPortIsUnused(port))
                Logger.LogExcept(new InvalidOperationException($"The TCP port {port} is currently used by another application."));

            Port = port;
            _tcpListener = new TcpListener(localAddress, port);
            ThreadingType = threadingType;

            switch (ThreadingType)
            {
                case EThreadingType.ThreadSpawner:
                    _tcpListenerThread = new Thread(HandleTcpListenerThreadSpawn);
                    break;

                case EThreadingType.WorkerThreads:
                default:
                _tcpListenerThread = new Thread(HandleTcpListener);
                    break;
            }
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

            // Give the `_tcpListenerThread` Thread time to finish.
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

            using (_networkStreamsMutex.Lock())
            {
                foreach (KeyValuePair<int, Stream> stream in _streams)
                {
                    try
                    {
                        stream.Value.Close();
                    }
                    catch { }
                }
            }

            foreach (var thread in _clientHandlerThreads)
            {
                if(thread.ThreadState != ThreadState.Stopped)
                {
                    try
                    {
                        Master.ForceQuitThread(thread);
                    }
                    catch { }
                }
            }

            WorkerThreads.Instance.Stop(ServerShutdownClientHandlerForceQuitTimeout);
        }

        /// <summary>
        /// Starts the TCP Listener Thread.
        /// </summary>
        protected virtual void Start()
        {
            if(_tcpListenerThread.ThreadState == ThreadState.Unstarted)
                _tcpListenerThread.Start();
        }

        internal void ClearStreams()
        {
            using (_networkStreamsMutex.Lock())
            {
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
            }
        }

        private TcpClient AcceptClient()
        {
            _tcpReceiveTask = _tcpListener.AcceptTcpClientAsync();
            _tcpReceiveTask.Wait();

            TcpClient tcpClient = _tcpReceiveTask.Result;
            tcpClient.ReceiveTimeout = _readTimeout;
            tcpClient.NoDelay = true;

            Logger.LogTrace("Client Connected: " + tcpClient.Client.RemoteEndPoint.ToString());

            return tcpClient;
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
                Logger.LogExcept("The TcpListener couldn't be started. The Port is probably blocked.", e);

                return;
            }

            while (Running)
            {
                try
                {
                    TcpClient tcpClient = AcceptClient();

                    WorkerThreads.Instance.EnqueueJob((Action<object>)CallHandleClient, tcpClient);

                    Logger.LogTrace("Enqueued Client Handler for " + tcpClient.Client.RemoteEndPoint.ToString() + ".");
                }
                catch (ThreadAbortException)
                {
                    break;
                }
                catch (Exception e)
                {
                    Logger.LogExcept("The TcpListener failed.", e);
                }
            }
        }

        private void HandleTcpListenerThreadSpawn()
        {
            try
            {
                _tcpListener.Start();
                Logger.LogInformation($"{nameof(WebServer)} {nameof(TcpListener)} successfully started on port {Port}.");
            }
            catch (Exception e)
            {
                Logger.LogExcept("The TcpListener couldn't be started. The Port is probably blocked.", e);

                return;
            }

            while (Running)
            {
                try
                {
                    TcpClient tcpClient = AcceptClient();
                    Thread thread = new Thread(new ParameterizedThreadStart(CallHandleClient));
                    _clientHandlerThreads.Add(thread);
                    thread.Start(tcpClient);
                }
                catch (ThreadAbortException)
                {
                    break;
                }
                catch (Exception e)
                {
                    Logger.LogExcept("The TcpListener failed.", e);
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

                if (ThreadingType != EThreadingType.WorkerThreads)
                    CurrentThreadStream = null;

                if (ThreadingType == EThreadingType.ThreadSpawner)
                    _clientHandlerThreads.Remove(Thread.CurrentThread);

            }
        }

        /// <summary>
        /// Handles client interactions.
        /// </summary>
        /// <param name="tcpClient">The TcpClient of the client.</param>
        /// <param name="networkStream">The networkStream of the connection to the client.</param>
        protected abstract void HandleClient(TcpClient tcpClient, NetworkStream networkStream);

        /// <summary>
        /// Gets or sets the current stream for the active thread.
        /// </summary>
        protected Stream CurrentThreadStream
        {
            get
            {
                using (_networkStreamsMutex.Lock())
                    return _streams[Thread.CurrentThread.ManagedThreadId];
            }

            set
            {
                using (_networkStreamsMutex.Lock())
                {
                    if (value == null)
                    {
                        _streams.Remove(Thread.CurrentThread.ManagedThreadId);
                    }
                    else
                    {
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
                    }
                }
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
