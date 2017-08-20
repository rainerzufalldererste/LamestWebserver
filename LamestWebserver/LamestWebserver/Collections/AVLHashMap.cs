using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml.Schema;
using System.Xml;
using System.Xml.Serialization;
using LamestWebserver.Serialization;

namespace LamestWebserver.Collections
{
    /// <summary>
    /// A HashMap which uses AVLTrees inside to access Values very fast.
    /// Returns default(T) / null if element not found.
    /// implements ISerializable, IXmlSerializable
    /// </summary>
    /// <typeparam name="TKey">The Type of the Keys (implement IComparable, IEquatable&lt;TKey&gt;)</typeparam>
    /// <typeparam name="TValue">The Type of the Values</typeparam>
    [Serializable]
    public class AVLHashMap<TKey, TValue> : Core.NullCheckable, IDictionary<TKey, TValue>, ISerializable, IXmlSerializable where TKey : IEquatable<TKey>, IComparable
    {
        private int _size = 1024;
        private int _elementCount = 0;
        
        internal object[] HashMap { get; private set; }

        /// <summary>
        /// Constructs a new AVLHashmap of the specified size
        /// </summary>
        /// <param name="size">the size of the hashmap</param>
        public AVLHashMap(int size)
        {
            this._size = size;
            HashMap = new object[size];
        }

        /// <summary>
        /// Constructs a new AVLHashmep with a size of 1024
        /// </summary>
        public AVLHashMap()
        {
            this._size = 1024;
            HashMap = new object[_size];
        }

        /// <inheritdoc />
        public ICollection<TKey> Keys
        {
            get
            {
                List<TKey> ret = new List<TKey>();

                for (int i = 0; i < HashMap.Length; i++)
                {
                    if (HashMap[i] == null)
                        continue;
                    else if (HashMap[i] is KeyValuePair<TKey, TValue>)
                        ret.Add(((KeyValuePair<TKey, TValue>)HashMap[i]).Key);
                    else
                        ret.AddRange(((AVLNode)HashMap[i]).GetSortedKeys());
                }

                return ret;
            }
        }

        /// <inheritdoc />
        public ICollection<TValue> Values
        {
            get
            {
                List<TValue> ret = new List<TValue>();

                for (int i = 0; i < HashMap.Length; i++)
                {
                    if (HashMap[i] == null)
                        continue;
                    else if (HashMap[i] is KeyValuePair<TKey, TValue>)
                        ret.Add(((KeyValuePair<TKey, TValue>)HashMap[i]).Value);
                    else
                        ret.AddRange(((AVLNode)HashMap[i]).GetSorted());
                }

                return ret;
            }
        }

        /// <inheritdoc />
        public int Count => _elementCount;

        /// <summary>
        /// Used for UnitTests.
        /// </summary>
        public void Validate()
        {
            int size = 0;

            for (int i = 0; i < HashMap.Length; i++)
            {
                if (HashMap[i] is AVLNode)
                {
                    AVLNode.CheckNodes((AVLNode)HashMap[i]);
                    size += AVLNode.GetCount((AVLNode)HashMap[i]);
                }
                else if (HashMap[i] != null)
                {
                    System.Diagnostics.Debug.Assert(Math.Abs(((KeyValuePair<TKey, TValue>)HashMap[i]).Key.GetHashCode()) % this._size == i, "The InnerSerializableKeyValuePair hash would resolve to a different spot than it lives in.");
                    size++;
                }
            }

            System.Diagnostics.Debug.Assert(size == _elementCount, "The elementCount is " + _elementCount + " but should be " + size);
        }

        /// <inheritdoc />
        public bool IsReadOnly => false;

