using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LamestWebserver.Collections;

namespace UnitTests
{
    [TestClass]
    public class WalkableQueueTest
    {
        [TestMethod]
        public void TestWalkableQueue()
        {
            WalkableQueue<string> walkableQueue = new WalkableQueue<string>();

            Assert.AreEqual(0, walkableQueue.Count);
            Assert.IsTrue(walkableQueue.AtEnd());

            walkableQueue.Push("");

            Assert.IsTrue(walkableQueue.AtEnd());
            Assert.AreEqual(1, walkableQueue.Count);
            Assert.AreEqual("", walkableQueue.Peek());
            Assert.AreEqual("", walkableQueue.Pop());
            Assert.IsTrue(walkableQueue.AtEnd());

            walkableQueue.Push("a");

            Assert.AreEqual(2, walkableQueue.Count);
            Assert.AreEqual("a", walkableQueue.Peek());
            Assert.AreEqual("", walkableQueue.Current);
            Assert.AreEqual("a", walkableQueue.Pop());
            Assert.IsTrue(walkableQueue.AtEnd());

            List<string> passed = walkableQueue.GetPassed();

            Assert.AreEqual(2, passed.Count);
            Assert.AreEqual("", passed[0]);
            Assert.AreEqual("a", passed[1]);

            walkableQueue.Push("b");
            walkableQueue.Push("c");
            walkableQueue.Push("d");

            List<string> consumable = walkableQueue.GetConsumable();

            Assert.AreEqual(3, consumable.Count);
            Assert.AreEqual("b", consumable[0]);
            Assert.AreEqual("c", consumable[1]);
            Assert.AreEqual("d", consumable[2]);

            Assert.IsFalse(walkableQueue.AtEnd());
            Assert.AreEqual(5, walkableQueue.Count);
            Assert.AreEqual("a", walkableQueue.Current);
            Assert.AreEqual("b", walkableQueue.Peek());
            Assert.IsFalse(walkableQueue.AtEnd());
            Assert.AreEqual("b", walkableQueue.Pop());
            Assert.IsFalse(walkableQueue.AtEnd());
            Assert.AreEqual("c", walkableQueue.Peek());
            Assert.AreEqual("b", walkableQueue.Current);
            Assert.IsFalse(walkableQueue.AtEnd());
            Assert.AreEqual("c", walkableQueue.Pop());
            Assert.AreEqual("c", walkableQueue.Current);
            Assert.IsTrue(walkableQueue.AtEnd());
            Assert.AreEqual("d", walkableQueue.Pop());
            Assert.IsTrue(walkableQueue.AtEnd());

            List<string> all = walkableQueue.GetAll();

            Assert.AreEqual(5, all.Count);

            walkableQueue.ResetPosition();

            Assert.AreEqual("", walkableQueue.Pop());
            Assert.AreEqual("a", walkableQueue.Pop());
            Assert.AreEqual("b", walkableQueue.Pop());
            Assert.AreEqual("c", walkableQueue.Pop());
            Assert.AreEqual("d", walkableQueue.Pop());

            walkableQueue.Clear();
            Assert.AreEqual(0, walkableQueue.Count);

            walkableQueue = new WalkableQueue<string>(new List<string> { "0", "1", "2", "3", "4" });

            Assert.AreEqual(5, walkableQueue.Count);

            for (int i = 0; i < 5; i++)
            {
                Assert.AreEqual(i.ToString(), walkableQueue[i]);
                walkableQueue[i] = new string('-', i);
                Assert.AreEqual(new string('-', i), walkableQueue[i]);
            }

            int index = 0;

            foreach (string s in walkableQueue)
                Assert.AreEqual(index++, s.Length);

            List<string> range = walkableQueue.GetRange(2, 2);

            index = 2;
            
            foreach (string s in range)
                Assert.AreEqual(index++, s.Length);
        }
    }
}
