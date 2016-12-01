using System;
using System.Threading;

namespace LamestWebserver
{
    public class UsableLocker : IDisposable
    {
        private ILockable obj;

        public UsableLocker(ILockable obj)
        {
            this.obj = obj;
            obj.Mutex.WaitOne();
        }

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

    public class UsableMutexLocker : IDisposable
    {
        private Mutex mutex;

        public UsableMutexLocker(Mutex mutex)
        {
            this.mutex = mutex;
            mutex.WaitOne();
        }

        public void Dispose()
        {
            mutex.ReleaseMutex();
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

    public interface ILockable
    {
        Mutex Mutex { get; }
    }
}