        /// <inheritdoc />
        public TValue this[TKey key]
        {
            get
            {
                if (key == null)
                    return default(TValue);

                int hash = Math.Abs(key.GetHashCode()) % _size;

                if (HashMap[hash] == null)
                {
                    return default(TValue);
                }
                else if (HashMap[hash] is KeyValuePair<TKey, TValue>)
                {
                    if (((KeyValuePair<TKey, TValue>)HashMap[hash]).Key.Equals(key))
                        return ((KeyValuePair<TKey, TValue>)HashMap[hash]).Value;
                    else
                        return default(TValue);
                }
                else // if HashMap[hash] is an AVL Node search for it
                {
                    AVLNode node = (AVLNode)HashMap[hash];
                    int compare = key.CompareTo(node.key);

                    while (true)
                    {
                        if (compare < 0)
                        {
                            node = node.left;

                            if (node != null)
                                compare = key.CompareTo(node.key);
                            else
                                return default(TValue);
                        }
                        else if (compare > 0)
                        {
                            node = node.right;

                            if (node != null)
                                compare = key.CompareTo(node.key);
                            else
                                return default(TValue);
                        }
                        else
                        {
                            return node.value;
                        }
                    }
                }
            }

            set
            {
                Add(new KeyValuePair<TKey, TValue>(key, value));
            }
        }

        /// <inheritdoc />
        public bool ContainsKey(TKey key)
        {
            int hash = Math.Abs(key.GetHashCode()) % _size;

            if (HashMap[hash] == null)
            {
                return false;
            }
            else if (HashMap[hash] is KeyValuePair<TKey, TValue>)
            {
                if (((KeyValuePair<TKey, TValue>)HashMap[hash]).Key.Equals(key))
                    return true;
                else
                    return false;
            }
            else // if HashMap[hash] is an AVL Node search for it
            {
                AVLNode node = (AVLNode)HashMap[hash];
                int compare = key.CompareTo(node.key);

                while (true)
                {
                    if (compare < 0)
                    {
                        node = node.left;

                        if (node != null)
                            compare = key.CompareTo(node.key);
                        else
                            return false;
                    }
                    else if (compare > 0)
                    {
                        node = node.right;

                        if (node != null)
                            compare = key.CompareTo(node.key);
                        else
                            return false;
                    }
                    else
                    {
                        return true;
                    }
                }
            }
        }

        /// <inheritdoc />
        public void Add(TKey key, TValue value)
        {
            Add(new KeyValuePair<TKey, TValue>(key, value));
        }

        /// <inheritdoc />
        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            int hash = Math.Abs(item.Key.GetHashCode()) % _size;

            if (HashMap[hash] is KeyValuePair<TKey, TValue>)
            {
                if (((KeyValuePair<TKey, TValue>)HashMap[hash]).Key.Equals(item.Key) && ((KeyValuePair<TKey, TValue>)HashMap[hash]).Value.Equals(item.Value))
                {
                    HashMap[hash] = null;
                    _elementCount--;
                    return true;
                }
                else return false;
            }
            else if (HashMap[hash] is AVLNode)
            {
                AVLNode node = (AVLNode)HashMap[hash];

                return AVLNode.FindRemoveItem(node, HashMap, hash, item, ref _elementCount);
            }

            return false; // Redundant
        }

        /// <inheritdoc />
        public bool TryGetValue(TKey key, out TValue value)
        {
            if (key == null)
            {
                value = default(TValue);
                return false;
            }

            value = this[key];
            return value != null && !value.Equals(default(TValue));
        }

        /// <inheritdoc />
        public void Add(KeyValuePair<TKey, TValue> item)
        {
            int hash = Math.Abs(item.Key.GetHashCode()) % _size;

            if (HashMap[hash] == null)
            {
                HashMap[hash] = item;
                _elementCount++;
            }
            else if (HashMap[hash] is KeyValuePair<TKey, TValue>)
            {
                if (((KeyValuePair<TKey, TValue>)HashMap[hash]).Key.Equals(item.Key))
                {
                    HashMap[hash] = item;
                }
                else
                {
                    AVLNode node = new AVLNode(key: ((KeyValuePair<TKey, TValue>)HashMap[hash]).Key, value: ((KeyValuePair<TKey, TValue>)HashMap[hash]).Value) { head = null };

                    if (item.Key.CompareTo(((KeyValuePair<TKey, TValue>)HashMap[hash]).Key) < 0)
                    {
                        node.left = new AVLNode(key: item.Key, value: item.Value) { head = node, isLeft = true };
                        node._depthL = 1;
                    }
                    else
                    {
                        node.right = new AVLNode(key: item.Key, value: item.Value) { head = node, isLeft = false };
                        node._depthR = 1;
                    }

                    HashMap[hash] = node;
                    _elementCount++;
                }
            }
            else
            {
                AVLNode node = (AVLNode)HashMap[hash];

                AVLNode.AddItem(node: node, item: item, HashMap: HashMap, hash: hash, elementCount: ref _elementCount);
#if TEST
                AVLNode.checkNodes(node);
#endif
            }
        }

