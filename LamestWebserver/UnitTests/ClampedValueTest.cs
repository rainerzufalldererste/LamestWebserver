using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LamestWebserver.Core;

namespace UnitTests
{
    [TestClass]
    public class ClampedValueTest
    {
        [TestMethod]
        public void TestClampedValue()
        {
            ClampedValue<double> a = new ClampedValue<double>();

            a.Maximum = 1;
            a.Minimum = -1;

            a.Value = 2;
            Assert.IsTrue(a == a.Maximum);

            a.Value = -2;
            Assert.IsTrue(a == a.Minimum);

            a.Value = 0.125;
            Assert.IsTrue(a == 0.125);

            try
            {
                a.Minimum = -5;
                Assert.Fail();
            }
            catch (InvalidOperationException)
            {
                Assert.IsTrue(a.Minimum == -1);
            }

            try
            {
                a.Maximum = 5;
                Assert.Fail();
            }
            catch (InvalidOperationException)
            {
                Assert.IsTrue(a.Maximum == 1);
            }

            ClampedValue<int> b = new ClampedValue<int>(25, 50);

            Assert.IsTrue(b.Minimum == 25);
            Assert.IsTrue(b.Maximum == 50);

            b.Value = 75;
            Assert.IsTrue(b == b.Maximum);

            b.Value = -2;
            Assert.IsTrue(b == b.Minimum);

            b.Value = 25;
            Assert.IsTrue(b == 25);

            b.Value = 50;
            Assert.IsTrue(b == 50);

            b.Value = 37;
            Assert.IsTrue(b == 37);

            try
            {
                b.Minimum = -5;
                Assert.Fail();
            }
            catch (InvalidOperationException)
            {
                Assert.IsTrue(b.Minimum == 25);
            }

            try
            {
                b.Maximum = 55;
                Assert.Fail();
            }
            catch (InvalidOperationException)
            {
                Assert.IsTrue(b.Maximum == 50);
            }

            ClampedValue<byte> c = new ClampedValue<byte>(127, 2, 240);

            Assert.IsTrue(c.Value == 127);
            Assert.IsTrue(c.Minimum == 2);
            Assert.IsTrue(c.Maximum == 240);
        }
    }
}
