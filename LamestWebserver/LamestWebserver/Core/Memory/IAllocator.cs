using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LamestWebserver.Core.Memory
{
    /// <summary>
    /// A memory allocation and freeing interface.
    /// </summary>
    public interface IAllocator
    {
        /// <summary>
        /// Allocates a block of memory.
        /// </summary>
        /// <typeparam name="T">The type of the object(s) to allocate memory for.</typeparam>
        /// <param name="count">The number of object(s) to allocate memory for.</param>
        /// <returns>The address as IntPtr.</returns>
        unsafe IntPtr AllocateMemory<T>(int count = 1) where T : struct;

        /// <summary>
        /// Frees a given IntPtr allocated with this Allocator.
        /// </summary>
        /// <param name="pointer">The IntPtr to free.</param>
        unsafe void Free(IntPtr pointer);
    }
}