        /// <inheritdoc />
        public void Clear()
        {
            _elementCount = 0;
            HashMap = new object[_size];
        }

        /// <inheritdoc />
        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            int hash = Math.Abs(item.Key.GetHashCode()) % _size;

            if (HashMap[hash] == null)
            {
                return false;
            }
            else if (HashMap[hash] is KeyValuePair<TKey, TValue>)
            {
                if (((KeyValuePair<TKey, TValue>)HashMap[hash]).Key.Equals(item.Key) && ((KeyValuePair<TKey, TValue>)HashMap[hash]).Value.Equals(item.Value))
                    return true;
                else
                    return false;
            }
            else // if HashMap[hash] is an AVL Node search for it
            {
                AVLNode node = (AVLNode)HashMap[hash];
                int compare = item.Key.CompareTo(node.key);

                while (true)
                {
                    if (compare < 0)
                    {
                        node = node.left;

                        if (node != null)
                            compare = item.Key.CompareTo(node.key);
                        else
                            return false;
                    }
                    else if (compare > 0)
                    {
                        node = node.right;

                        if (node != null)
                            compare = item.Key.CompareTo(node.key);
                        else
                            return false;
                    }
                    else
                    {
                        return node.value.Equals(item.Value);
                    }
                }
            }
        }

        /// <inheritdoc />
        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            foreach (KeyValuePair<TKey, TValue> keyValuePair in this)
                array[arrayIndex++] = keyValuePair;
        }

        /// <inheritdoc />
        public bool Remove(TKey key)
        {
            int hash = Math.Abs(key.GetHashCode()) % _size;

            if (HashMap[hash] is KeyValuePair<TKey, TValue>)
            {
                if (((KeyValuePair<TKey, TValue>)HashMap[hash]).Key.Equals(key))
                {
                    HashMap[hash] = null;
                    _elementCount--;
                    return true;
                }
                else return false;
            }
            else if (HashMap[hash] is AVLNode)
            {
                AVLNode node = (AVLNode)HashMap[hash];

                return AVLNode.FindRemoveKey(node, HashMap, hash, key, ref _elementCount);
            }

            return false; // Redundant
        }

        /// <inheritdoc />
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            List<KeyValuePair<TKey, TValue>> list = new List<KeyValuePair<TKey, TValue>>();

            for (int i = 0; i < HashMap.Length; i++)
            {
                if (HashMap[i] is KeyValuePair<TKey, TValue>)
                    list.Add((KeyValuePair<TKey, TValue>)HashMap[i]);
                else if (HashMap[i] is AVLNode)
                    list.AddRange(((AVLNode)HashMap[i]).GetAllData());
            }

            return list.GetEnumerator();
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            List<TValue> list = new List<TValue>();

            for (int i = 0; i < HashMap.Length; i++)
            {
                if (HashMap[i] is KeyValuePair<TKey, TValue>)
                    list.Add((((KeyValuePair<TKey, TValue>)HashMap[i]).Value));
                else if (HashMap[i] is AVLNode)
                    list.AddRange(((AVLNode)HashMap[i]).GetSorted());
            }

            return list.GetEnumerator();
        }

        /// <inheritdoc />
        //[OnSerializing]
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(_size), _size);

            Entry[] elements = new Entry[this.Count];
            int index = 0;

            foreach (var element in this)
                elements[index++] = element;

            info.AddValue(nameof(elements), elements);
        }

        /// <inheritdoc />
        public XmlSchema GetSchema()
        {
            return null;
        }

        /// <inheritdoc />
        public void ReadXml(XmlReader reader)
        {
            reader.ReadStartElement();
            reader.ReadStartElement();

            _size = reader.ReadElement<int>();

            HashMap = new object[_size];

            List<Entry> entries = reader.ReadElement<List<Entry>>();

            foreach (Entry e in entries)
                this[e.Key] = e.Value;

            reader.ReadToEndElement("AVLHashMap");
            reader.ReadEndElement();
        }

        /// <inheritdoc />
        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement("AVLHashMap");

            writer.WriteElement("Size", _size);

            Entry[] elements = new Entry[this.Count];
            int index = 0;

            foreach (var element in this)
            {
                elements[index++] = element;
            }

            writer.WriteElement("Elements", elements);

            writer.WriteEndElement();
        }

        /// <summary>
        /// Only used for Serializing
        /// </summary>
        [Serializable]
        public struct Entry
        {
            /// <summary>
            /// The Key
            /// </summary>
            public TKey Key { get; set; }

            /// <summary>
            /// The Value
            /// </summary>
            public TValue Value { get; set; }

            /// <summary>
            /// Constructs a new Entry
            /// </summary>
            /// <param name="key">the Key</param>
            /// <param name="value">the Value</param>
            public Entry(TKey key, TValue value)
            {
                this.Key = key;
                this.Value = value;
            }

            /// <summary>
            /// Casts a KeyValuePair to an Entry
            /// </summary>
            /// <param name="input">the KeyValuePair</param>
            /// <returns>the Entry</returns>
            public static implicit operator Entry(KeyValuePair<TKey, TValue> input)
            {
                return new Entry(input.Key, input.Value);
            }
        }

        /// <summary>
        /// Deserializes an AVLHashmap
        /// </summary>
        /// <param name="info">SerializationInfo</param>
        /// <param name="context">StreamingContext</param>
        public AVLHashMap(SerializationInfo info, StreamingContext context)
        {
            _size = info.GetInt32(nameof(_size));
            HashMap = new object[_size];

            Entry[] elements;
            elements = (Entry[])info.GetValue(nameof(elements), typeof(Entry[]));

            foreach (var e in elements)
                this[e.Key] = e.Value;
        }
        
        internal class AVLNode
        {
            internal AVLNode head;

            internal AVLNode left, right;
            internal int balance { get { return -_depthL + _depthR; } }
            internal int _depthL = 0;
            internal int _depthR = 0;
            internal TKey key;
            internal TValue value;
            internal bool isLeft = true;

            /// <summary>
            /// Empty constructor for Deserialisation
            /// </summary>
            internal AVLNode()
            {

            }

            internal AVLNode(TKey key, TValue value)
            {
                this.key = key;
                this.value = value;
            }

            private void Rebalance(object[] HashMap, int index)
            {
                if (Math.Abs(balance) > 2)
                {
                    if (balance < -2)
                    {
                        left.Rebalance(HashMap, index);
                    }
                    else
                    {
                        right.Rebalance(HashMap, index);
                    }
                }

                if (balance > 1)
                {
                    //          5_2             |              7
                    //      2        7_1        |        5          8
                    //            6       8_1   |     2      6          9
                    //                       9  |

                    //            5             |              5
                    //     2            6       |       3           6
                    //        3                 |    2      4
                    //         4                |

                    //          5               |          5                |           7
                    //    2           8         |     2        7            |      5          8
                    //             7     9      |            6    8         |   2     6          9
                    //            6             |                   9       |

                    if (right.balance > 0)
                    {
                        RotateLeft(right, HashMap, index);
                    }
                    else
                    {
                        RotateRight(right.left, HashMap, index);

                        RotateLeft(right, HashMap, index);
                    }
                }
                else if (balance < -1)
                {
                    //              5           |            5
                    //         4          9     |       4          8
                    //                 8        |                7   9
                    //                7         |

                    //            5             |              5            |           3
                    //     1            6       |       3           6       |      1           5
                    //  0     3                 |    1     4                |   0           4      6
                    //          4               |  0                        |

                    if (left.balance < 0)
                    {
                        RotateRight(left, HashMap, index);
                    }
                    else
                    {
                        RotateLeft(left.right, HashMap, index);

                        RotateRight(left, HashMap, index);
                    }
                }

#if TEST
                checkNodeSelf(this);
#endif
            }

            private static void CheckHeads(AVLNode node, TKey key)
            {
                if (node.head != null)
                {
                    System.Diagnostics.Debug.Assert(node.head.key.Equals(key), "Mismatching Head Keys: (is " + node.head.key + " should be " + key + ")", node.head.ToString());
                }

                if (node.right != null)
                    CheckHeads(node.right, node.key);

                if (node.left != null)
                    CheckHeads(node.left, node.key);
            }

            private static void CheckOrder(AVLNode node)
            {
                var list = node.GetSortedKeys();

                for (int i = 0; i < list.Count - 1; i++)
                {
                    System.Diagnostics.Debug.Assert(list[i].CompareTo(list[i + 1]) < 0, "Unordered Keys");
                }
            }

            internal static void CheckNodes(AVLNode node)
            {
                while (node.head != null)
                {
                    node = node.head;
                }

                CheckSide(node);
                CheckBalance(node);
                CheckOrder(node);
                CheckHeads(node, default(TKey));
            }

            private static void CheckNodeSelf(AVLNode node)
            {
                CheckSide(node);
                CheckBalance(node);
                CheckOrder(node);

                if (node.head != null)
                    CheckHeads(node, node.head.key);
                else
                    CheckHeads(node, default(TKey));
            }

            private static void CheckSide(AVLNode node)
            {
                if (node.right != null)
                {
                    System.Diagnostics.Debug.Assert(!node.right.isLeft, "The Node to the Right is not marked as !isLeft", node.ToString());
                    CheckSide(node.right);
                }

                if (node.left != null)
                {
                    System.Diagnostics.Debug.Assert(node.left.isLeft, "The Node to the Left is not marked as isLeft", node.ToString());
                    CheckSide(node.left);
                }
            }

            private static int CheckBalance(AVLNode node)
            {
                int r = 0, l = 0;

                if (node.right != null)
                    r = CheckBalance(node.right) + 1;

                if (node.left != null)
                    l = CheckBalance(node.left) + 1;

                System.Diagnostics.Debug.Assert(node._depthL == l, "Invalid Depth L (is " + node._depthL + " should be " + l + ")", node.ToString());
                System.Diagnostics.Debug.Assert(node._depthR == r, "Invalid Depth R (is " + node._depthR + " should be " + r + ")", node.ToString());

                return Math.Max(r, l);
            }

            private static void RotateLeft(AVLNode node, object[] HashMap, int index)
            {
                // swap head
                AVLNode oldhead = node.head;
                node.head = node.head.head;
                oldhead.head = node;

                // change, who is r,l (also isLeft)
                bool tmpb = node.isLeft;
                node.isLeft = oldhead.isLeft;
                oldhead.isLeft = true;

                if (node.head != null)
                {
                    if (node.isLeft)
                    {
                        node.head.left = node;
                    }
                    else
                    {
                        node.head.right = node;
                    }
                }
                else
                {
                    HashMap[index] = node;
                    node.head = null;
                }

                // update children
                oldhead.right = node.left;
                node.left = oldhead;

                if (oldhead.right != null)
                {
                    oldhead.right.isLeft = false;
                    oldhead.right.head = oldhead;
                }

                oldhead.isLeft = true;

                UpdateDepth(oldhead);
                UpdateDepth(node);
            }

            private static void RotateRight(AVLNode node, object[] HashMap, int index)
            {
                // swap head
                AVLNode oldhead = node.head;
                node.head = node.head.head;
                oldhead.head = node;

                // change, who is r,l (also isLeft)
                bool tmpb = node.isLeft;
                node.isLeft = oldhead.isLeft;
                oldhead.isLeft = false;

                if (node.head != null)
                {
                    if (node.isLeft)
                    {
                        node.head.left = node;
                    }
                    else
                    {
                        node.head.right = node;
                    }
                }
                else
                {
                    HashMap[index] = node;
                    node.head = null;
                }

                // update children
                oldhead.left = node.right;
                node.right = oldhead;

                if (oldhead.left != null)
                {
                    oldhead.left.isLeft = true;
                    oldhead.left.head = oldhead;
                }

                oldhead.isLeft = false;
                
                UpdateDepth(oldhead);
                UpdateDepth(node);
            }

            private static void UpdateDepth(AVLNode node)
            {
                node._depthL = node.left == null ? 0 : Math.Max(node.left._depthL, node.left._depthR) + 1;
                node._depthR = node.right == null ? 0 : Math.Max(node.right._depthL, node.right._depthR) + 1;
            }

            public override string ToString()
            {
                return this.ToString(0);
            }

            internal string ToString(int count)
            {
                return (right != null ? right.ToString(count + 4) : new string(' ', count + 4) + "+ null") + "\n" + new string(' ', count) + (isLeft ? "L" : "R") + " \"" + key.ToString() + "\" : \"" + value.ToString() + "\" (" + balance + " | L" + _depthL + "R" + _depthR + ") \n" + (left != null ? left.ToString(count + 4) : new string(' ', count + 4) + "+ null");
            }

            internal static int GetMaxDepth(AVLNode node)
            {
                if (node.right != null)
                    node._depthR = 1 + GetMaxDepth(node.right);

                if (node.left != null)
                    node._depthL = 1 + GetMaxDepth(node.left);

                return Math.Max(node._depthL, node._depthR);
            }

            /// <summary>
            /// Called after adding a node
            /// </summary>
            internal static void BalanceBubbleUp(AVLNode node, object[] HashMap, int index)
            {
                while (node.head != null)
                {
                    if (node._depthL > node._depthR)
                    {
                        if (node.isLeft)
                            node.head._depthL = node._depthL + 1;
                        else
                            node.head._depthR = node._depthL + 1;
                    }
                    else
                    {
                        if (node.isLeft)
                            node.head._depthL = node._depthR + 1;
                        else
                            node.head._depthR = node._depthR + 1;
                    }

                    if (Math.Abs(node.head.balance) > 1)
                        node.head.Rebalance(HashMap, index);
                    else
                        node = node.head;
                }
            }

            /// <summary>
            /// Called after removing a node - can handle more than 2 or -2 balances on self
            /// </summary>
            internal static void BalanceSelfBubbleUp(AVLNode node, object[] HashMap, int index)
            {
                while (node != null)
                {
                    node._depthL = node.left == null ? 0 : (GetMaxDepth(node.left) + 1);
                    node._depthR = node.right == null ? 0 : (GetMaxDepth(node.right) + 1);

                    if (Math.Abs(node.balance) > 1)
                        node.Rebalance(HashMap, index);
                    else
                        node = node.head;
                }
            }

            internal List<TValue> GetSorted()
            {
                List<TValue> ret = new List<TValue>();

                if (left != null)
                    ret.AddRange(left.GetSorted());

                ret.Add(this.value);

                if (right != null)
                    ret.AddRange(right.GetSorted());

                return ret;
            }

            internal List<TKey> GetSortedKeys()
            {
                List<TKey> ret = new List<TKey>();

                if (left != null)
                    ret.AddRange(left.GetSortedKeys());

                ret.Add(this.key);

                if (right != null)
                    ret.AddRange(right.GetSortedKeys());

                return ret;
            }

            internal static int GetCount(AVLNode node)
            {
                return 1 + (node.left == null ? 0 : GetCount(node.left)) + (node.right == null ? 0 : GetCount(node.right));
            }

            internal static bool FindRemoveKey(AVLNode headNode, object[] HashMap, int hash, TKey key, ref int elementCount)
            {
                int compare = key.CompareTo(headNode.key);

                while (true)
                {
                    if (compare < 0)
                    {
                        headNode = headNode.left;

                        if (headNode != null)
                        {
                            compare = key.CompareTo(headNode.key);
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else if (compare > 0)
                    {
                        headNode = headNode.right;

                        if (headNode != null)
                        {
                            compare = key.CompareTo(headNode.key);
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else
                    {
                        AVLNode.RemoveNode(headNode, HashMap, hash, ref elementCount);

                        return true;
                    }
                }
            }


            internal static bool FindRemoveItem(AVLNode headNode, object[] HashMap, int hash, KeyValuePair<TKey, TValue> item, ref int elementCount)
            {
                int compare = item.Key.CompareTo(headNode.key);

                while (true)
                {
                    if (compare < 0)
                    {
                        headNode = headNode.left;

                        if (headNode != null)
                        {
                            compare = item.Key.CompareTo(headNode.key);
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else if (compare > 0)
                    {
                        headNode = headNode.right;

                        if (headNode != null)
                        {
                            compare = item.Key.CompareTo(headNode.key);
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (headNode.value.Equals(item.Value))
                        {
                            AVLNode.RemoveNode(headNode, HashMap, hash, ref elementCount);

                            return true;
                        }

                        return false;
                    }
                }
            }

            internal static void RemoveNode(AVLNode node, object[] HashMap, int hash, ref int elementCount)
            {
                if (node.right == null && node.left == null) // no children
                {
                    if (node.head == null) // was the top node
                    {
                        HashMap[hash] = null;
                    }
                    else
                    {
                        if (node.isLeft)
                        {
                            node.head.left = null;
                            node.head._depthL = 0;
                        }
                        else
                        {
                            node.head.right = null;
                            node.head._depthR = 0;
                        }

                        AVLNode.BalanceSelfBubbleUp(node.head, HashMap, hash);
                    }
                }
                else if (node.right == null || node.left == null) // one child
                {
                    AVLNode child = node.right != null ? node.right : node.left;

                    if (node.head == null) // was the top node
                    {
                        HashMap[hash] = child;
                        child.head = null;
                    }
                    else
                    {
                        child.isLeft = node.isLeft;

                        if (node.isLeft)
                        {
                            node.head.left = child;
                            child.head = node.head;
                            node.head._depthL -= 1;
                        }
                        else
                        {
                            node.head.right = child;
                            child.head = node.head;
                            node.head._depthR -= 1;
                        }

                        AVLNode.BalanceSelfBubbleUp(node.head, HashMap, hash);
                    }
                }
                else // two children :O
                {
                    AVLNode child = node.right, childhead = node.head;

                    while (child.left != null)
                    {
                        childhead = child;
                        child = child.left;
                    }

                    if (childhead != node.head)
                    {
                        if (child.right != null)
                        {
                            childhead.left = child.right;
                            child.right.head = childhead;
                            child.right.isLeft = true;
                            childhead._depthL--;
                        }
                        else
                        {
                            childhead.left = null;
                            childhead._depthL = 0;
                        }

                        child.right = node.right;
                    }

                    child.left = node.left;
                    child.left.head = child;
                    child.head = node.head;
                    child.isLeft = node.isLeft;

                    if (node.head == null)
                    {
                        HashMap[hash] = child;
                    }
                    else
                    {
                        if (node.isLeft)
                        {
                            node.head.left = child;
                        }
                        else
                        {
                            node.head.right = child;
                        }
                    }

                    if (childhead == node.head)
                    {
                        AVLNode.BalanceSelfBubbleUp(child, HashMap, hash);
                    }
                    else
                    {
                        child.right.head = child;
                        AVLNode.BalanceSelfBubbleUp(childhead, HashMap, hash);
                    }
                }

                elementCount--;
            }

            internal static void AddItem(AVLNode node, KeyValuePair<TKey, TValue> item, object[] HashMap, int hash, ref int elementCount)
            {
                int compare = item.Key.CompareTo(node.key);

                while (true)
                {
                    if (compare < 0)
                    {
                        if (node.left == null)
                        {
                            node.left = new AVLNode(key: item.Key, value: item.Value) { head = node, isLeft = true };
                            node._depthL = 1;
                            AVLNode.BalanceBubbleUp(node, HashMap, hash);
                            elementCount++;
                            break;
                        }
                        else
                        {
                            node = node.left;
                            compare = item.Key.CompareTo(node.key);
                        }
                    }
                    else if (compare > 0)
                    {
                        if (node.right == null)
                        {
                            node.right = new AVLNode(key: item.Key, value: item.Value) { head = node, isLeft = false };
                            node._depthR = 1;
                            AVLNode.BalanceBubbleUp(node, HashMap, hash);
                            elementCount++;
                            break;
                        }
                        else
                        {
                            node = node.right;
                            compare = item.Key.CompareTo(node.key);
                        }
                    }
                    else
                    {
                        node.value = item.Value;
                        break;
                    }
                }
            }

            internal List<KeyValuePair<TKey, TValue>> GetAllData()
            {
                List<KeyValuePair<TKey, TValue>> ret = new List<KeyValuePair<TKey, TValue>>();

                ret.Add(new KeyValuePair<TKey, TValue>(key, value));

                if (right != null)
                    ret.AddRange(right.GetAllData());

                if (left != null)
                    ret.AddRange(left.GetAllData());

                return ret;
            }
        }
    }
}
