using LamestWebserver.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LamestWebserver.Synchronization
{
    /// <summary>
    /// Like a regular Mutex, but simplified, deadlock-safe (due to auto-releasing after a certain time) and disposable if Lock() is called, making it available in using statements.
    /// UsableLockSimple.Lock() is also available for sorted locking to prevent deadlocks.
    /// Attention: UsableLockSimple might retry actions if deadlocks occur or a certain operation takes to long. Keep that in mind.
    /// <para />
    /// Do not use this Synchronizer if you're experienced with multi-threading and expect it to work like a Mutex. This is a simplified Synchronizer to help unexperienced People to write multi-threaded applications.
    /// 
    /// <example>
    /// using (usableLockSimple.Lock())
    /// {
    ///     // Your Code.
    /// }
    /// </example>
    /// </summary>
    public sealed class UsableLockSimple
    {
        /// <summary>
        /// The milliseconds to wait for the mutex to be free.
        /// </summary>
        public static int MutexWaitMillis = 100;

        /// <summary>
        /// The milliseconds to wait for the mutex to expect a neverending method might have aquired it and just releasing it by yourself.
        /// </summary>
        public static int MutexSelfRelease = 200;

        private ReaderWriterLockSlim innerMutex;
        private ID ID = new ID();

        private Mutex helperMutex = new Mutex();
        private DateTime? lastLocked = null;

        /// <summary>
        /// Constructs a new UsableLockSimple.
        /// </summary>
        public UsableLockSimple()
        {
            innerMutex = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        }

        /// <summary>
        /// Checks if the lastLocked time is to far away, so that we should ignore the value of it and release the mutex
        /// </summary>
        private void HandleTimer()
        {
            helperMutex.WaitOne();

            if (lastLocked != null && (DateTime.Now - lastLocked.Value).TotalMilliseconds > MutexSelfRelease)
            {
                try
                {
                    innerMutex = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
                }
                catch (Exception)
                {
                    ServerHandler.LogMessage($"Usable Mutex '{ID}' was locked longer than {MutexSelfRelease} millis");
                }

                lastLocked = null;
            }

            helperMutex.ReleaseMutex();
        }

        /// <summary>
        /// Sets the current Time as lastLocked time if the execute value is true
        /// </summary>
        /// <param name="execute">is only executed if true - this value is passed through (like Mutex.WaitOne return value)</param>
        /// <returns>the execute value</returns>
        private bool StartTimer(bool execute = true)
        {
            if (execute)
            {
                helperMutex.WaitOne();

                lastLocked = DateTime.Now;

                helperMutex.ReleaseMutex();
            }

            return execute;
        }

        /// <summary>
        /// Sets the current Thread as not locked
        /// </summary>
        private void StopTimer()
        {
            helperMutex.WaitOne();

            lastLocked = null;

            helperMutex.ReleaseMutex();
        }

        /// <summary>
        /// Locks the mutex.
        /// </summary>
        /// <returns>true if the mutex could be locked in time</returns>
        public bool WaitOne()
        {
            HandleTimer();

            return StartTimer(innerMutex.TryEnterWriteLock(MutexWaitMillis));
        }

        /// <summary>
        /// Releases the mutex.
        /// </summary>
        public void ReleaseMutex()
        {
            try
            {
                innerMutex.ExitWriteLock();
            }
            catch (SynchronizationLockException)
            {
                ServerHandler.LogMessage($"The time-canceled Mutex '{ID}' has been released");
            }

            StopTimer();
        }

        /// <summary>
        /// Locks the innerMutex in a way, so that it can be used through a using statement (IDisposable)
        /// </summary>
        /// <returns></returns>
        public UsableMultiUsableMutexLocker Lock()
        {
            return new UsableMultiUsableMutexLocker(this);
        }

        /// <summary>
        /// Is used to lock especially multiple mutexes in sorted order to prevent deadlocks
        /// </summary>
        /// <param name="mutexes">the UsableLockSimple-s to lock</param>
        /// <returns>a UsableMutliMutexLocker, that already locked the given mutexes</returns>
        public static UsableMultiUsableMutexLocker Lock(params UsableLockSimple[] mutexes)
        {
            UsableLockSimple[] mut = new UsableLockSimple[mutexes.Length];

            for (int i = 0; i < mutexes.Length; i++)
            {
                int currentIndex = -1;

                for (int j = 0; j < mutexes.Length; j++)
                {
                    if (mut.Contains(mutexes[j]))
                        continue;

                    if (mut[i] == null)
                        currentIndex = j;
                    else if(mutexes[currentIndex].ID.CompareTo(mutexes[j].ID) < 0)
                        currentIndex = j;
                }

                mut[i] = mutexes[currentIndex];
            }

            return new UsableMultiUsableMutexLocker(mut.ToArray());
        }
    }

    /// <summary>
    /// A MutexLocker for multiple Mutexes
    /// </summary>
    public class UsableMultiMutexLocker : IDisposable
    {
        private readonly Mutex[] _mutexes;
        private readonly bool[] _locked;

        /// <summary>
        /// constructs a new UsableMultiMutexLocker and already locks all given mutexes.
        /// </summary>
        /// <param name="mutexes">the mutexes to lock</param>
        public UsableMultiMutexLocker(params Mutex[] mutexes)
        {
            this._mutexes = mutexes;
            this._locked = new bool[mutexes.Length];

            for (int i = 0; i < mutexes.Length; i++)
            {
                if (!mutexes[i].WaitOne(UsableLockSimple.MutexWaitMillis))
                {
                    Dispose();
                    throw new MutexRetryException();
                }

                _locked[i] = true;
            }
        }

        /// <summary>
        /// Releases all locked mutexes in opposite locking order.
        /// </summary>
        public void Dispose()
        {
            for (int i = _mutexes.Length - 1; i >= 0; i--)
            {
                if(_locked[i])
                    _mutexes[i].ReleaseMutex();
            }
        }
    }

    /// <summary>
    /// A MutexLocker for multiple UsableLockSimple-s
    /// </summary>
    public class UsableMultiUsableMutexLocker : IDisposable
    {
        private UsableLockSimple[] mutexes;
        private bool[] locked;

        /// <summary>
        /// constructs a new UsableMultiUsableMutexLocker and already locks all given mutexes.
        /// </summary>
        /// <param name="mutexes">the UsableLockSimple-s to lock</param>
        public UsableMultiUsableMutexLocker(params UsableLockSimple[] mutexes)
        {
            this.mutexes = mutexes;
            this.locked = new bool[mutexes.Length];

            for (int i = 0; i < mutexes.Length; i++)
            {
                if (!mutexes[i].WaitOne())
                {
                    Dispose();
                    throw new MutexRetryException();
                }

                locked[i] = true;
            }
        }

        /// <summary>
        /// Releases all locked UsableLockSimple-s in opposite locking order.
        /// </summary>
        public void Dispose()
        {
            for (int i = mutexes.Length - 1; i >= 0; i--)
            {
                if (locked[i])
                    mutexes[i].ReleaseMutex();
            }
        }
    }

    /// <summary>
    /// This exception symbolizes, that a mutex could not be aquired in time and the Operation has been aborted.
    /// </summary>
    public class MutexRetryException : Exception
    {
    }
}
