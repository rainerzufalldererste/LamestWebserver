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
            AVLHashMap<string, int> hashmap = new AVLHashMap<string, int>(20);

            for (int i = 0; i < 8000; i++)
            {
                hashmap.Add(i.ToString(), i + 1);
            }

            for (int i = 0; i < 8000; i++)
            {
                Assert.IsTrue(hashmap[i.ToString()] == i + 1);
            }

            for (int i = 0; i < hashmap.HashMap.Length; i++)
            {
                if(hashmap.HashMap[i] != null && !(hashmap.HashMap[i] is KeyValuePair<string, int>))
                {
                    Console.WriteLine(hashmap.ToString() + "\n\n");
                }
            }
        }
    }
}
