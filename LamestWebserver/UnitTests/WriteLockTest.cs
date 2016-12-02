using System;
using System.Threading;
using LamestWebserver;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
    [TestClass]
    public class WriteLockTest
    {
        [TestMethod]
        public void TestWriteLock()
        {
            Thread t1 = new Thread(Reads), t2 = new Thread(Reads), t3 = new Thread(Reads);
            Thread ta = new Thread(Writes), tb = new Thread(Writes), tc = new Thread(Writes);

            t1.Start();
            ta.Start();
            t2.Start();
            tb.Start();
            t3.Start();
            tc.Start();
            
            while(t1.IsAlive || t2.IsAlive || t3.IsAlive || ta.IsAlive || tb.IsAlive || tc.IsAlive)
                Thread.Sleep(1);
        }

        private void Reads()
        {
            for (int i = 0; i < 111; i++)
            {
                Read(true, false, false);
                Thread.Sleep(1);
                Read(false, true, false);
                Thread.Sleep(1);
                Read(false, false, true);
            }
        }
        private void Writes()
        {
            for (int i = 0; i < 111; i++)
            {
                Write(true, false, false);
                Thread.Sleep(3);
                Write(false, true, false);
                Thread.Sleep(3);
                Write(false, false, true);
            }
        }

        UsableWriteLock wla = new UsableWriteLock(), wlb = new UsableWriteLock(), wlc = new UsableWriteLock();
        private int ia = 0, ib = 0, ic = 0;
        UsableWriteLock console = new UsableWriteLock();

        private void Read(bool a, bool b, bool c)
        {
            if (a)
            {
                using (wla.LockRead())
                {
                    using (console.LockWrite())
                    {
                        Console.WriteLine("a is " + ia);
                    }
                }
            }
            else if (b)
            {
                using (UsableWriteLock.LockRead(wla, wlb))
                {
                    using (console.LockWrite())
                    {
                        Console.WriteLine("a is " + ia + ", b is " + ib);
                    }
                }
            }
            else if (c)
            {
                using (UsableWriteLock.LockRead(wla, wlb, wlc))
                {
                    using (console.LockWrite())
                    {
                        Console.WriteLine("a is " + ia + ", b is " + ib + ", c is " + ic);
                    }
                }
            }
        }

        private void Write(bool a, bool b, bool c)
        {
            if (a)
            {
                using (wla.LockWrite())
                {
                    using (console.LockWrite())
                    {
                        ia++;

                        Console.WriteLine("a => " + ia);
                    }
                }
            }
            else if (b)
            {
                using (UsableWriteLock.LockWrite(wla, wlb))
                {
                    using (console.LockWrite())
                    {
                        ia++;
                        ib++;

                        Console.WriteLine("a => " + ia + ", b => " + ib);
                    }
                }
            }
            else if (c)
            {
                using (UsableWriteLock.LockRead(wla, wlb, wlc))
                {
                    using (console.LockWrite())
                    {
                        ia++;
                        ib++;
                        ic++;

                        Console.WriteLine("a => " + ia + ", b => " + ib + ", c => " + ic);
                    }
                }
            }
        }
    }
}
