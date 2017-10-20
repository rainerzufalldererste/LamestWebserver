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
            BitList blist = new BitList();
            List<bool> list = new List<bool>();
            Random r = new Random(1234);

            for (int i = 0; i < 1024; i++)
            {
                bool b = r.NextDouble() < 0.5;

                blist.Add(b);
                list.Add(b);

                AssertListEquals(list, blist);
            }

            for (int i = 1024 - 1; i >= 0; i -= 3)
            {
                blist.RemoveAt(i);
                list.RemoveAt(i);

                AssertListEquals(list, blist);
                
                if (i % 4 == 0)
                {
                    bool b = r.NextDouble() < 0.5;
                    int index = r.Next(10);

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
