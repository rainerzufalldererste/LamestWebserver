using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LamestWebserver;
using LamestWebserver.Collections;
using System.Collections.Generic;

namespace UnitTests
{
    [TestClass]
    public class CollectionUnitTests
    {
        [TestMethod]
        public void testAVLHashMaps()
        {
            AVLHashMap<string, string> hashmap = new AVLHashMap<string, string>(1);
            List<string> hashes = new List<string>();
            List<string> values = new List<string>();

            for (int i = 0; i < 1000; i++)
            {
                hashes.Add(SessionContainer.generateHash());
                values.Add(SessionContainer.generateHash());
                hashmap[hashes[i]] = values[i];
                Assert.IsTrue(hashmap[hashes[i]] == values[i]);
            }

            for (int i = 0; i < 1000; i++)
            {
                Assert.IsTrue(hashmap[hashes[i]] == values[i]);
            }
        }
    }
}
