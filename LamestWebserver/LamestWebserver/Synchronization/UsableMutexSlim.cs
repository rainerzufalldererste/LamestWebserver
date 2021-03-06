﻿using System;
using System.Threading;

namespace LamestWebserver.Synchronization
{
    /// <summary>
    /// Just a simple UsableMutex with no handling for deadlocks - Only to use Mutexes with IDisposable.
    /// </summary>
    public sealed class UsableMutexSlim
    {
        private readonly Mutex _innerMutex = new Mutex();
        private UsableSlimMutexLocker _locker;

        /// <summary>
        /// Locks the mutex; IDisposable.
        /// </summary>
        /// <returns>an IDisposable object that releases the mutex on Dispose()</returns>
        public UsableSlimMutexLocker Lock()
        {
            if (_locker == null)
                _locker = new UsableSlimMutexLocker(this);
            else
                _locker.ReLock();

            return _locker;
        }

        /// <summary>
        /// Just a simple IDisposable Mutex lock/release.
        /// </summary>
        public class UsableSlimMutexLocker : IDisposable
        {
            private readonly UsableMutexSlim _innerMutex;

            internal UsableSlimMutexLocker(UsableMutexSlim innerMutex)
            {
                this._innerMutex = innerMutex;
                innerMutex._innerMutex.WaitOne();
            }

            /// <summary>
            /// Locks the internal Mutex again.
            /// </summary>
            internal void ReLock()
            {
                _innerMutex._innerMutex.WaitOne();
            }

            /// <inheritdoc />
            public void Dispose()
            {
                _innerMutex._innerMutex.ReleaseMutex();
            }
        }
    }
}
