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
}
