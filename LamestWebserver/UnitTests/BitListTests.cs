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
            BitList list = new BitList();

            for (int i = 0; i < 1025; i++)
                list.Add(true);

            Assert.AreEqual(0, list.IndexOf(true));
            Assert.AreEqual(-1, list.IndexOf(false));

            list.Add(false);
            Assert.AreEqual(0, list.IndexOf(true));
            Assert.AreEqual(1025, list.IndexOf(false));

            Assert.IsTrue(list.Remove(false));
            Assert.AreEqual(0, list.IndexOf(true));
            Assert.AreEqual(-1, list.IndexOf(false));

            try
            {
                var x = new bool[1024];
                list.CopyTo(x, 0);

                Assert.Fail();
            }
            catch (InvalidOperationException) { }

            try
            {
                var x = new bool[1026];
                list.CopyTo(x, 3);

                Assert.Fail();
            }
            catch (InvalidOperationException) { }

            var array = new bool[1025];
            list.CopyTo(array, 0);
            int index = 0;

            foreach (bool value in array)
                Assert.AreEqual(list[index++], value);

            Assert.AreEqual(1025, list.Count);

            for (int i = 0; i < 1025; i++)
                list.RemoveAt(list.Count / 2);

            Assert.AreEqual(0, list.Count);

            if (list.Remove(false))
                Assert.Fail();

            if (list.Remove(true))
                Assert.Fail();

            try
            {
                list.RemoveAt(0);
                Assert.Fail();
            }
            catch (IndexOutOfRangeException) { }

            try
            {
                list[0] = false;

                Assert.Fail();
            }
            catch (IndexOutOfRangeException) { }
            
            try
            {
                var a = list[0];

                Assert.Fail();
            }
            catch (IndexOutOfRangeException) { }

            list.Insert(0, true);

            if (!list.Remove(true))
                Assert.Fail();

            try
            {
                list.Insert(1, true);
                Assert.Fail();
            }
            catch (IndexOutOfRangeException) { }


            try
            {
                list.CopyTo(null, 0);
                Assert.Fail();
            }
            catch (NullReferenceException) { }
            
            array = new bool[0];
            list.CopyTo(array, 0);

            try
            {
                list.CopyTo(array, -1);
                Assert.Fail();
            }
            catch (IndexOutOfRangeException) { }

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

            bool[] arrayA = new bool[list.Count + 2];
            bool[] arrayB = new bool[blist.Count + 2];

            Assert.AreEqual(arrayA.Length, arrayB.Length);

            list.CopyTo(arrayA, 2);
            blist.CopyTo(arrayB, 2);

            for (int i = 0; i < arrayA.Length; i++)
                Assert.AreEqual(arrayA[i], arrayB[i]);

            for (int i = list.Count - 1; i >= 0; i--)
            {
                blist.RemoveAt(i);
                list.RemoveAt(i);

                AssertListEquals(list, blist);
            }
        }

        private void AssertListEquals<T>(IList<T> a, IList<T> b)
        {
            Assert.AreEqual(a.Count, b.Count);

            for (int i = 0; i < a.Count; i++)
                Assert.AreEqual(a[i], b[i]);

            int index = 0;

            foreach (T x in b)
            {
                Assert.AreEqual(a[index], x);

                index++;
            }

            index = 0;

            foreach (T x in a)
            {
                Assert.AreEqual(b[index], x);

                index++;
            }
        }
    }
}
