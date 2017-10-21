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
            TestBitListWith(128, 1234);
            TestBitListWith(1024, 54321);
            TestBitListWith(256, 11);
            TestBitListWith(2048, 242);
            TestBitListWith(1111, 42);

            BitList blist = new BitList();

            blist.Add(false);
            blist.Add(false);

            Assert.IsTrue(blist.Contains(false));
            Assert.IsFalse(blist.Contains(true));

            blist.Add(true);

            Assert.IsTrue(blist.Contains(false));
            Assert.IsTrue(blist.Contains(true));

            blist.Clear();
            Assert.AreEqual(0, blist.Count);

            blist.Add(true);
            blist.Add(true);

            Assert.IsFalse(blist.Contains(false));
            Assert.IsTrue(blist.Contains(true));

            blist.Add(false);

            Assert.IsTrue(blist.Contains(false));
            Assert.IsTrue(blist.Contains(true));

            blist.Clear();
            Assert.AreEqual(0, blist.Count);

            for (int i = 0; i < 128; i++)
                blist.Add(false);

            Assert.IsTrue(blist.Contains(false));
            Assert.IsFalse(blist.Contains(true));

            blist.Add(true);

            Assert.IsTrue(blist.Contains(false));
            Assert.IsTrue(blist.Contains(true));

            Assert.AreEqual(129, blist.Count);

            blist.Remove(true);
            Assert.AreEqual(128, blist.Count);

            Assert.IsTrue(blist.Contains(false));
            Assert.IsFalse(blist.Contains(true));

            blist.Clear();
            Assert.AreEqual(0, blist.Count);

            for (int i = 0; i < 128; i++)
                blist.Add(true);
            
            Assert.IsFalse(blist.Contains(false));
            Assert.IsTrue(blist.Contains(true));

            blist.Add(false);

            Assert.IsTrue(blist.Contains(false));
            Assert.IsTrue(blist.Contains(true));

            Assert.AreEqual(129, blist.Count);

            blist.Remove(false);
            Assert.AreEqual(128, blist.Count);

            Assert.IsFalse(blist.Contains(false));
            Assert.IsTrue(blist.Contains(true));
        }

        private void TestBitListWith(int size, int seed)
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

            for (int i = 0; i < blist.Count; i++)
            {
                bool b = r.NextDouble() < 0.5;
                int index = r.Next(blist.Count);

                blist[index] = b;
                list[index] = b;

                AssertListEquals(list, blist);
            }

            for (int i = 0; i < size; i++)
            {
                bool b = r.NextDouble() < 0.5;

                blist.Add(b);
                list.Add(b);

                AssertListEquals(list, blist);
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
