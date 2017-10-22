using LamestWebserver.Synchronization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace LamestWebserver.Core.Memory
{
    /// <summary>
    /// Provides very fast Allocation for a flushable volatile memory pool.
    /// </summary>
    public unsafe class FlushableMemoryPool : IAllocator, IDisposable
    {
        /// <summary>
        /// When this amount of concurrent FlushableMemoryPools is exceeded raise an exception. (If it is negative or zero: never raise an exception)
        /// </summary>
        public static int MaximumThreads = 128;

        private static int _concurrentThreads;
        private static int _threadIndex = 0;
        private static UsableMutexSlim _threadCountMutex = new UsableMutexSlim();

        /// <summary>
        /// Retrieves the ThreadID of this FlushableMemoryPool.
        /// </summary>
        public readonly int ThreadID;

        private int _highWaterMark = 1024;
        private int _currentSize = 1024;
        private int _position = 0;

        /// <summary>
        /// Retrieves the allocated size of this FlushableMemoryPool.
        /// </summary>
        public int Size => _highWaterMark;

        /// <summary>
        /// Retrieves the allocated size of the FlushableMemoryPool of the current thread.
        /// </summary>
        public static int AllocatedSize
        {
            get
            {
                if (ThreadFlushableMemoryPool == null)
                    return 0;

                return ThreadFlushableMemoryPool.Size;
            }
        }

        private UsableMutexSlim _mutex = new UsableMutexSlim();
        private List<IntPtr> _memoryBlocks = new List<IntPtr>();

        [ThreadStatic]
        private static FlushableMemoryPool ThreadFlushableMemoryPool;

        /// <summary>
        /// Creates or Flushes the FlushableMemoryPool of the current thread.
        /// </summary>
        public static void AquireOrFlush()
        {
            // This is thread-safe because ThreadFlushableMemoryPool is ThreadStatic.

            if (ThreadFlushableMemoryPool == null)
            {
                ThreadFlushableMemoryPool = new FlushableMemoryPool();
                Logger.LogInformation("A new FlushableMemoryPool has been initialized.");
            }
            else if (ThreadFlushableMemoryPool._position > 0 || ThreadFlushableMemoryPool._memoryBlocks.Count > 1)
            {
                ThreadFlushableMemoryPool.Flush();
                Logger.LogInformation($"FlushableMemoryPool '{ThreadFlushableMemoryPool.ThreadID}' has been flushed.");
            }
        }

        /// <summary>
        /// Allocates a block of memory.
        /// </summary>
        /// <typeparam name="T">The type of the object(s) to allocate memory for.</typeparam>
        /// <param name="count">The number of object(s) to allocate memory for.</param>
        /// <returns>The address as IntPtr.</returns>
        public static IntPtr Allocate<T>(int count = 1) where T : struct
        {
            // This is thread-safe because ThreadFlushableMemoryPool is ThreadStatic.

            if (ThreadFlushableMemoryPool == null)
                AquireOrFlush();

            return ThreadFlushableMemoryPool.AllocateMemory<T>(count);
        }

        /// <summary>
        /// Destroys the FlushableMemoryPool of the current thread.
        /// </summary>
        public static void Destroy()
        {
            // This is thread-safe because ThreadFlushableMemoryPool is ThreadStatic.

            if (ThreadFlushableMemoryPool != null)
            {
                ThreadFlushableMemoryPool.Dispose();
                ThreadFlushableMemoryPool = null;
            }
        }

        /// <summary>
        /// Constructs a new FlushableMemoryPool.
        /// </summary>
        public FlushableMemoryPool(int DefaultAllocationSize = 1024)
        {
            using (_threadCountMutex.Lock())
            {
                ThreadID = _threadIndex++;
                _concurrentThreads++;

                if (MaximumThreads > 0 && _concurrentThreads >= MaximumThreads)
                    throw new IndexOutOfRangeException($"The maximum specified count of concurrent Flushable Memory Pools has been exceeded. You can remove or increase this limit by changing {nameof(FlushableMemoryPool)}.{nameof(MaximumThreads)} either to <= 0 to disable it, or to a greater value to increase the limit.");
            }

            _highWaterMark = DefaultAllocationSize;

            _memoryBlocks.Add(Marshal.AllocHGlobal(_highWaterMark));
        }

        /// <summary>
        /// Destructs the current Flushable Memory Pool by calling Dispose();
        /// </summary>
        ~FlushableMemoryPool()
        {
            Dispose();
        }

        /// <inheritdoc />
        public IntPtr AllocateMemory<T>(int count = 1) where T : struct
        {
            int totalSize = count * Marshal.SizeOf(typeof(T));

            using (_mutex.Lock())
            {
                if (_memoryBlocks.Count > 1)
                    _highWaterMark += totalSize;

                if (_position + totalSize > _currentSize)
                {
                    if(_memoryBlocks.Count == 1)
                        _highWaterMark += (_position + totalSize - _highWaterMark);

                    _currentSize *= 2;
                    _memoryBlocks.Add(Marshal.AllocHGlobal(_currentSize));
                    _position = 0;
                }

                IntPtr ret = (_memoryBlocks.Last() + _position);

                _position += totalSize;

                return ret;
            }
        }

        /// <inheritdoc />
        public void Free(IntPtr pointer)
        {
            // we don't need free here. we just clear on flush.
        }

        /// <summary>
        /// Flushes the Memory and resets the position.
        /// </summary>
        public void Flush()
        {
            using (_mutex.Lock())
            {
                _position = 0;

                if (_memoryBlocks.Count > 1)
                {
                    _currentSize = _highWaterMark;

                    foreach (IntPtr memoryBlock in _memoryBlocks)
                        Marshal.FreeHGlobal(memoryBlock);

                    _memoryBlocks.Clear();

                    _memoryBlocks.Add(Marshal.AllocHGlobal(_highWaterMark));
                }
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            using (_threadCountMutex.Lock())
                _concurrentThreads--;

            using (_mutex.Lock())
            {
                foreach (IntPtr memoryBlock in _memoryBlocks)
                    Marshal.FreeHGlobal(memoryBlock);

                _memoryBlocks.Clear();
            }
        }
    }
}
