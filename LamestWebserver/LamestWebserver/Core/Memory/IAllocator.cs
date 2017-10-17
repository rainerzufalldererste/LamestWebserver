using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LamestWebserver.Core.Memory
{
    interface IAllocator
    {
        unsafe IntPtr Allocate<T>(int size, int sizeOfT) where T : struct;

        unsafe void Free(IntPtr pointer);
    }
}
