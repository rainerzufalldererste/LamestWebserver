using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LamestWebserver.Collections;

namespace UnitTests
{
    [TestClass]
    public class BitListTests
    {
        [TestMethod]
        public void TestBitList()
        {
            TestBitList(128, 1234);
            TestBitList(1024, 54321);
            TestBitList(256, 11);
            TestBitList(2048, 242);
            TestBitList(1111, 42);
        }

        private void TestBitList(int size, int seed)
        {
            BitList blist = new BitList();
            List<bool> list = new List<bool>();
            Random r = new Random(seed);

            for (int i = 0; i < size; i++)
            {
                bool b = r.NextDouble() < 0.5;

                blist.Add(b);
                list.Add(b);

                AssertListEquals(list, blist);
            }

            for (int i = size - 1; i >= 0; i -= 3)
            {
                blist.RemoveAt(i);
                list.RemoveAt(i);

                AssertListEquals(list, blist);

                if (i % 4 == 0)
                {
                    bool b = r.NextDouble() < 0.5;
                    int index = r.Next(100);

                    blist.Insert(index, b);
                    list.Insert(index, b);

                    AssertListEquals(list, blist);
                }
            }
        }

        private void AssertListEquals<T>(IList<T> a, IList<T> b)
        {
            Assert.AreEqual(a.Count, b.Count);

            for (int i = 0; i < a.Count; i++)
                Assert.AreEqual(a[i], b[i]);
        }
    }
}
