using Microsoft.VisualStudio.TestTools.UnitTesting;
using LamestWebserver.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests
{
    [TestClass]
    public class IDTests
    {
        [TestMethod]
        public void TestID()
        {
            ID id0 = new ID();
            ID id1 = new ID();

            if (id0.CompareTo(id1) > 0)
                Assert.IsTrue(id1.CompareTo(id0) < 0);
            else if (id0.CompareTo(id1) < 0)
                Assert.IsTrue(id1.CompareTo(id0) > 0);

            Assert.IsTrue(id0.CompareTo(id1) != 0 && id1.CompareTo(id0) != 0);
            Assert.AreNotEqual(id0.Value, id1.Value);
            Assert.IsTrue(!ArraysAreEqual(id0.GetByteArray(), id1.GetByteArray()));
            Assert.IsTrue(!ArraysAreEqual(id0.GetUlongArray(), id1.GetUlongArray()));

            id0.Value = id1.Value;

            Assert.IsTrue(id0.CompareTo(id1) == 0 && id1.CompareTo(id0) == 0);
            Assert.AreEqual(id0.Value, id1.Value);
            Assert.IsTrue(ArraysAreEqual(id0.GetByteArray(), id1.GetByteArray()));
            Assert.IsTrue(ArraysAreEqual(id0.GetUlongArray(), id1.GetUlongArray()));

            id1 = new ID(id0.Value);

            Assert.IsTrue(id0.CompareTo(id1) == 0 && id1.CompareTo(id0) == 0);
            Assert.AreEqual(id0.Value, id1.Value);
            Assert.IsTrue(ArraysAreEqual(id0.GetByteArray(), id1.GetByteArray()));
            Assert.IsTrue(ArraysAreEqual(id0.GetUlongArray(), id1.GetUlongArray()));

            id1 = new ID(id0.GetByteArray());

            Assert.IsTrue(id0.CompareTo(id1) == 0 && id1.CompareTo(id0) == 0);
            Assert.AreEqual(id0.Value, id1.Value);
            Assert.IsTrue(ArraysAreEqual(id0.GetByteArray(), id1.GetByteArray()));
            Assert.IsTrue(ArraysAreEqual(id0.GetUlongArray(), id1.GetUlongArray()));

            id1 = new ID(id0.GetUlongArray());

            Assert.IsTrue(id0.CompareTo(id1) == 0 && id1.CompareTo(id0) == 0);
            Assert.AreEqual(id0.Value, id1.Value);
            Assert.IsTrue(ArraysAreEqual(id0.GetByteArray(), id1.GetByteArray()));
            Assert.IsTrue(ArraysAreEqual(id0.GetUlongArray(), id1.GetUlongArray()));

            id1 = new ID();

            if (id0.CompareTo(id1) > 0)
                Assert.IsTrue(id1.CompareTo(id0) < 0);
            else if (id0.CompareTo(id1) < 0)
                Assert.IsTrue(id1.CompareTo(id0) > 0);

            Assert.IsTrue(id0.CompareTo(id1) != 0 && id1.CompareTo(id0) != 0);
            Assert.AreNotEqual(id0.Value, id1.Value);
            Assert.IsTrue(!ArraysAreEqual(id0.GetByteArray(), id1.GetByteArray()));
            Assert.IsTrue(!ArraysAreEqual(id0.GetUlongArray(), id1.GetUlongArray()));

            id0 = new ID(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7 });

            try
            {
                id0 = new ID(new byte[] { 0, 1, 2, 3, 4, 5, 6 });
                Assert.Fail();
            }
            catch (InvalidOperationException) { }

            try
            {
                id0 = new ID(new byte[0]);
                Assert.Fail();
            }
            catch (InvalidOperationException) { }

            try
            {
                byte[] a = null;
                id0 = new ID(a);
                Assert.Fail();
            }
            catch (NullReferenceException) { }

            id0 = new ID("BAADF00DBAADF00D");
            id0 = new ID("b175a813badF00DCAFEBABEb00b55a1e");

            try
            {
                id0 = new ID("");
                Assert.Fail();
            }
            catch (InvalidOperationException) { }

            try
            {
                id0 = new ID("0");
                Assert.Fail();
            }
            catch (InvalidOperationException) { }

            try
            {
                id0 = new ID("012301230");
                Assert.Fail();
            }
            catch (InvalidOperationException) { }

            try
            {
                string s = null;
                id0 = new ID(s);
                Assert.Fail();
            }
            catch (NullReferenceException) { }

            try
            {
                id0 = new ID("q1230123abcd1234");
                Assert.Fail();
            }
            catch (FormatException) { }

            try
            {
                id0 = new ID("0123 123abcd1234");
                Assert.Fail();
            }
            catch (FormatException) { }

            try
            {
                id0 = new ID("abcd12340123012" + (char)('0' - 1));
                Assert.Fail();
            }
            catch (FormatException) { }

            try
            {
                id0 = new ID("abcd12340123012" + (char)('9' + 1));
                Assert.Fail();
            }
            catch (FormatException) { }

            try
            {
                id0 = new ID("abcd12340123012" + (char)('a' - 1));
                Assert.Fail();
            }
            catch (FormatException) { }

            try
            {
                id0 = new ID("abcd12340123012" + (char)('f' + 1));
                Assert.Fail();
            }
            catch (FormatException) { }

            try
            {
                id0 = new ID("abcd12340123012" + (char)('A' - 1));
                Assert.Fail();
            }
            catch (FormatException) { }

            try
            {
                id0 = new ID("abcd12340123012" + (char)('F' + 1));
                Assert.Fail();
            }
            catch (FormatException) { }

            id0 = new ID("FFFFFFFFFFFFFFFF");

            Assert.AreEqual("FFFFFFFFFFFFFFFF", id0.ToString());
            Assert.AreEqual(id0.Value, id0.ToString());
            Assert.AreEqual(1, id0.GetUlongArray().Length);
            Assert.AreEqual(ulong.MaxValue, id0.GetUlongArray()[0]);

            id1 = new ID("0123456789ABCDEF");

            Assert.AreEqual("0123456789ABCDEF", id1.ToString());
            Assert.AreEqual(1, id1.GetUlongArray().Length);
            Assert.AreEqual(0xFEDCBA9876543210ul, id1.GetUlongArray()[0]);
        }

        [TestMethod]
        public void TestLongID()
        {
            LongID LongID0 = new LongID();
            LongID LongID1 = new LongID();

            if (LongID0.CompareTo(LongID1) > 0)
                Assert.IsTrue(LongID1.CompareTo(LongID0) < 0);
            else if (LongID0.CompareTo(LongID1) < 0)
                Assert.IsTrue(LongID1.CompareTo(LongID0) > 0);

            Assert.IsTrue(LongID0.CompareTo(LongID1) != 0 && LongID1.CompareTo(LongID0) != 0);
            Assert.AreNotEqual(LongID0.Value, LongID1.Value);
            Assert.IsTrue(!ArraysAreEqual(LongID0.GetByteArray(), LongID1.GetByteArray()));
            Assert.IsTrue(!ArraysAreEqual(LongID0.GetUlongArray(), LongID1.GetUlongArray()));

            LongID0.Value = LongID1.Value;

            Assert.IsTrue(LongID0.CompareTo(LongID1) == 0 && LongID1.CompareTo(LongID0) == 0);
            Assert.AreEqual(LongID0.Value, LongID1.Value);
            Assert.IsTrue(ArraysAreEqual(LongID0.GetByteArray(), LongID1.GetByteArray()));
            Assert.IsTrue(ArraysAreEqual(LongID0.GetUlongArray(), LongID1.GetUlongArray()));

            LongID1 = new LongID(LongID0.Value);

            Assert.IsTrue(LongID0.CompareTo(LongID1) == 0 && LongID1.CompareTo(LongID0) == 0);
            Assert.AreEqual(LongID0.Value, LongID1.Value);
            Assert.IsTrue(ArraysAreEqual(LongID0.GetByteArray(), LongID1.GetByteArray()));
            Assert.IsTrue(ArraysAreEqual(LongID0.GetUlongArray(), LongID1.GetUlongArray()));

            LongID1 = new LongID(LongID0.GetByteArray());

            Assert.IsTrue(LongID0.CompareTo(LongID1) == 0 && LongID1.CompareTo(LongID0) == 0);
            Assert.AreEqual(LongID0.Value, LongID1.Value);
            Assert.IsTrue(ArraysAreEqual(LongID0.GetByteArray(), LongID1.GetByteArray()));
            Assert.IsTrue(ArraysAreEqual(LongID0.GetUlongArray(), LongID1.GetUlongArray()));

            LongID1 = new LongID(LongID0.GetUlongArray());

            Assert.IsTrue(LongID0.CompareTo(LongID1) == 0 && LongID1.CompareTo(LongID0) == 0);
            Assert.AreEqual(LongID0.Value, LongID1.Value);
            Assert.IsTrue(ArraysAreEqual(LongID0.GetByteArray(), LongID1.GetByteArray()));
            Assert.IsTrue(ArraysAreEqual(LongID0.GetUlongArray(), LongID1.GetUlongArray()));

            LongID1 = new LongID();

            if (LongID0.CompareTo(LongID1) > 0)
                Assert.IsTrue(LongID1.CompareTo(LongID0) < 0);
            else if (LongID0.CompareTo(LongID1) < 0)
                Assert.IsTrue(LongID1.CompareTo(LongID0) > 0);

            Assert.IsTrue(LongID0.CompareTo(LongID1) != 0 && LongID1.CompareTo(LongID0) != 0);
            Assert.AreNotEqual(LongID0.Value, LongID1.Value);
            Assert.IsTrue(!ArraysAreEqual(LongID0.GetByteArray(), LongID1.GetByteArray()));
            Assert.IsTrue(!ArraysAreEqual(LongID0.GetUlongArray(), LongID1.GetUlongArray()));
        }

        private bool ArraysAreEqual<T>(T[] a, T[] b) where T : IComparable<T>
        {
            if (a.Length != b.Length)
                return false;

            for (int i = 0; i < a.Length; i++)
            {
                if (a[i].CompareTo(b[i]) != 0 || b[i].CompareTo(a[i]) != 0)
                    return false;
            }

            return true;
        }
    }
}