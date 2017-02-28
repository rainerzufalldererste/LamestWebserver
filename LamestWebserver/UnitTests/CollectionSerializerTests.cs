using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using LamestWebserver;
using LamestWebserver.Collections;
using LamestWebserver.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
    [TestClass]
    public class CollectionSerializerTests
    {
        [Serializable]
        public class TestKey : IEquatable<TestKey>, IComparable
        {
            public string testString;
            public int testInt;

            /// <inheritdoc />
            public bool Equals(TestKey other)
            {
                return testString == other.testString && testInt == other.testInt;
            }

            /// <inheritdoc />
            public int CompareTo(object obj)
            {
                if (obj is TestKey)
                    return testString.CompareTo((obj as TestKey).testString);

                throw new InvalidOperationException();
            }
        }

        [Serializable]
        public class TestValue
        {
            public TestKey key;
            public string testString;
        }

        [TestMethod]
        public void testCollectionJsonSerialisazion()
        {
            AVLTree<TestKey, TestValue> avlTree = new AVLTree<TestKey, TestValue>();
            Dictionary<TestKey, TestValue> referenceDictionary = new Dictionary<TestKey, TestValue>();

            for (int i = 0; i < 1024; i++)
            {
                string value = SessionContainer.GenerateHash();
                var testKey = new TestKey() {testInt = i, testString = value};

                avlTree.Add(new KeyValuePair<TestKey, TestValue>(testKey, new TestValue() {testString = value, key = testKey}));
                referenceDictionary.Add(testKey, new TestValue() {testString = value, key = testKey});
            }

            Serializer.WriteBinaryData(avlTree, "avltree.bin");
            avlTree = Serializer.ReadBinaryData<AVLTree<TestKey, TestValue>>("avltree.bin");

            Assert.IsTrue(avlTree.Count == 1024);

            foreach (var element in referenceDictionary)
            {
                Assert.IsTrue(avlTree[element.Key] != null);
            }
        }
    }
}
