using System;
using System.Collections.Generic;
using System.Threading;
using LamestWebserver.Synchronization;

namespace LamestWebserver
{
    /// <summary>
    /// Executes enqueued tasks using a fixed amount of worker threads.
    /// </summary>
    public class ThreadedWorker
    {
        private static ThreadedWorker _currentWorker;
        private static UsableMutexSlim _currentWorkerMutex = new UsableMutexSlim();

        /// <summary>
        /// The ThreadedWorker used in the Server.
        /// </summary>
        public static ThreadedWorker CurrentWorker
        {
            get
            {
                using (_currentWorkerMutex.Lock())
                {
                    if (_currentWorker == null)
                        _currentWorker = new ThreadedWorker();

                    return _currentWorker;
                }
            }
        }

        /// <summary>
        /// The default Thread-count of a new Threaded Worker.
        /// </summary>
        public static uint DefaultWorkerCount = 10;

        /// <summary>
        /// The count of WorkerThreads for this ThreadedWorker.
        /// </summary>
        public readonly uint WorkerCount;

        private readonly Queue<WorkerTask> _tasks = new Queue<WorkerTask>();
        private readonly Thread[] _workers;

        private bool _running
        {
            get
            {
                _mutex.WaitOne();
                var ret = __running;
                _mutex.ReleaseMutex();
                return ret;
            }

            set
            {
                _mutex.WaitOne();
                __running = value;
                _mutex.ReleaseMutex();
            }
        }

        private bool __running = true;
        private readonly Mutex _mutex = new Mutex();

        /// <summary>
        /// Constructs a new ThreadedWorker with the default amount of WorkerThreads
        /// </summary>
        public ThreadedWorker() : this(DefaultWorkerCount)
        {
        }

        /// <summary>
        /// Constructs a new ThreadedWorker with a specific amount of WorkerThreads
        /// </summary>
        /// <param name="workerCount">the amount of WorkerThreads</param>
        public ThreadedWorker(uint workerCount)
        {
            ServerHandler.LogMessage($"Starting ThreadedWorker with {workerCount} WorkerThreads.");

            WorkerCount = workerCount;
            _running = true;
            _workers = new Thread[WorkerCount];

            for (int i = 0; i < workerCount; i++)
            {
                _workers[i] = new Thread(Work);
                _workers[i].Start();
            }
        }

        /// <summary>
        /// Enqueues a new Task to the ThreadedWorker pool
        /// </summary>
        /// <param name="task">the delegate to start</param>
        /// <param name="parameters">the parameters to start the delegate with</param>
        public void EnqueueJob(Delegate task, params object[] parameters)
        {
            WorkerTask wt = new WorkerTask(task, parameters);
            _mutex.WaitOne();
            _tasks.Enqueue(wt);
            _mutex.ReleaseMutex();
        }

        /// <summary>
        /// The amount of currently enqueued tasks
        /// </summary>
        public int TaskCount
        {
            get
            {
                _mutex.WaitOne();
                int ret = _tasks.Count;
                _mutex.ReleaseMutex();

                return ret;
            }
        }

        /// <summary>
        /// Stops all WorkerTasks
        /// </summary>
        public void Stop(int? timeout = 250)
        {
            _running = false;

            foreach (Thread worker in _workers)
            {
                try
                {
                    if (timeout.HasValue)
                    {
                        if (!worker.Join(timeout.Value))
                        {
                            try
                            {
                                ServerHandler.LogMessage("Worker Thread didn't complete in time. Forcing to quit.");
                                Master.ForceQuitThread(worker);
                            }
                            catch (Exception e)
                            {
                                ServerHandler.LogMessage("Failed to quit WorkerThread.\n" + e);
                            }
                        }
                    }
                    else
                    {
                        worker.Join();
                    }
                }
                catch
                {
                }
            }
        }

        /// <summary>
        /// Waits until all WorkerTasks are done
        /// </summary>
        public void Join(int? timeout = null)
        {
            while(TaskCount > 0)
                Thread.Sleep(1);

            _running = false;

            foreach (Thread worker in _workers)
            {
                try
                {
                    if (timeout.HasValue)
                    {
                        if (!worker.Join(timeout.Value))
                        {
                            try
                            {
                                ServerHandler.LogMessage("Worker Thread didn't complete in time. Forcing to quit.");
                                Master.ForceQuitThread(worker);
                            }
                            catch (Exception e)
                            {
                                ServerHandler.LogMessage("Failed to quit WorkerThread.\n" + e);
                            }
                        }
                    }
                    else
                    {
                        worker.Join();
                    }
                }
                catch
                {
                }
            }
        }

        private void Work()
        {
            WorkerTask currentTask = null;

            while (_running)
            {
                _mutex.WaitOne();

                if (_tasks.Count > 0)
                    currentTask = _tasks.Dequeue();

                _mutex.ReleaseMutex();

                if (currentTask == null)
                {
                    Thread.Sleep(1);
                    continue;
                }

                try
                {
                    currentTask.Task.DynamicInvoke(currentTask.Parameters);
                }
                catch (Exception e)
                {
                    ServerHandler.LogMessage("Exception in WorkerTask '" + currentTask.Task.Method.Name + "'.\n" + e);
                }
            }
        }
    }

    /// <summary>
    /// A WorkerTasks to be executed by ThreadedWorker's WorkerThreads.
    /// </summary>
    public class WorkerTask
    {
        /// <summary>
        /// The parameters to start the delegate with
        /// </summary>
        public readonly object[] Parameters;

        /// <summary>
        /// The delegate to start
        /// </summary>
        public readonly Delegate Task;

        /// <summary>
        /// Constructs a new WorkerTask
        /// </summary>
        /// <param name="task">the delegate to start</param>
        /// <param name="parameters">the parameters to execute on the delegate</param>
        public WorkerTask(Delegate task, params object[] parameters)
        {
            Task = task;
            Parameters = parameters;
        }
    }
}
