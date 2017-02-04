using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LamestWebserver;
using System.Threading;

namespace UnitTests
{
    [TestClass]
    public class UsableMutexTest
    {
        [TestMethod]
        public void TestUsableMutexes()
        {
            UsableMutex mutex = new UsableMutex();
            bool someValue = false;

            Thread t0 = new Thread(() =>
            {
                using (mutex.Lock())
                {
                    Thread.Sleep(10000);
                    someValue = true;
                    Console.WriteLine($"T0] value is now true");
                }
            });

            Thread t1 = new Thread(() =>
            {
                Thread.Sleep(50);
                int tries = 0;

                retry:
                Console.WriteLine($"T1] {tries} tries...");

                Assert.IsTrue(tries < 10);

                try
                {
                    using (mutex.Lock())
                    {
                        Assert.IsFalse(someValue);
                        Console.WriteLine($"T1] value is still false");
                    }
                }
                catch (MutexRetryException)
                {
                    tries++;
                    Thread.Sleep(50);
                    goto retry;
                }
            });

            t0.Start();
            t1.Start();

            t0.Join();
            t1.Join();
        }
    }
}
