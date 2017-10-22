using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LamestWebserver.Core.Memory;
using System.Linq;

namespace UnitTests
{
    [TestClass]
    public class FlushableMemoryPoolTests
    {
        [TestMethod]
        public void TestFlushableMemoryPool()
        {
            Assert.AreEqual(0, FlushableMemoryPool.AllocatedSize);

            List<IntPtr> pointers = new List<IntPtr>();

            int size = 0;

            for (int i = 1; i < 1025; i++)
            {
                size += i * sizeof(int);

                pointers.Add(FlushableMemoryPool.Allocate<int>(i));
            }

            Assert.AreEqual(size, FlushableMemoryPool.AllocatedSize);

            FlushableMemoryPool.AquireOrFlush();
            pointers.Clear();

            for (int i = 1; i < 1025; i++)
            {
                pointers.Add(FlushableMemoryPool.Allocate<int>(i));

                unsafe
                {
                    *(int*)(pointers.Last()) = i;
                }
            }

            Assert.AreEqual(size, FlushableMemoryPool.AllocatedSize);

            for (int i = 0; i < 1024; i++)
            {
                if(i > 0)
                    Assert.IsTrue((pointers[i - 1] + i * sizeof(int)) == (pointers[i]));

                unsafe
                {
                    *(int*)(pointers[i]) = i + 1;
                }
            }

            FlushableMemoryPool.Destroy();
        }
    }
}
