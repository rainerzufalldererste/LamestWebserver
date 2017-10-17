using LamestWebserver.Synchronization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace LamestWebserver.Core.Memory
{
    public unsafe class FlushableMemoryPool : IAllocator, IDisposable
    {
        /// <summary>
        /// When this amount of concurrent FlushableMemoryPools is exceeded raise an exception. (If it is negative or zero: never raise an exception)
        /// </summary>
        public static int MaximumThreads = 128;

        private static int _concurrentThreads;
        private static int _threadIndex = 0;
        private static UsableMutexSlim _threadCountMutex = new UsableMutexSlim();

        public readonly int ThreadID;
        
        private IntPtr _data;
        private int _highWaterMark = 1024;
        private int _position = 0;

        private UsableMutexSlim mutex = new UsableMutexSlim();

        [ThreadStatic]
        private static FlushableMemoryPool ThreadFlushableMemoryPool;

        public static FlushableMemoryPool AquireOrFlush()
        {
            if (ThreadFlushableMemoryPool == null)
            {
                ThreadFlushableMemoryPool = new FlushableMemoryPool();
                Logger.LogInformation("A new FlushableMemoryPool has been initialized.");
            }
            else if(ThreadFlushableMemoryPool._position > 0)
            {
                ThreadFlushableMemoryPool.Flush();
                Logger.LogInformation($"FlushableMemoryPool '{ThreadFlushableMemoryPool.ThreadID}' has been flushed.");
            }

            return ThreadFlushableMemoryPool;
        }

        public static IntPtr AllocateOnThread<T>(int size, int sizeOfT) where T : struct
        {
            if (ThreadFlushableMemoryPool == null)
                AquireOrFlush();

            return ThreadFlushableMemoryPool.Allocate<T>(size, sizeOfT);
        }

        public FlushableMemoryPool()
        {
            using (_threadCountMutex.Lock())
            {
                ThreadID = _threadIndex++;
                _concurrentThreads++;

                if (MaximumThreads > 0 && _concurrentThreads >= MaximumThreads)
                    throw new IndexOutOfRangeException($"The maximum specified count of concurrent Flushable Memory Pools has been exceeded. You can remove or increase this limit by changing {nameof(FlushableMemoryPool)}.{nameof(MaximumThreads)} either to <= 0 to disable it, or to a greater value to increase the limit.");
            }

            _data = Marshal.AllocHGlobal(_highWaterMark);
        }

        ~FlushableMemoryPool()
        {
            using (_threadCountMutex.Lock())
                _concurrentThreads--;
        }

        public IntPtr Allocate<T>(int size, int sizeofT) where T : struct
        {
            int totalSize = size * sizeofT;

            using (mutex.Lock())
            {
                if (_position + totalSize > _highWaterMark)
                {
                    _highWaterMark = _position + totalSize;
                    Marshal.ReAllocHGlobal(_data, (IntPtr)_highWaterMark);
                }

                IntPtr ret = (_data + _position);

                _position += totalSize;

                return ret;
            }
        }

        public void Free(IntPtr pointer)
        {
            // we don't need free here. we just clear on flush.
        }

        public void Flush()
        {
            using (mutex.Lock())
                _position = 0;
        }

        public void Dispose()
        {
            Marshal.FreeHGlobal(_data);
        }
    }
}
