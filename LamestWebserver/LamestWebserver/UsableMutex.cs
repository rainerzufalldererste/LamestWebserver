using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LamestWebserver
{
    /// <summary>
    /// A regular Mutex, but disposable if Lock() is called, making it available in using statements
    /// UsableMutex.Lock() is also available for sorted locking to prevent deadlocks.
    /// 
    /// <example>
    /// using (usableMutex.Lock())
    /// {
    ///     // Your Code.
    /// }
    /// </example>
    /// </summary>
    public class UsableMutex
    {
        private Mutex innerMutex;
        private readonly string ID = SessionContainer.generateHash();

        public UsableMutex()
        {
            innerMutex = new Mutex();
        }

        public UsableMutex(bool initiallyOwned)
        {
            innerMutex = new Mutex(initiallyOwned);
        }

        public UsableMutex(bool initiallyOwned, string name)
        {
            innerMutex = new Mutex(initiallyOwned, name);
        }

        public UsableMutex(bool initiallyOwned, string name, out bool createdNew)
        {
            innerMutex = new Mutex(initiallyOwned, name, out createdNew);
        }

        public UsableMutex(bool initiallyOwned, string name, out bool createdNew, System.Security.AccessControl.MutexSecurity mutexSecurity)
        {
            innerMutex = new Mutex(initiallyOwned, name, out createdNew, mutexSecurity);
        }

        public bool WaitOne()
        {
            return innerMutex.WaitOne();
        }

        public bool WaitOne(int millisecondsTimeout)
        {
            return innerMutex.WaitOne(millisecondsTimeout);
        }

        public bool WaitOne(TimeSpan timeout)
        {
            return innerMutex.WaitOne(timeout);
        }

        public bool WaitOne(int millisecondsTimeout, bool exitContext)
        {
            return innerMutex.WaitOne(millisecondsTimeout, exitContext);
        }

        public bool WaitOne(TimeSpan timeout, bool exitContext)
        {
            return innerMutex.WaitOne(timeout, exitContext);
        }

        public void ReleaseMutex()
        {
            innerMutex.ReleaseMutex();
        }

        /// <summary>
        /// Locks the innerMutex in a way, so that it can be used through a using statement (IDisposable)
        /// </summary>
        /// <returns></returns>
        public UsableMutexLocker Lock()
        {
            return new UsableMutexLocker(innerMutex);
        }

        /// <summary>
        /// Is used to lock especially multiple mutexes in sorted order to prevent deadlocks
        /// </summary>
        /// <param name="mutexes">the usablemutexes to lock</param>
        /// <returns>a UsableMutliMutexLocker, that already locked the given mutexes</returns>
        public static UsableMultiMutexLocker Lock(params UsableMutex[] mutexes)
        {
            Mutex[] mut = new Mutex[mutexes.Length];

            for (int i = 0; i < mutexes.Length; i++)
            {
                int currentIndex = -1;

                for (int j = 0; j < mutexes.Length; j++)
                {
                    if (mut.Contains(mutexes[j].innerMutex))
                        continue;

                    if (mut[i] == null)
                        currentIndex = j;
                    else if(mutexes[currentIndex].ID.CompareTo(mutexes[j].ID) < 0)
                        currentIndex = j;
                }

                mut[i] = mutexes[currentIndex].innerMutex;
            }

            return new UsableMultiMutexLocker(mut.ToArray());
        }
    }

    /// <summary>
    /// A MutexLocker for multiple Mutexes
    /// </summary>
    public class UsableMultiMutexLocker : IDisposable
    {
        private Mutex[] mutexes;
        private bool[] locked;

        /// <summary>
        /// constructs a new UsableMultiMutexLocker and already locks all given mutexes.
        /// </summary>
        /// <param name="mutexes">the mutexes to lock</param>
        public UsableMultiMutexLocker(params Mutex[] mutexes)
        {
            this.mutexes = mutexes;
            this.locked = new bool[mutexes.Length];

            for (int i = 0; i < mutexes.Length; i++)
            {
                if (!mutexes[i].WaitOne(100))
                {
                    Dispose();
                    throw new MutexRetryException();
                }

                locked[i] = true;
            }
        }

        /// <summary>
        /// Releases all locked mutexes in opposite locking order.
        /// </summary>
        public void Dispose()
        {
            for (int i = mutexes.Length - 1; i >= 0; i--)
            {
                if(locked[i])
                    mutexes[i].ReleaseMutex();
            }
        }
    }

    internal class MutexRetryException : Exception
    {
    }
}
