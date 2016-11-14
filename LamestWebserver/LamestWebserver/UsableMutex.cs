using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// A regular Mutex, but disposable if Lock() is called.
/// This let's you use it within a using statement
/// 
/// Syntax:
/// 
/// using (usableMutex.Lock())
/// {
///     // Your Code.
/// }
/// 
/// </summary>
namespace LamestWebserver
{
    /// <summary>
    /// Wrapper for a Mutex.
    /// Supports Lock(), an operation to help using mutexes throug "using" statements and UsableMutex.Lock() for sorted locking to prevent deadlocks.
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

        /// <summary>
        /// constructs a new UsableMultiMutexLocker and already locks all given mutexes.
        /// </summary>
        /// <param name="mutexes">the mutexes to lock</param>
        public UsableMultiMutexLocker(params Mutex[] mutexes)
        {
            this.mutexes = mutexes;

            for (int i = 0; i < mutexes.Length; i++)
            {
                mutexes[i].WaitOne();
            }
        }

        /// <summary>
        /// Releases all locked mutexes in opposite locking order.
        /// </summary>
        public void Dispose()
        {
            for (int i = mutexes.Length - 1; i >= 0; i--)
            {
                mutexes[i].ReleaseMutex();
            }
        }
    }
}
