using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using LamestWebserver.Synchronization;

using static LamestWebserver.Core.WorkerTask;

namespace LamestWebserver.Core
{
    /// <summary>
    /// Executes enqueued tasks using a fixed amount of worker threads.
    /// </summary>
    public class ThreadedWorker : NullCheckable
    {
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
                currentTask = null;
                bool hasTasks = false;

                using (_writeLock.LockRead()) // faster because multiple threads can read at the same time.
                    hasTasks = _tasks.Count > 0;

                if (hasTasks)
                {
                    using (_writeLock.LockWrite())
                        if (_tasks.Count > 0)
                            currentTask = _tasks.Dequeue();

                if (currentTask == null)
                {
                    Thread.Sleep(1);
                    continue;
                }

#if DEBUG
                Logger.LogTrace($"Dequeued Job in WorkerThread: {currentTask.Task.Method.Name} ({_tasks.Count} tasks left.)");
#endif

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

        /// <summary>
        /// Waits until multiple Workers have finished running.
        /// <para/>
        /// This Join will throw an Exception if raised inside the workers.
        /// </summary>
        /// <param name="workers">The workers to wait for.</param>
        public static void JoinTasks(params WorkerTask[] workers)
        {
            foreach (WorkerTask worker in workers)
                worker.Join();
        }

        /// <summary>
        /// Waits until multiple Workers have finished running.
        /// <para/>
        /// This Join will throw an Exception if raised inside the workers.
        /// </summary>
        /// <param name="maximumTotalWaitTime">The maximum total time to wait for the tasks to finish.</param>
        /// <param name="workers">The workers to wait for.</param>
        public static void JoinTasks(TimeSpan maximumTotalWaitTime, params WorkerTask[] workers)
        {
            DateTime start = DateTime.UtcNow;

            foreach (WorkerTask worker in workers)
            {
                TimeSpan ts = DateTime.UtcNow - start + maximumTotalWaitTime;

                if (ts > TimeSpan.Zero)
                {
                    worker.Join(ts);
                }
                else
                {
                    if ((from w in workers where w.State != ETaskState.Done select 0).Any())
                        throw new UnfinishedTaskException();
                }
            }

            return;
        }

        /// <summary>
        /// Waits until multiple workers have finished running.
        /// <para/>
        /// This Join will not throw Exceptions.
        /// </summary>
        /// <param name="workers">The workers to wait for.</param>
        /// <returns>Returns true if successfully joined all workers.</returns>
        public static bool JoinTasksSafe(params WorkerTask[] workers)
        {
            foreach (WorkerTask worker in workers)
                if (!worker.JoinSafe())
                    return false;

            return true;
        }


        /// <summary>
        /// Waits until multiple Workers have finished running.
        /// <para/>
        /// This Join will not throw Exceptions.
        /// </summary>
        /// <param name="maximumTotalWaitTime">The maximum total time to wait for the tasks to finish.</param>
        /// <param name="workers">The workers to wait for.</param>
        /// <returns>Returns true if successfully joined all workers.</returns>
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
        internal WorkerTask(Delegate task, params object[] parameters)
        {
            if (task == null)
                throw new ArgumentNullException(nameof(task));

            Task = task;
            Parameters = parameters;
        }

        /// <summary>
        /// The current Execution State of this task.
        /// </summary>
        public ETaskState State { get; internal set; } = ETaskState.Waiting;

        /// <summary>
        /// The returned value from the executed task - if any.
        /// </summary>
        public object ReturnedValue { get; internal set; } = null;

        /// <summary>
        /// The thrown exception from the executed task - if any.
        /// </summary>
        public Exception ExceptionThrown { get; internal set; } = null;

        /// <summary>
        /// Waits until the task has been executed.
        /// <para/>
        /// This Join will not throw Exceptions.
        /// </summary>
        /// <returns>Returns true if task was executed successfully.</returns>
        public bool JoinSafe()
        {
            object obj;

            return JoinSafe(out obj, null);
        }

        /// <summary>
        /// Waits until the task has been executed.
        /// <para/>
        /// This Join will not throw Exceptions.
        /// </summary>
        /// <param name="maximumWaitTime">The maximum amount of time to wait for the task to be executed.</param>
        /// <returns>Returns true if task was executed successfully.</returns>
        public bool JoinSafe(TimeSpan? maximumWaitTime)
        {
            object obj;

            return JoinSafe(out obj, maximumWaitTime);
        }

        /// <summary>
        /// Waits until the task has been executed.
        /// <para/>
        /// This Join will not throw Exceptions.
        /// </summary>
        /// <param name="milliseconds">The maximum amount of time to wait for the task to be executed in milliseconds.</param>
        /// <returns>Returns true if task was executed successfully.</returns>
        public bool JoinSafe(int milliseconds)
        {
            object obj;

            return JoinSafe(out obj, TimeSpan.FromMilliseconds(milliseconds));
        }

        /// <summary>
        /// Waits until the task has been executed.
        /// <para/>
        /// This Join will not throw Exceptions.
        /// </summary>
        /// <param name="returnedValue">The returned value - if any.</param>
        /// <param name="maximumWaitTime">The maximum amount of time to wait for the task to be executed.</param>
        /// <returns>Returns true if task was executed successfully.</returns>
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

        /// <summary>
        /// Waits until the task has been executed.
        /// <para/>
        /// This Join will not throw Exceptions.
        /// </summary>
        /// <typeparam name="T">The Type of the returned value to be casted into.</typeparam>
        /// <param name="returnedValue">The returned value casted to T.</param>
        /// <param name="maximumWaitTime">The maximum amount of time to wait for the task to be executed.</param>
        /// <returns>Returns true if task was executed successfully.</returns>
        public bool JoinSafe<T>(out T returnedValue, TimeSpan? maximumWaitTime = null)
        {
            object returnedValueObject;

            bool ret = JoinSafe(out returnedValueObject, maximumWaitTime);

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

        /// <summary>
        /// Waits until the task has been executed.
        /// <para/>
        /// This Join will not throw Exceptions.
        /// </summary>
        /// <typeparam name="T">The Type of the returned value to be casted into.</typeparam>
        /// <param name="maximumWaitTime">The maximum amount of time to wait for the task to be executed.</param>
        /// <returns>The returned value casted to T if successfull or null if it failed.</returns>
        public T JoinSafeOrNull<T>(TimeSpan? maximumWaitTime = null) where T : class
        {
            T ret;

            if (JoinSafe(out ret, maximumWaitTime))
                return ret;

            return null;
        }

        /// <summary>
        /// Waits until the task has been executed.
        /// <param/>
        /// This Join will throw an Exception if raised during execution.
        /// </summary>
        public void Join()
        {
            object obj;

            Join(out obj, null);
        }

        /// <summary>
        /// Waits until the task has been executed.
        /// <param/>
        /// This Join will throw an Exception if raised during execution.
        /// </summary>
        /// <param name="maximumWaitTime">The maximum amount of time to wait for the task to be executed.</param>
        public void Join(TimeSpan? maximumWaitTime)
        {
            object obj;

            Join(out obj, maximumWaitTime);
        }

        /// <summary>
        /// Waits until the task has been executed.
        /// <param/>
        /// This Join will throw an Exception if raised during execution.
        /// </summary>
        /// <param name="milliseconds">The maximum amount of time to wait for the task to be executed in milliseconds.</param>
        public void Join(int milliseconds)
        {
            object obj;

            Join(out obj, TimeSpan.FromMilliseconds(milliseconds));
        }

        /// <summary>
        /// Waits until the task has been executed.
        /// <param/>
        /// This Join will throw an Exception if raised during execution.
        /// </summary>
        /// <param name="returnedValue">The returned value - if any.</param>
        /// <param name="maximumWaitTime">The maximum amount of time to wait for the task to be executed.</param>
        public void Join(out object returnedValue, TimeSpan? maximumWaitTime = null)
        {
            DateTime startTime = DateTime.UtcNow;

            while (!maximumWaitTime.HasValue || startTime + maximumWaitTime.Value > DateTime.UtcNow)
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

        /// <summary>
        /// Waits until the task has been executed.
        /// <param/>
        /// This Join will throw an Exception if raised during execution.
        /// </summary>
        /// <typeparam name="T">The Type of the returned value to be casted into.</typeparam>
        /// <param name="returnedValue">The returned value casted to T.</param>
        /// <param name="maximumWaitTime">The maximum amount of time to wait for the task to be executed.</param>
        public void Join<T>(out T returnedValue, TimeSpan? maximumWaitTime = null)
        {
            object returnedValueObject;

            bool ret = JoinSafe(out returnedValueObject, maximumWaitTime);

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
        

        /// <summary>
        /// An enum containing the possible State Types of a WorkerTask.
        /// </summary>
        public enum ETaskState : byte
        {
            /// <summary>
            /// The Task has not been worked on yet.
            /// </summary>
            Waiting,

            /// <summary>
            /// The task is currently being executed.
            /// </summary>
            Executing,

            /// <summary>
            /// The task finished successfully.
            /// </summary>
            Done,

            /// <summary>
            /// The task execution raised an Exception.
            /// </summary>
            ExceptionThrown
        }
    }

    /// <summary>
    /// An exception to represent that the given task(s) could not be executed in the given timespan. 
    /// </summary>
    [Serializable]
    public class UnfinishedTaskException : Exception
    {
        /// <inheritdoc />
        public UnfinishedTaskException() : base("The given task could not be executed in the given timespan.")
        {

        }

        /// <inheritdoc />
        public UnfinishedTaskException(string message) : base(message)
        {

        }

        /// <inheritdoc />
        public UnfinishedTaskException(string message, Exception innerException) : base(message, innerException)
        {

        }
    }
}
