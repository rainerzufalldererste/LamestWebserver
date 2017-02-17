using System;
using System.Threading;
using LamestWebserver;
using LamestWebserver.Synchronization;
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
        private bool ba = false, bb = false, bc = false;
        UsableWriteLock console = new UsableWriteLock();

        private void Read(bool a, bool b, bool c)
        {
            if (a)
            {
                using (wla.LockRead())
                {
                    Assert.IsFalse(ba);

                    string s = " | " + (ba ? "1" : "0");

                    using (console.LockWrite())
                    {
                        Console.WriteLine("a is " + ia + s);
                    }

                    Assert.IsFalse(ba);
                }
            }
            else if (b)
            {
                using (UsableWriteLock.LockRead(wla, wlb))
                {
                    Assert.IsFalse(ba);
                    Assert.IsFalse(bb);

                    string s = " | " + (ba ? "1" : "0") + " | " + (bb ? "1" : "0");

                    using (console.LockWrite())
                    {
                        Console.WriteLine("a is " + ia + ", b is " + ib + s);
                    }

                    Assert.IsFalse(ba);
                    Assert.IsFalse(bb);
                }
            }
            else if (c)
            {
                using (UsableWriteLock.LockRead(wla, wlb, wlc))
                {
                    Assert.IsFalse(ba);
                    Assert.IsFalse(bb);
                    Assert.IsFalse(bc);

                    string s = " | " + (ba ? "1":"0") + " | " + (bb ? "1" : "0") + " | " + (bc ? "1" : "0");

                    using (console.LockWrite())
                    {
                        Console.WriteLine("a is " + ia + ", b is " + ib + ", c is " + ic + s);
                    }

                    Assert.IsFalse(ba);
                    Assert.IsFalse(bb);
                    Assert.IsFalse(bc);
                }
            }
        }

        private void Write(bool a, bool b, bool c)
        {
            if (a)
            {
                using (wla.LockWrite())
                {
                    ba = true;

                    ia++;

                    using (console.LockWrite())
                    {
                        Console.WriteLine("a => " + ia);
                    }

                    ba = false;
                }
            }
            else if (b)
            {
                using (UsableWriteLock.LockWrite(wla, wlb))
                {
                    ba = true;
                    bb = true;

                    ia++;
                    ib++;

                    using (console.LockWrite())
                    {
                        Console.WriteLine("a => " + ia + ", b => " + ib);
                    }

                    ba = false;
                    bb = false;
                }
            }
            else if (c)
            {
                using (UsableWriteLock.LockWrite(wla, wlb, wlc))
                {
                    ba = true;
                    bb = true;
                    bc = true;

                    ia++;
                    ib++;
                    ic++;

                    using (console.LockWrite())
                    {
                        Console.WriteLine("a => " + ia + ", b => " + ib + ", c => " + ic);
                    }

                    ba = false;
                    bb = false;
                    bc = false;
                }
            }
        }
    }
}
