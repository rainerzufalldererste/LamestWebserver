using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace LamestWebserver.Collections
{
    public class AVLHashMap<TKey, TValue> : IDictionary<TKey, TValue>, ISerializable where TKey : IEquatable<TKey>, IComparable
    {
        private int size = 1024;
        protected int elementCount = 0;

        public object[] HashMap { get; private set; }

        public AVLHashMap(int size = 1024)
        {
            this.size = size;
            HashMap = new object[size];
        }

        public ICollection<TKey> Keys
        {
            get
            {
                throw new InvalidOperationException("This Dictinary does not support getting all Keys");
            }
        }

        public ICollection<TValue> Values
        {
            get
            {
                throw new InvalidOperationException("This Dictinary does not support getting all Values");
            }
        }

        public int Count
        {
            get
            {
                return elementCount;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public TValue this[TKey key]
        {
            get
            {
                int hash = Math.Abs(key.GetHashCode()) % size;

                if (HashMap[hash] == null)
                {
                    return default(TValue); // Chris: Should we throw an exception instead?
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

        public bool ContainsKey(TKey key)
        {
            if (this[key].Equals(default(TValue)))
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public void Add(TKey key, TValue value)
        {
            Add(new KeyValuePair<TKey, TValue>(key, value));
        }

        public bool Remove(TKey key)
        {
            throw new NotImplementedException();
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            value = this[key];
            return value.Equals(default(TValue));
        }

        public unsafe void Add(KeyValuePair<TKey, TValue> item)
        {
            int hash = Math.Abs(item.Key.GetHashCode()) % size;

            if (HashMap[hash] == null)
            {
                HashMap[hash] = item;
                elementCount++;
            }
            else if (HashMap[hash] is KeyValuePair<TKey, TValue>)
            {
                if (((KeyValuePair<TKey, TValue>)HashMap[hash]).Key.Equals(item.Key))
                {
                    HashMap[hash] = item;
                }
                else
                {
                    AVLNode node = new AVLNode() { head = null, key = ((KeyValuePair<TKey, TValue>)HashMap[hash]).Key, value = ((KeyValuePair<TKey, TValue>)HashMap[hash]).Value };

                    if (item.Key.CompareTo(((KeyValuePair<TKey, TValue>)HashMap[hash]).Key) < 0)
                    {
                        node.left = new AVLNode() { head = node, key = item.Key, value = item.Value, isLeft = true };
                        node.balance--;
                    }
                    else
                    {
                        node.right = new AVLNode() { head = node, key = item.Key, value = item.Value, isLeft = false };
                        node.balance++;
                    }

                    HashMap[hash] = node;
                    elementCount++;
                }
            }
            else
            {
                // TODO: Add to AVLTree; if exists don't add to elementCount and Dequeue old item if maxSize.HasValue; if !exists add to elementCount
                List<AVLNode> balances_plus = new List<AVLNode>();
                List<AVLNode> balances_minus = new List<AVLNode>();
                AVLNode node = (AVLNode)HashMap[hash];
                int compare = item.Key.CompareTo(node.key);

                while (true)
                {
                    if (compare < 0)
                    {
                        balances_minus.Add(node);

                        if (node.left == null)
                        {
                            node.left = new AVLNode() { head = node, key = item.Key, value = item.Value, balance = 0, isLeft = true };
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
                        balances_plus.Add(node);

                        if (node.right == null)
                        {
                            node.right = new AVLNode() { head = node, key = item.Key, value = item.Value, balance = 0, isLeft = false };
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
                        goto DONT_CHANGE_BALANCE;
                    }
                }

                for (int i = balances_plus.Count - 1; i >= 0; i--)
                {
                    balances_plus[i].balance++;
                }

                for (int i = balances_minus.Count - 1; i >= 0; i--)
                {
                    balances_minus[i].balance++;
                }

                DONT_CHANGE_BALANCE:;
            }
        }

        public void Clear()
        {
            elementCount = 0;
            HashMap = new object[size];
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            try
            {
                TValue element = this[item.Key];

                if (element.Equals(item.Value))
                    return true;
                else
                    return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            int hash = Math.Abs(item.Key.GetHashCode()) % size;
            
            if (HashMap[hash] is KeyValuePair<TKey, TValue>)
            {
                if (((KeyValuePair<TKey, TValue>)HashMap[hash]).Key.Equals(item.Key))
                {
                    HashMap[hash] = null;
                    elementCount--;
                    return true;
                }
                else return false;
            }
            else if (HashMap[hash] is AVLNode)
            {
                List<AVLNode> right = new List<AVLNode>();
                List<AVLNode> left = new List<AVLNode>();

                AVLNode node = (AVLNode)HashMap[hash];
                int compare = item.Key.CompareTo(node);

                while(true)
                {
                    if(compare < 0)
                    {
                        node = node.left;

                        if(node != null)
                        {
                            compare = item.Key.CompareTo(node.key);
                            left.Add(node.head);
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else if (compare > 0)
                    {
                        node = node.right;

                        if (node != null)
                        {
                            compare = item.Key.CompareTo(node.key);
                            right.Add(node.head);
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if(node.isLeft)
                        {
                            AVLNode child = node.right;

                            if (child == null)
                            {
                                if (node.left != null)
                                {
                                    if (node.head == null) // is root node
                                    {
                                        // TODO: implement
                                    }
                                    else
                                    {
                                        node.head.balance--;
                                        node.head.left = node.right;
                                    }
                                }
                                else // else: node has no children
                                {
                                    if (node.head == null)
                                    {
                                        HashMap[hash] = null;
                                    }
                                }
                            }
                            else
                            {
                                // Chris: search for highest child

                                while (child.right != null)
                                {
                                    child = child.right;
                                }

                                AVLNode childLeft = child.left;

                                if (childLeft != null)
                                {
                                    child.head.right = childLeft;
                                    child.head.balance--;

                                }
                                else
                                {
                                    child.head.right = null;
                                    child.head.balance--;
                                }

                                if (node.head == null) // is root node
                                {
                                    // TODO: implement
                                }
                                else
                                {
                                    child.head = node.head;
                                    node.head.left = child;

                                    child.left = node.left;
                                    child.right = node.right;

                                    child.balance = node.balance - 1;
                                    child.head.balance--;
                                }
                            }
                        }
                        else
                        {
                            // Chris: search for lowest child
                        }

                        for (int i = right.Count - 1; i >= 0; i--)
                        {
                            right[i].balance++;
                        }

                        for (int i = left.Count - 1; i >= 0; i--)
                        {
                            left[i].balance--;
                        }

                        elementCount--;
                        return true;
                    }
                }
            }

            throw new NotImplementedException();
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(size), size);
            info.AddValue(nameof(elementCount), elementCount);
            info.AddValue(nameof(HashMap), HashMap);
        }

        public AVLHashMap(SerializationInfo info, StreamingContext context)
        {
            size = info.GetInt32(nameof(size));
            elementCount = info.GetInt32(nameof(elementCount));
            HashMap = (object[])info.GetValue(nameof(HashMap), typeof(object[]));
        }

        internal class AVLNode
        {
            internal AVLNode head, left, right;
            internal int balance { get { return _balance; } set { _balance = value; if ( Math.Abs(_balance) > 1 ) { rebalance(); } } }
            private int _balance = 0;
            internal TKey key;
            internal TValue value;
            internal bool isLeft = true;
            
            private void rebalance()
            {
                if(_balance > 1)
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

                    if(right.balance > 0)
                    {
                        this._balance -= 2;
                        right._balance -= 1;

                        rotl(right);
                    }
                    else
                    {
                        this._balance -= 2;
                        right.left._balance = -right._balance + 1;
                        right._balance -= 1;

                        rotr(right.left);
                        rotl(right);
                    }
                }
                else if(balance < -1)
                {
                    //              5           |            5
                    //         4          9     |       4          8
                    //                 8        |                7   9
                    //                7         |

                    //            5             |              5            |           3
                    //     1            6       |       3           6       |      1           5
                    //  0     3                 |    1     4                |   0           4      6
                    //          4               |  0                        |

                    if (right.balance < 0)
                    {
                        this._balance += 2;
                        left._balance += 1;

                        rotr(left);
                    }
                    else
                    {
                        this._balance += 2;
                        left.right._balance = -left._balance - 1;
                        left._balance += 1;

                        rotl(left.right);
                        rotr(left);
                    }
                }
            }

            private static void rotl(AVLNode node)
            {
                string node_ = node.ToString();
                string head_ = node.head.ToString();

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

                // update children
                oldhead.right = node.left;
                node.left = oldhead;
                /*
                // update balances
                oldhead._balance -= 2;
                node._balance -= 1;*/
            }

            private static void rotr(AVLNode node)
            {
                string node_ = node.ToString();
                string head_ = node.head.ToString();

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
                // update children
                oldhead.left = node.right;
                node.right = oldhead;
                /*
                // update balances
                // oldhead._balance;
                node._balance += 1;*/
            }

            public override string ToString()
            {
                return "(" + (left != null ? left.ToString() + ", " : "" ) + key.ToString() + " (" + balance + ")" + (right != null ? ", " + right.ToString() : "") + ")";
            }
        }
    }
}
