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
        AVLHashMap<string, string> hashmap;
        AVLTree<string, string> tree;

        [TestMethod]
        public void testAVLHashMaps()
        {
            hashmap = new AVLHashMap<string, string>(1);
            executeTestHashMap();
            hashmap.Clear();
            
            hashmap = new AVLHashMap<string, string>(10);
            executeTestHashMap();
            hashmap.Clear();

            hashmap = new AVLHashMap<string, string>(1024);
            executeTestHashMap();
            hashmap.Clear();
        }

        [TestMethod]
        public void testAVLTrees()
        {
            tree = new AVLTree<string, string>();
            executeTestTree();
            tree.Clear();
        }

        public void executeTestHashMap()
        {
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
            Assert.IsFalse(hashmap.Remove(""));
            Assert.IsFalse(hashmap.Remove(new KeyValuePair<string, string>("", "")));

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

        public void executeTestTree()
        {
            List<string> hashes = new List<string>();
            List<string> values = new List<string>();
            const int size = 1000;

            Console.WriteLine("Small Tests...");
            Assert.IsTrue(tree.Count == 0);
            tree.Add(new KeyValuePair<string, string>("key", "value"));
            Assert.IsTrue(tree.ContainsKey("key"));
            Assert.IsFalse(tree.ContainsKey("value"));
            Assert.IsTrue(tree["key"] == "value");
            Assert.IsTrue(tree.Count == 1);
            tree.Validate();
            tree.Add(new KeyValuePair<string, string>("key", "value2"));
            Assert.IsTrue(tree.ContainsKey("key"));
            Assert.IsFalse(tree.ContainsKey("value"));
            Assert.IsTrue(tree["key"] == "value2");
            Assert.IsTrue(tree.Count == 1);
            tree.Validate();
            tree.Add(new KeyValuePair<string, string>("key", "avalue"));
            Assert.IsTrue(tree.ContainsKey("key"));
            Assert.IsFalse(tree.ContainsKey("value"));
            Assert.IsTrue(tree["key"] == "avalue");
            Assert.IsTrue(tree.Count == 1);
            tree.Validate();
            Assert.IsFalse(tree.Remove("value"));
            Assert.IsTrue(tree.Remove("key"));
            tree.Validate();
            Assert.IsTrue(tree.Count == 0);
            tree.Add(new KeyValuePair<string, string>("key", "value2"));
            Assert.IsTrue(tree.Count == 1);
            tree.Clear();
            Assert.IsTrue(tree.Count == 0);
            tree.Add(new KeyValuePair<string, string>("key", "value"));
            Assert.IsTrue(tree.ContainsKey("key"));
            Assert.IsFalse(tree.ContainsKey("value"));
            Assert.IsTrue(tree["key"] == "value");
            Assert.IsTrue(tree.Count == 1);
            tree.Validate();
            tree.Clear();
            Assert.IsFalse(tree.Remove(""));
            Assert.IsFalse(tree.Remove(new KeyValuePair<string, string>("", "")));

            Console.WriteLine("Adding...");

            for (int i = 0; i < size; i++)
            {
                Assert.IsTrue(tree.Count == i);
                hashes.Add(SessionContainer.generateHash());
                values.Add(SessionContainer.generateHash());
                tree[hashes[i]] = values[i];
                Assert.IsTrue(tree[hashes[i]] == values[i]);
                Assert.IsTrue(tree.Keys.Contains(hashes[i]));
                Assert.IsTrue(tree.Values.Contains(values[i]));
                tree.Validate();
            }

            Console.WriteLine("Overriding...");

            for (int i = 0; i < size; i++)
            {
                Assert.IsTrue(tree[hashes[i]] == values[i]);
                values[i] = SessionContainer.generateHash();
                tree[hashes[i]] = values[i];
                Assert.IsTrue(tree.Count == size);
            }

            Console.WriteLine("Checking...");

            for (int i = 0; i < size; i++)
            {
                Assert.IsTrue(tree[hashes[i]] == values[i]);
                Assert.IsTrue(tree.Keys.Contains(hashes[i]));
                Assert.IsTrue(tree.Values.Contains(values[i]));
            }

            Console.WriteLine("Validating...");

            tree.Validate();

            Console.WriteLine("Deleting...");

            for (int i = 0; i < size; i++)
            {
                Assert.IsTrue(tree.Count == size - i);
                Assert.IsTrue(tree.ContainsKey(hashes[i]));
                Assert.IsTrue(tree[hashes[i]] != default(string));
                Assert.IsTrue(tree.Remove(hashes[i]));
                Assert.IsFalse(tree.Keys.Contains(hashes[i]));
                Assert.IsFalse(tree.Values.Contains(values[i]));

                if (true)
                {
                    for (int j = i + 1; j < size; j++)
                    {
                        Assert.IsFalse(tree[hashes[j]].Contains(hashes[j]));
                    }

                    for (int j = 0; j < i; j++)
                    {
                        Assert.IsFalse(tree.Remove(hashes[j]));
                    }
                }

                Assert.IsTrue(tree[hashes[i]] == default(string));
                tree.Validate();
            }
        }
    }
}
