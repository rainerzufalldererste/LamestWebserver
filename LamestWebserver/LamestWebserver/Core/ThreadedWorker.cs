using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using LamestWebserver.Synchronization;

namespace LamestWebserver.Core
{
    /// <summary>
    /// Executes enqueued tasks using a fixed amount of worker threads.
    /// </summary>
    public class ThreadedWorker : NullCheckable
    {
        private static ThreadedWorker _currentWorker;

        /// <summary>
        /// The ThreadedWorker used in the Server.
        /// </summary>
        public static readonly Singleton<ThreadedWorker> CurrentWorker = new Singleton<ThreadedWorker>();

        /// <summary>
        /// The default Thread-count of a new Threaded Worker.
        /// </summary>
        public static uint DefaultWorkerCount = (uint)Environment.ProcessorCount;

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
                using (_writeLock.LockRead())
                    return __running;
            }

            set
            {
                using (_writeLock.LockWrite())
                    __running = value;
            }
        }

        private bool __running = true;
        private readonly UsableWriteLock _writeLock = new UsableWriteLock();

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
        /// <returns>The WorkerTask object for this Job.</returns>
        public WorkerTask EnqueueJob(Delegate task, params object[] parameters)
        {
            if (task == null)
                throw new ArgumentNullException(nameof(task));

            WorkerTask ret = new WorkerTask(task, parameters);

            using (_writeLock.LockWrite())
                _tasks.Enqueue(ret);

            return ret;
        }

        /// <summary>
        /// The amount of currently enqueued tasks
        /// </summary>
        public int TaskCount
        {
            get
            {
                using (_writeLock.LockRead())
                    return _tasks.Count;
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
            while (TaskCount > 0)
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
                bool hasTasks = false;

                using (_writeLock.LockRead())
                    hasTasks = _tasks.Count > 0;

                if (hasTasks)
                    using (_writeLock.LockWrite())
                        currentTask = _tasks.Dequeue();

                if (currentTask == null)
                {
                    Thread.Sleep(1);
                    continue;
                }

                try
                {
                    currentTask.State = ETaskState.Executing;

                    object returnedValue = currentTask.Task.DynamicInvoke(currentTask.Parameters);

                    currentTask.ReturnedValue = returnedValue;
                    currentTask.State = ETaskState.Done;
                }
                catch (Exception e)
                {
                    currentTask.ExceptionThrown = e;
                    currentTask.State = ETaskState.ExceptionThrown;

                    ServerHandler.LogMessage("Exception in WorkerTask '" + currentTask.Task.Method.Name + "'.\n" + e);
                }
            }
        }

        public static void JoinTasks(params WorkerTask[] workers)
        {
            foreach (WorkerTask worker in workers)
                worker.Join();
        }

        public static bool JoinTasks(TimeSpan maximumTotalWaitTime, params WorkerTask[] workers)
        {
            DateTime start = DateTime.UtcNow;

            foreach (WorkerTask worker in workers)
            {
                TimeSpan ts = DateTime.UtcNow - start + maximumTotalWaitTime;

                if (ts > TimeSpan.Zero)
                {
                    if (!worker.JoinSafe(ts))
                        return false;
                }
                else
                {
                    return !(from w in workers where w.State != ETaskState.Done select 0).Any();
                }
            }

            return true;
        }

        public static bool JoinTasksSafe(params WorkerTask[] workers)
        {
            foreach (WorkerTask worker in workers)
                if (!worker.JoinSafe())
                    return false;

            return true;
        }

        public static bool JoinTasksSafe(TimeSpan maximumTotalWaitTime, params WorkerTask[] workers)
        {
            DateTime start = DateTime.UtcNow;

            foreach (WorkerTask worker in workers)
            {
                TimeSpan ts = DateTime.UtcNow - start + maximumTotalWaitTime;

                if (ts > TimeSpan.Zero)
                {
                    if (!worker.JoinSafe(ts))
                        return false;
                }
                else
                {
                    return !(from w in workers where w.State != ETaskState.Done select 0).Any();
                }
            }

            return true;
        }
    }

    public enum ETaskState : byte
    {
        Waiting,
        Executing,
        Done,
        ExceptionThrown
    }

    /// <summary>
    /// A WorkerTasks to be executed by ThreadedWorker's WorkerThreads.
    /// </summary>
    public class WorkerTask : NullCheckable
    {
        /// <summary>
        /// The parameters to start the delegate with
        /// </summary>
        internal readonly object[] Parameters;

        /// <summary>
        /// The delegate to start
        /// </summary>
        internal readonly Delegate Task;

        /// <summary>
        /// Constructs a new WorkerTask
        /// </summary>
        /// <param name="task">the delegate to start</param>
        /// <param name="parameters">the parameters to execute on the delegate</param>
        public WorkerTask(Delegate task, params object[] parameters)
        {
            if (task == null)
                throw new ArgumentNullException(nameof(task));

            Task = task;
            Parameters = parameters;
        }

        public ETaskState State { get; internal set; } = ETaskState.Waiting;

        public object ReturnedValue { get; internal set; } = null;

        public Exception ExceptionThrown { get; internal set; } = null;

        public bool JoinSafe() => JoinSafe(out object obj, null);

        public bool JoinSafe(TimeSpan? maximumWaitTime) => JoinSafe(out object obj, maximumWaitTime);

        public bool JoinSafe(int milliseconds) => JoinSafe(out object obj, TimeSpan.FromMilliseconds(milliseconds));

        public bool JoinSafe(out object returnedValue, TimeSpan? maximumWaitTime = null)
        {
            DateTime startTime = DateTime.UtcNow;

            while (maximumWaitTime.HasValue || startTime + maximumWaitTime.Value > DateTime.UtcNow)
            {
                switch (State)
                {
                    case ETaskState.Waiting:
                    case ETaskState.Executing:
                        Thread.Sleep(1);
                        break;

                    case ETaskState.Done:
                        returnedValue = ReturnedValue;
                        return true;

                    case ETaskState.ExceptionThrown:
                        returnedValue = null;
                        return false;
                }
            }

            returnedValue = null;
            return false;
        }

        public bool JoinSafe<T>(out T returnedValue, TimeSpan? maximumWaitTime = null)
        {
            bool ret = JoinSafe(out object returnedValueObject, maximumWaitTime);

            if (ret)
            {
                if (returnedValueObject is T)
                {
                    returnedValue = (T)returnedValueObject;
                    return true;
                }
                else
                {
                    ExceptionThrown = new InvalidCastException($"Cannot cast '{returnedValueObject.GetType().FullName}' to '{typeof(T).FullName}'.");
                    State = ETaskState.ExceptionThrown;
                }
            }

            returnedValue = default(T);
            return false;
        }

        public T JoinSafeOrNull<T>(TimeSpan? maximumWaitTime = null) where T : class
        {
            if (JoinSafe(out T ret, maximumWaitTime))
                return ret;

            return null;
        }

        public void Join() => Join(out object obj, null);

        public void Join(TimeSpan? maximumWaitTime) => Join(out object obj, maximumWaitTime);

        public void Join(int milliseconds) => Join(out object obj, TimeSpan.FromMilliseconds(milliseconds));

        public void Join(out object returnedValue, TimeSpan? maximumWaitTime = null)
        {
            DateTime startTime = DateTime.UtcNow;

            while (maximumWaitTime.HasValue || startTime + maximumWaitTime.Value > DateTime.UtcNow)
            {
                switch (State)
                {
                    case ETaskState.Waiting:
                    case ETaskState.Executing:
                        Thread.Sleep(1);
                        break;

                    case ETaskState.Done:
                        returnedValue = ReturnedValue;
                        return;

                    case ETaskState.ExceptionThrown:
                        throw ExceptionThrown;
                }
            }

            throw new UnfinishedTaskException();
        }

        public void Join<T>(out T returnedValue, TimeSpan? maximumWaitTime = null)
        {
            bool ret = JoinSafe(out object returnedValueObject, maximumWaitTime);

            if (ret)
            {
                if (returnedValueObject is T)
                {
                    returnedValue = (T)returnedValueObject;
                    return;
                }
                else
                {
                    State = ETaskState.ExceptionThrown;
                    ExceptionThrown = new InvalidCastException($"Cannot cast '{returnedValueObject.GetType().FullName}' to '{typeof(T).FullName}'.");

                    throw ExceptionThrown;
                }
            }

            throw new UnfinishedTaskException();
        }
    }

    [Serializable]
    public class UnfinishedTaskException : Exception
    {
        public UnfinishedTaskException() : base("The given task could not be executed in time.")
        {

        }
        
        public UnfinishedTaskException(string message) : base(message)
        {

        }

        public UnfinishedTaskException(string message, Exception innerException) : base(message, innerException)
        {

        }
    }
}
