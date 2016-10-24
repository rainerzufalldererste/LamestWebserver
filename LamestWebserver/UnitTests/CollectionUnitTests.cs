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
            const int size = 1000;

            Console.WriteLine("Small Tests...");
            Assert.IsTrue(hashmap.Count == 0);
            hashmap.Add(new KeyValuePair<string, string>("key", "value"));
            Assert.IsTrue(hashmap.ContainsKey("key"));
            Assert.IsFalse(hashmap.ContainsKey("value"));
            Assert.IsTrue(hashmap["key"] == "value");
            Assert.IsTrue(hashmap.Count == 1);
            hashmap.Validate();
            hashmap.Add(new KeyValuePair<string, string>("key", "value2"));
            Assert.IsTrue(hashmap.ContainsKey("key"));
            Assert.IsFalse(hashmap.ContainsKey("value"));
            Assert.IsTrue(hashmap["key"] == "value2");
            Assert.IsTrue(hashmap.Count == 1);
            hashmap.Validate();
            hashmap.Add(new KeyValuePair<string, string>("key", "avalue"));
            Assert.IsTrue(hashmap.ContainsKey("key"));
            Assert.IsFalse(hashmap.ContainsKey("value"));
            Assert.IsTrue(hashmap["key"] == "avalue");
            Assert.IsTrue(hashmap.Count == 1);
            hashmap.Validate();
            Assert.IsFalse(hashmap.Remove("value"));
            Assert.IsTrue(hashmap.Remove("key"));
            hashmap.Validate();
            Assert.IsTrue(hashmap.Count == 0);
            hashmap.Add(new KeyValuePair<string, string>("key", "value2"));
            Assert.IsTrue(hashmap.Count == 1);
            hashmap.Clear();
            Assert.IsTrue(hashmap.Count == 0);
            hashmap.Add(new KeyValuePair<string, string>("key", "value"));
            Assert.IsTrue(hashmap.ContainsKey("key"));
            Assert.IsFalse(hashmap.ContainsKey("value"));
            Assert.IsTrue(hashmap["key"] == "value");
            Assert.IsTrue(hashmap.Count == 1);
            hashmap.Validate();
            hashmap.Clear();

            Console.WriteLine("Adding...");

            for (int i = 0; i < size; i++)
            {
                Assert.IsTrue(hashmap.Count == i);
                hashes.Add(SessionContainer.generateHash());
                values.Add(SessionContainer.generateHash());
                hashmap[hashes[i]] = values[i];
                Assert.IsTrue(hashmap[hashes[i]] == values[i]);
                Assert.IsTrue(hashmap.Keys.Contains(hashes[i]));
                Assert.IsTrue(hashmap.Values.Contains(values[i]));
                hashmap.Validate();
            }

            Console.WriteLine("Overriding...");

            for (int i = 0; i < size; i++)
            {
                Assert.IsTrue(hashmap[hashes[i]] == values[i]);
                values[i] = SessionContainer.generateHash();
                hashmap[hashes[i]] = values[i];
                Assert.IsTrue(hashmap.Count == size);
            }

            Console.WriteLine("Checking...");

            for (int i = 0; i < size; i++)
            {
                Assert.IsTrue(hashmap[hashes[i]] == values[i]);
                Assert.IsTrue(hashmap.Keys.Contains(hashes[i]));
                Assert.IsTrue(hashmap.Values.Contains(values[i]));
            }

            Console.WriteLine("Validating...");

            hashmap.Validate();

            Console.WriteLine("Deleting...");
            
            for (int i = 0; i < size; i++)
            {
                Assert.IsTrue(hashmap.Count == size - i);
                Assert.IsTrue(hashmap.ContainsKey(hashes[i]));
                Assert.IsTrue(hashmap[hashes[i]] != default(string));
                Assert.IsTrue(hashmap.Remove(hashes[i]));
                Assert.IsFalse(hashmap.Keys.Contains(hashes[i]));
                Assert.IsFalse(hashmap.Values.Contains(values[i]));

                if (true)
                {
                    for (int j = i + 1; j < size; j++)
                    {
                        Assert.IsFalse(hashmap[hashes[j]].Contains(hashes[j]));
                    }

                    for (int j = 0; j < i; j++)
                    {
                        Assert.IsFalse(hashmap.Remove(hashes[j]));
                    }
                }

                Assert.IsTrue(hashmap[hashes[i]] == default(string));
                hashmap.Validate();
            }
        }
    }
}
