using System;
using System.Linq;
using System.Threading;

namespace LamestWebserver
{
    /// <summary>
    /// A safe and fast way to read and write from shared ressources without blocking everything.
    /// </summary>
    public class UsableWriteLock
    {
        private uint readCounter = 0;
        private Mutex readMutex = new Mutex();
        private SemaphoreSlim writeSemaphore = new SemaphoreSlim(1, 1);
        private readonly string ID = SessionContainer.generateHash();
        private uint writePending = 0;
        private SemaphoreSlim writePendingSemaphore = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Locks the WriteLock for reading
        /// </summary>
        /// <returns>An IDisposable Object to be used in a using statement</returns>
        public UsableWriteLockDisposable_read LockRead()
        {
            readMutex.WaitOne();

            if (writePending > 0)
            {
                readMutex.ReleaseMutex();

                writePendingSemaphore.Wait();
                writePendingSemaphore.Release();

                readMutex.WaitOne();
            }

            if(readCounter == 0)
            {
                readMutex.ReleaseMutex();
                
                writeSemaphore.Wait();

                readMutex.WaitOne();
            }

            readCounter++;

            readMutex.ReleaseMutex();

            return new UsableWriteLockDisposable_read(this);
        }

        /// <summary>
        /// Locks the WriteLock for Writing
        /// </summary>
        /// <returns>An IDisposable Object to be used in a using statement</returns>
        public UsableWriteLockDisposable_write LockWrite()
        {
            readMutex.WaitOne();

            writePending++;

            writePendingSemaphore.Wait();

            readMutex.ReleaseMutex();

            writeSemaphore.Wait();

            return new UsableWriteLockDisposable_write(this);
        }

        /// <summary>
        /// Locks a couple of UsableWriteLocks for Reading in order to prevent deadlocks.
        /// </summary>
        /// <param name="locks">the WriteLocks to lock</param>
        /// <returns>a multidisposer to release the locks in opposite order</returns>
        public static MultiDisposer LockRead(params UsableWriteLock[] locks)
        {
            IDisposable[] disposables = new IDisposable[locks.Length];
            UsableWriteLock[] lockys = new UsableWriteLock[locks.Length];

            for (int i = 0; i < locks.Length; i++)
            {
                int currentIndex = -1;

                for (int j = 0; j < locks.Length; j++)
                {
                    if (lockys.Contains(locks[j]))
                        continue;

                    if (lockys[i] == null)
                        currentIndex = j;
                    else if (string.Compare(lockys[i].ID, locks[j].ID, StringComparison.Ordinal) < 0)
                        currentIndex = j;
                }

                lockys[i] = locks[currentIndex];
                lockys[i].LockRead();
                disposables[i] = locks[currentIndex].LockRead();
            }

            return new MultiDisposer(disposables);
        }

        /// <summary>
        /// Locks a couple of UsableWriteLocks for Writing in order to prevent deadlocks.
        /// </summary>
        /// <param name="locks">the WriteLocks to lock</param>
        /// <returns>a multidisposer to release the locks in opposite order</returns>
        public static MultiDisposer LockWrite(params UsableWriteLock[] locks)
        {
            IDisposable[] disposables = new IDisposable[locks.Length];
            UsableWriteLock[] lockys = new UsableWriteLock[locks.Length];

            for (int i = 0; i < locks.Length; i++)
            {
                int currentIndex = -1;

                for (int j = 0; j < locks.Length; j++)
                {
                    if (lockys.Contains(locks[j]))
                        continue;

                    if (lockys[i] == null)
                        currentIndex = j;
                    else if (string.Compare(lockys[i].ID, locks[j].ID, StringComparison.Ordinal) < 0)
                        currentIndex = j;
                }

                lockys[i] = locks[currentIndex];
                lockys[i].LockWrite();
                disposables[i] = locks[currentIndex].LockWrite();
            }

            return new MultiDisposer(disposables);
        }

        /// <summary>
        /// A helper class to be used in a using statement
        /// </summary>
        public class UsableWriteLockDisposable_write : IDisposable
        {
            private UsableWriteLock writeLock;

            internal UsableWriteLockDisposable_write(UsableWriteLock writeLock)
            {
                this.writeLock = writeLock;
            }

            /// <summary>
            /// Releases the Semaphore
            /// </summary>
            public void Dispose()
            {
                writeLock.writeSemaphore.Release();

                writeLock.readMutex.WaitOne();
                writeLock.writePending--;
                writeLock.readMutex.ReleaseMutex();

                writeLock.writePendingSemaphore.Release();
            }
        }

        /// <summary>
        /// A helper class to be used in a using statement
        /// </summary>
        public class UsableWriteLockDisposable_read : IDisposable
        {
            private readonly UsableWriteLock _writeLock;

            internal UsableWriteLockDisposable_read(UsableWriteLock writeLock)
            {
                this._writeLock = writeLock;
            }

            /// <summary>
            /// Releases the mutex and unsubscribes from the writeLock
            /// </summary>
            public void Dispose()
            {
                _writeLock.readMutex.WaitOne();
                _writeLock.readCounter--;

                if (_writeLock.readCounter == 0)
                {
                    _writeLock.writeSemaphore.Release();
                }

                _writeLock.readMutex.ReleaseMutex();
            }
        }
    }

    /// <summary>
    /// A wrapper class for a writeLock to be used in a using statement
    /// </summary>
    public class UsableSemaphore : IDisposable
    {
        private readonly SemaphoreSlim _semaphore;

        /// <summary>
        /// Constructs a new UsableSemaphore. The Semaphore is not locked and will not be locked until you call Lock().
        /// </summary>
        /// <param name="semaphore">the writeLock</param>
        public UsableSemaphore(SemaphoreSlim semaphore)
        {
            this._semaphore = semaphore;
        }

        /// <summary>
        /// Locks this Semaphore.
        /// </summary>
        public void Lock()
        {
            this._semaphore.Wait();
        }

        /// <summary>
        /// Releases this Semaphore.
        /// </summary>
        public void Dispose()
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// A MultiDisposer disposes all given object on dispose.
    /// </summary>
    public class MultiDisposer : IDisposable
    {
        private readonly IDisposable[] _disposables;

        /// <summary>
        /// Creates a MultiDisposer.
        /// </summary>
        /// <param name="disposables">the IDisposable objects to dispose on dispose.</param>
        public MultiDisposer(params IDisposable[] disposables)
        {
            this._disposables = disposables;
        }

        /// <summary>
        /// Disposes all given disposing objects in oposite order.
        /// </summary>
        public void Dispose()
        {
            for (int i = _disposables.Length - 1; i >= 0; i--)
            {
                _disposables[i].Dispose();
            }
        }
    }

}
