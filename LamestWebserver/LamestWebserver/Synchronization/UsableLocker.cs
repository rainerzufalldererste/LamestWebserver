using System;
using System.Threading;

namespace LamestWebserver.Synchronization
{
    /// <summary>
    /// Locks and Releases a ILockable object using IDisposable.
    /// </summary>
    public class UsableLocker : IDisposable
    {
        private readonly ILockable obj;

        /// <summary>
        /// Constructs a new UsableLocker
        /// </summary>
        /// <param name="obj">the ILockable object</param>
        public UsableLocker(ILockable obj)
        {
            this.obj = obj;
            obj.Mutex.WaitOne();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            obj.Mutex.ReleaseMutex();
        }

        /// <summary>
        /// Executes code locking and releasing a given mutex before and after and passes exceptions through this behaviour.
        /// </summary>
        /// <param name="obj">the ILockable object</param>
        /// <param name="action">the code to execute</param>
        public static void TryLock(ILockable obj, Action action)
        {
            obj.Mutex.WaitOne();
            
            try
            {
                action();
            }
            catch(Exception e)
            {
                obj.Mutex.ReleaseMutex();
                throw new Exception(e.Message, e);
            }

            obj.Mutex.ReleaseMutex();
        }
    }

    /// <summary>
    /// Locks and Releases a Mutex via IDisposable.
    /// </summary>
    public class UsableMutexLocker : IDisposable
    {
        private readonly Mutex _mutex;
        private readonly bool _locked = false;

        /// <summary>
        /// Creates a new UsableMutexLocker.
        /// </summary>
        /// <param name="mutex">the mutex to lock</param>
        public UsableMutexLocker(Mutex mutex)
        {
            this._mutex = mutex;

            if(!mutex.WaitOne(100))
                throw new MutexRetryException();

            _locked = true;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_locked)
                _mutex.ReleaseMutex();
        }

        /// <summary>
        /// Executes code locking and releasing a given mutex before and after and passes exceptions through this behaviour.
        /// </summary>
        /// <param name="mutex">the mutex</param>
        /// <param name="action">the code to execute</param>
        public static void TryLock(Mutex mutex, Action action)
        {
            mutex.WaitOne();

            try
            {
                action();
            }
            catch(Exception e)
            {
                mutex.ReleaseMutex();
                throw new Exception(e.Message, e);
            }

            mutex.ReleaseMutex();
        }
    }

    /// <summary>
    /// A Lockable object
    /// </summary>
    public interface ILockable
    {
        /// <summary>
        /// the mutex that is locked.
        /// </summary>
        Mutex Mutex { get; }
    }
}
