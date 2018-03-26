using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LamestWebserver.Collections;

namespace UnitTests
{
    [TestClass]
    public class FixedSizeQueueTests
    {
        [TestMethod]
        public void TestFixedSizeQueue()
        {
            FixedSizeQueue<string> fixedSizeQueue = new FixedSizeQueue<string>(128);

            TestWithSize(fixedSizeQueue, 128);

            fixedSizeQueue.MaximumCapacity = 1234;
            TestWithSize(fixedSizeQueue, 1234);
        }

        private void TestWithSize(FixedSizeQueue<string> fixedSizeQueue, int size)
        {
            Assert.AreEqual(size, fixedSizeQueue.MaximumCapacity);
            Assert.AreEqual(0, fixedSizeQueue.Count);
            Assert.IsFalse(fixedSizeQueue.Contains(null));

            for (int i = 0; i < size; i++)
            {
                fixedSizeQueue.Push(i.ToString());
                Assert.AreEqual(i + 1, fixedSizeQueue.Count);
                Assert.IsFalse(fixedSizeQueue.Contains((i + 1).ToString()));

                for (int j = 0; j < i; j++)
                {
                    Assert.AreEqual((i - j).ToString(), fixedSizeQueue[j]);
                    Assert.IsTrue(fixedSizeQueue.Contains((i - j).ToString()));
                }
            }

            int index = size;

            for (int i = 0; i < size * 3 + 11; i++)
            {
                fixedSizeQueue.Push(index++.ToString());
                Assert.AreEqual(size, fixedSizeQueue.Count);

                for (int j = 0; j < size; j++)
                    Assert.AreEqual((index - j - 1).ToString(), fixedSizeQueue[j]);
            }

            foreach (string s in fixedSizeQueue)
                Assert.AreEqual((--index).ToString(), s);

            fixedSizeQueue.Clear();

            Assert.AreEqual(0, fixedSizeQueue.Count);
        }
    }
}
