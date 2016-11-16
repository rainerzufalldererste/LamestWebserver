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
        QueuedAVLTree<string, string> qtree;

        [Serializable]
        public class Person : IComparable, IEquatable<Person>
        {
            public int age;
            public string name;
            public int CompareTo(object obj) { if (!(obj is Person)) throw new NotImplementedException(); return this.age.CompareTo(((Person)obj).age); }
            public bool Equals(Person other) { return (other.name == name && other.age == age); }
            public override int GetHashCode()
            {
                return (age + name).GetHashCode();
            }
        };

        [Serializable]
        public class Couple { public Person man, woman; };

        [TestMethod]
        public void testSerializeClassAVLTree()
        {
            Console.WriteLine("Building Class AVLTree...");

            AVLTree<Person, Couple> tree = new AVLTree<Person, Couple>();
            int count = 1000;

            for (int i = 0; i < count; i++)
            {
                Person a = new Person() { age = i, name = "a" + i };
                Person b = new Person() { age = i, name = "b" + i };
                Couple c = new Couple() { man = a, woman = b };

                tree.Add(a, c);
                tree.Add(b, c);
            }

            Console.WriteLine("Serializing...");
            Serializer.writeData(tree, "tree");

            Console.WriteLine("Deserializing...");
            tree = Serializer.getData<AVLTree<Person, Couple>>("tree");

            Console.WriteLine("Validating...");
            for (int i = 0; i < count; i++)
            {
                Assert.IsTrue(tree[new Person() { age = i, name = "a" + i }].woman.Equals(new Person() { age = i, name = "b" + i }));
                Assert.IsTrue(tree[new Person() { age = i, name = "b" + i }].woman.Equals(new Person() { age = i, name = "b" + i }));
                Assert.IsTrue(tree[new Person() { age = i, name = "a" + i }].man.Equals(new Person() { age = i, name = "a" + i }));
                Assert.IsTrue(tree[new Person() { age = i, name = "b" + i }].man.Equals(new Person() { age = i, name = "a" + i }));
            }
        }

        [TestMethod]
        public void testSerializeClassAVLHashMap()
        {
            Console.WriteLine("Building Class AVLHashMap...");

            AVLHashMap<Person, Couple> hashmap = new AVLHashMap<Person, Couple>();
            int count = 1000;

            for (int i = 0; i < count; i++)
            {
                Person a = new Person() { age = i, name = "a" + i };
                Person b = new Person() { age = i, name = "b" + i };
                Couple c = new Couple() { man = a, woman = b };

                hashmap.Add(a, c);
                hashmap.Add(b, c);
            }

            Console.WriteLine("Serializing...");
            Serializer.writeData(hashmap, "hashmap");

            Console.WriteLine("Deserializing...");
            hashmap = Serializer.getData<AVLHashMap<Person, Couple>>("hashmap");

            Console.WriteLine("Validating...");
            for (int i = 0; i < count; i++)
            {
                Assert.IsTrue(hashmap[new Person() { age = i, name = "a" + i }].woman.Equals(new Person() { age = i, name = "b" + i }));
                Assert.IsTrue(hashmap[new Person() { age = i, name = "b" + i }].woman.Equals(new Person() { age = i, name = "b" + i }));
                Assert.IsTrue(hashmap[new Person() { age = i, name = "a" + i }].man.Equals(new Person() { age = i, name = "a" + i }));
                Assert.IsTrue(hashmap[new Person() { age = i, name = "b" + i }].man.Equals(new Person() { age = i, name = "a" + i }));
            }
        }

        [TestMethod]
        public void testSerializeClassQueuedAVLTree()
        {
            Console.WriteLine("Building Class QueuedAVLTree...");

            QueuedAVLTree<Person, Couple> qtree = new QueuedAVLTree<Person, Couple>();
            int count = 1000;

            for (int i = 0; i < count; i++)
            {
                Person a = new Person() { age = i, name = "a" + i };
                Person b = new Person() { age = i, name = "b" + i };
                Couple c = new Couple() { man = a, woman = b };

                qtree.Add(a, c);
                qtree.Add(b, c);
            }

            Console.WriteLine("Serializing...");
            Serializer.writeData(qtree, "qtree");

            Console.WriteLine("Deserializing...");
            qtree = Serializer.getData<QueuedAVLTree<Person, Couple>>("qtree");

            Console.WriteLine("Validating...");
            for (int i = 0; i < count; i++)
            {
                Assert.IsTrue(qtree[new Person() { age = i, name = "a" + i }].woman.Equals(new Person() { age = i, name = "b" + i }));
                Assert.IsTrue(qtree[new Person() { age = i, name = "b" + i }].woman.Equals(new Person() { age = i, name = "b" + i }));
                Assert.IsTrue(qtree[new Person() { age = i, name = "a" + i }].man.Equals(new Person() { age = i, name = "a" + i }));
                Assert.IsTrue(qtree[new Person() { age = i, name = "b" + i }].man.Equals(new Person() { age = i, name = "a" + i }));
            }
        }

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

        [TestMethod]
        public void testQueuedAVLTrees()
        {
            qtree = new QueuedAVLTree<string, string>(1);
            List<string> hashes = new List<string>();
            List<string> values = new List<string>();

            Assert.IsTrue(qtree.Count == 0);
            qtree.Validate();
            qtree.Add("wolo", "123");
            Assert.AreEqual(qtree["wolo"], "123");
            Assert.IsTrue(qtree.Count == 1);
            Assert.IsTrue(qtree.ContainsKey("wolo"));
            Assert.IsTrue(qtree.Contains(new KeyValuePair<string, string>("wolo", "123")));
            Assert.IsFalse(qtree.ContainsKey("wolo1"));
            Assert.IsFalse(qtree.Contains(new KeyValuePair<string, string>("wolo", "1234")));
            Assert.IsFalse(qtree.Contains(new KeyValuePair<string, string>("wolo1", "123")));
            Assert.IsFalse(qtree.Remove(new KeyValuePair<string, string>("wolo", "1234")));
            Assert.IsFalse(qtree.Remove(new KeyValuePair<string, string>("wolo1", "123")));
            Assert.IsTrue(qtree.Count == 1);
            qtree.Validate();
            qtree.Add("yolo", "0123");
            Assert.AreEqual(qtree["yolo"], "0123");
            Assert.IsTrue(qtree.Count == 1);
            Assert.IsTrue(qtree.ContainsKey("yolo"));
            Assert.IsTrue(qtree.Contains(new KeyValuePair<string, string>("yolo", "0123")));
            Assert.IsFalse(qtree.Contains(new KeyValuePair<string, string>("wolo", "123")));
            Assert.IsFalse(qtree.ContainsKey("wolo"));
            qtree.Validate();
            Assert.IsTrue(qtree.Remove(new KeyValuePair<string, string>("yolo", "0123")));
            Assert.IsTrue(qtree.Count == 0);
            Assert.IsFalse(qtree.Contains(new KeyValuePair<string, string>("yolo", "0123")));
            Assert.IsFalse(qtree.ContainsKey("yolo"));
            qtree.Validate();
            qtree["wolo"] = "abc";
            Assert.IsTrue(qtree.Count == 1);
            Assert.IsTrue(qtree.Remove("wolo"));
            qtree.Clear();
            Assert.IsTrue(qtree.Count == 0);
            qtree.Validate();

            qtree = new QueuedAVLTree<string, string>(10);
            executeTestQueuedTree(10);
            qtree.Clear();

            qtree = new QueuedAVLTree<string, string>(1024);
            executeTestQueuedTree(1024);
            qtree.Clear();
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

            Serializer.writeData(hashmap, nameof(hashmap));
            hashmap = Serializer.getData<AVLHashMap<string, string>>(nameof(hashmap));

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

            Serializer.writeData(hashmap, nameof(hashmap));
            hashmap = Serializer.getData<AVLHashMap<string, string>>(nameof(hashmap));

            hashmap.Validate();
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

            Serializer.writeData(tree, nameof(tree));
            tree = Serializer.getData<AVLTree<string, string>>(nameof(tree));

            tree.Validate();
        }

        public void executeTestQueuedTree(int size)
        {
            List<string> hashes = new List<string>();
            List<string> values = new List<string>();

            Console.WriteLine("Small Tests...");
            Assert.IsTrue(qtree.Count == 0);
            qtree.Add(new KeyValuePair<string, string>("key", "value"));
            Assert.IsTrue(qtree.ContainsKey("key"));
            Assert.IsFalse(qtree.ContainsKey("value"));
            Assert.IsTrue(qtree["key"] == "value");
            Assert.IsTrue(qtree.Count == 1);
            qtree.Validate();
            qtree.Add(new KeyValuePair<string, string>("key", "value2"));
            Assert.IsTrue(qtree.ContainsKey("key"));
            Assert.IsFalse(qtree.ContainsKey("value"));
            Assert.IsTrue(qtree["key"] == "value2");
            Assert.IsTrue(qtree.Count == 1);
            qtree.Validate();
            qtree.Add(new KeyValuePair<string, string>("key", "avalue"));
            Assert.IsTrue(qtree.ContainsKey("key"));
            Assert.IsFalse(qtree.ContainsKey("value"));
            Assert.IsTrue(qtree["key"] == "avalue");
            Assert.IsTrue(qtree.Count == 1);
            qtree.Validate();
            Assert.IsFalse(qtree.Remove("value"));
            Assert.IsTrue(qtree.Remove("key"));
            qtree.Validate();
            Assert.IsTrue(qtree.Count == 0);
            qtree.Add(new KeyValuePair<string, string>("key", "value2"));
            Assert.IsTrue(qtree.Count == 1);
            qtree.Clear();
            Assert.IsTrue(qtree.Count == 0);
            qtree.Add(new KeyValuePair<string, string>("key", "value"));
            Assert.IsTrue(qtree.ContainsKey("key"));
            Assert.IsFalse(qtree.ContainsKey("value"));
            Assert.IsTrue(qtree["key"] == "value");
            Assert.IsTrue(qtree.Count == 1);
            qtree.Validate();
            qtree.Clear();
            Assert.IsFalse(qtree.Remove(""));
            Assert.IsFalse(qtree.Remove(new KeyValuePair<string, string>("", "")));

            Console.WriteLine("Adding...");

            for (int i = 0; i < size; i++)
            {
                Assert.IsTrue(qtree.Count == i);
                hashes.Add(SessionContainer.generateHash());
                values.Add(SessionContainer.generateHash());
                qtree[hashes[i]] = values[i];
                Assert.IsTrue(qtree[hashes[i]] == values[i]);
                Assert.IsTrue(qtree.Keys.Contains(hashes[i]));
                Assert.IsTrue(qtree.Values.Contains(values[i]));
                qtree.Validate();
            }

            Console.WriteLine("Overriding...");

            for (int i = 0; i < size; i++)
            {
                Assert.IsTrue(qtree[hashes[i]] == values[i]);
                values[i] = SessionContainer.generateHash();
                qtree[hashes[i]] = values[i];
                Assert.IsTrue(qtree.Count == size);
            }

            Console.WriteLine("Checking...");

            for (int i = 0; i < size; i++)
            {
                Assert.IsTrue(qtree[hashes[i]] == values[i]);
                Assert.IsTrue(qtree.Keys.Contains(hashes[i]));
                Assert.IsTrue(qtree.Values.Contains(values[i]));
            }

            Console.WriteLine("Validating...");

            qtree.Validate();

            Console.WriteLine("Deleting...");
            
            for (int i = 0; i < size; i++)
            {
                Assert.IsTrue(qtree.Count == size - i);
                Assert.IsTrue(qtree.ContainsKey(hashes[i]));
                Assert.IsTrue(qtree[hashes[i]] != default(string));
                Assert.IsTrue(qtree.Remove(hashes[i]));
                Assert.IsFalse(qtree.Keys.Contains(hashes[i]));
                Assert.IsFalse(qtree.Values.Contains(values[i]));

                if (true)
                {
                    for (int j = i + 1; j < size; j++)
                    {
                        Assert.IsFalse(qtree[hashes[j]].Contains(hashes[j]));
                    }

                    for (int j = 0; j < i; j++)
                    {
                        Assert.IsFalse(qtree.Remove(hashes[j]));
                    }
                }

                Assert.IsTrue(qtree[hashes[i]] == default(string));
                qtree.Validate();
            }

            hashes.Clear();
            values.Clear();

            Console.WriteLine("Adding...");

            for (int i = 0; i < size; i++)
            {
                Assert.IsTrue(qtree.Count == i);
                hashes.Add(SessionContainer.generateHash());
                values.Add(SessionContainer.generateHash());
                qtree[hashes[i]] = values[i];
                Assert.IsTrue(qtree[hashes[i]] == values[i]);
                Assert.IsTrue(qtree.Keys.Contains(hashes[i]));
                Assert.IsTrue(qtree.Values.Contains(values[i]));
                qtree.Validate();
            }

            Console.WriteLine("Overflowing...");

            for (int i = 0; i < size; i++)
            {
                Assert.IsTrue(qtree.Count == size);
                hashes.Add(SessionContainer.generateHash());
                values.Add(SessionContainer.generateHash());
                qtree[hashes[size + i]] = values[size + i];
                Assert.IsTrue(qtree[hashes[size + i]] == values[size + i]);
                Assert.IsTrue(qtree.Keys.Contains(hashes[size + i]));
                Assert.IsTrue(qtree.Values.Contains(values[size + i]));
                qtree.Validate();
            }

            Console.WriteLine("Overriding...");

            for (int i = 0; i < size; i++)
            {
                values[i] = SessionContainer.generateHash();
                qtree[hashes[size + i]] = values[size + i];
                Assert.IsTrue(qtree[hashes[size + i]] == values[size + i]);
                Assert.IsTrue(qtree.Keys.Contains(hashes[size + i]));
                Assert.IsTrue(qtree.Values.Contains(values[size + i]));
                Assert.IsTrue(qtree.Count == size);
                qtree.Validate();
            }

            Console.WriteLine("Validating...");

            qtree.Validate();

            Console.WriteLine("Deleting...");

            for (int i = 0; i < size; i++)
            {
                Assert.IsTrue(qtree.Count == size - i);
                Assert.IsTrue(qtree.ContainsKey(hashes[size + i]));
                Assert.IsTrue(qtree[hashes[size + i]] != default(string));
                Assert.IsTrue(qtree.Remove(hashes[size + i]));
                Assert.IsFalse(qtree.Keys.Contains(hashes[size + i]));
                Assert.IsFalse(qtree.Values.Contains(values[size + i]));

                if (true)
                {
                    for (int j = i + 1; j < size; j++)
                    {
                        Assert.IsFalse(qtree[hashes[size + j]].Contains(hashes[size + j]));
                    }

                    for (int j = 0; j < i; j++)
                    {
                        Assert.IsFalse(qtree.Remove(hashes[size + j]));
                    }
                }

                Assert.IsTrue(qtree[hashes[size + i]] == default(string));
                qtree.Validate();
            }
        }
    }
}
