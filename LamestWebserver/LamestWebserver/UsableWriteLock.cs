using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LamestWebserver
{
    /// <summary>
    /// A safe and fast way to read and write from shared ressources without blocking everything.
    /// </summary>
    public class UsableWriteLock
    {
        private uint readCounter = 0;
        private Mutex readMutex = new Mutex();
        private Mutex writeMutex = new Mutex();
        private readonly string ID = SessionContainer.generateHash();

        /// <summary>
        /// Locks the WriteLock for reading
        /// </summary>
        /// <returns>An IDisposable Object to be used in a using statement</returns>
        public UsableWriteLockDisposable_read LockRead()
        {
            readMutex.WaitOne();

            if(readCounter == 0)
            {
                readMutex.ReleaseMutex();

                writeMutex.WaitOne();

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
            writeMutex.WaitOne();

            return new UsableWriteLockDisposable_write(writeMutex);
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
                    else if (lockys[i].ID.CompareTo(locks[j].ID) < 0)
                        currentIndex = j;
                }

                lockys[i] = locks[currentIndex];
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
                    else if (lockys[i].ID.CompareTo(locks[j].ID) < 0)
                        currentIndex = j;
                }

                lockys[i] = locks[currentIndex];
                disposables[i] = locks[currentIndex].LockWrite();
            }

            return new MultiDisposer(disposables);
        }

        /// <summary>
        /// A helper class to be used in a using statement
        /// </summary>
        public class UsableWriteLockDisposable_write : IDisposable
        {
            private Mutex mutex;

            internal UsableWriteLockDisposable_write(Mutex mutex)
            {
                this.mutex = mutex;
            }

            /// <summary>
            /// Releases the mutex
            /// </summary>
            public void Dispose()
            {
                mutex.ReleaseMutex();
            }
        }

        /// <summary>
        /// A helper class to be used in a using statement
        /// </summary>
        public class UsableWriteLockDisposable_read : IDisposable
        {
            private UsableWriteLock writeLock;

            internal UsableWriteLockDisposable_read(UsableWriteLock writeLock)
            {
                this.writeLock = writeLock;
            }

            /// <summary>
            /// Releases the mutex and unsubscribes from the writeLock
            /// </summary>
            public void Dispose()
            {
                writeLock.readMutex.WaitOne();
                writeLock.readCounter--;

                if (writeLock.readCounter == 0)
                    writeLock.writeMutex.ReleaseMutex();

                writeLock.readMutex.ReleaseMutex();
            }
        }
    }

    /// <summary>
    /// A MultiDisposer disposes all given object on dispose.
    /// </summary>
    public class MultiDisposer : IDisposable
    {
        private IDisposable[] disposables;

        /// <summary>
        /// Creates a MultiDisposer.
        /// </summary>
        /// <param name="disposables">the IDisposable objects to dispose on dispose.</param>
        public MultiDisposer(params IDisposable[] disposables)
        {
            this.disposables = disposables;
        }

        /// <summary>
        /// Disposes all given disposing objects in oposite order.
        /// </summary>
        public void Dispose()
        {
            for (int i = disposables.Length - 1; i >= 0; i--)
            {
                disposables[i].Dispose();
            }
        }
    }

}
