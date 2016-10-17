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

        public void Add(KeyValuePair<TKey, TValue> item)
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
                        node._depthL = 1;
                    }
                    else
                    {
                        node.right = new AVLNode() { head = node, key = item.Key, value = item.Value, isLeft = false };
                        node._depthR = 1;
                    }

                    HashMap[hash] = node;
                    elementCount++;
                }
            }
            else
            {
                // TODO: Add to AVLTree; if exists don't add to elementCount and Dequeue old item if maxSize.HasValue; if !exists add to elementCount
                List<AVLNode> balances = new List<AVLNode>();
                AVLNode node = (AVLNode)HashMap[hash];
                int compare = item.Key.CompareTo(node.key);

                while (true)
                {
                    if (compare < 0)
                    {
                        balances.Add(node);

                        if (node.left == null)
                        {
                            node.left = new AVLNode() { head = node, key = item.Key, value = item.Value, isLeft = true };
                            node._depthL = 1;
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("\n" + node.ToString() + "\n");
                            Console.ForegroundColor = ConsoleColor.Gray;
                            AVLNode.balanceBubbleUp(node);
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
                        balances.Add(node);

                        if (node.right == null)
                        {
                            node.right = new AVLNode() { head = node, key = item.Key, value = item.Value, isLeft = false };
                            node._depthR = 1;
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("\n" + node.ToString() + "\n");
                            Console.ForegroundColor = ConsoleColor.Gray;
                            AVLNode.balanceBubbleUp(node);
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
                                        node.head._depthL--;
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
                                    child.head._depthL--;

                                }
                                else
                                {
                                    child.head.right = null;
                                    child.head._depthL--;
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

                                    // TODO: balances
                                }
                            }
                        }
                        else
                        {
                            // Chris: search for lowest child
                        }

                        for (int i = right.Count - 1; i >= 0; i--)
                        {
                            if(right[i].left == null)
                                right[i]._depthR++;
                        }

                        for (int i = left.Count - 1; i >= 0; i--)
                        {
                            if (left[i].right == null)
                                left[i]._depthR--;
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
            internal int balance { get { return -_depthL + _depthR ; } }
            internal int _depthL = 0;
            internal int _depthR = 0;
            internal TKey key;
            internal TValue value;
            internal bool isLeft = true;
            
            private void rebalance()
            {
                AVLNode displayNode = this;

                while (displayNode.head != null)
                    displayNode = displayNode.head;

                Console.ForegroundColor = ConsoleColor.Gray;

                Console.WriteLine(displayNode.ToString());

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

                    if(right.balance > 0)
                    {
                        /*
                        this._balance -= 2;
                        right._balance -= 1;
                        */
                        rotl(right);

                        Console.Write("(ROTL)");
                    }
                    else
                    {
                        /*
                        this._balance -= 2;
                        right.left._balance = -right._balance + 1;
                        right._balance += 1;
                        */
                        /*this.value =        (TValue)(object)9999;
                        right.value =       (TValue)(object)99999;
                        right.left.value =  (TValue)(object)999999;*/

                        Console.ForegroundColor = ConsoleColor.Yellow;
                        while (displayNode.head != null)
                            displayNode = displayNode.head;

                        rotr(right.left);

                        Console.WriteLine("\n==ROTR===\n" + displayNode + "\n\n\n");

                        Console.ForegroundColor = ConsoleColor.DarkYellow;

                        rotl(right);

                        Console.Write("(ROTL)");
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

                    if (left.balance < 0)
                    {
                        /*
                        this._balance += 2;
                        left._balance += 1;
                        */
                        rotr(left);

                        Console.Write("(ROTL)");
                    }
                    else
                    {
                        /*
                        this.value =        (TValue)(object)9999;
                        left.value =        (TValue)(object)99999;
                        left.right.value =  (TValue)(object)999999;
                        */
                        /*this._balance += 2;
                        left.right._balance = -left._balance - 1;
                        left._balance -= 1;*/

                        Console.ForegroundColor = ConsoleColor.Yellow;
                        while (displayNode.head != null)
                            displayNode = displayNode.head;

                        rotl(left.right);

                        Console.WriteLine("\n==ROTL===\n" + displayNode + "\n\n\n");
                        
                        Console.ForegroundColor = ConsoleColor.DarkYellow;

                        rotr(left);

                        Console.Write("(ROTR)");
                    }
                }

                while (displayNode.head != null)
                    displayNode = displayNode.head;

                Console.WriteLine("\n=====\n" + displayNode + "\n\n\n");

                checkNodes(displayNode);
            }

            private static void checkNodes(AVLNode node)
            {
                while(node.head != null)
                {
                    node = node.head;
                }

                checkNode(node);
                checkBalance(node);
            }

            private static void checkNode(AVLNode node)
            {
                if(node.right != null)
                {
                    System.Diagnostics.Debug.Assert(!node.right.isLeft, "The Node to the Right is not marked as !isLeft", node.ToString());
                    checkNode(node.right);
                }

                if (node.left != null)
                {
                    System.Diagnostics.Debug.Assert(node.left.isLeft, "The Node to the Left is not marked as isLeft", node.ToString());
                    checkNode(node.left);
                }
            }

            private static int checkBalance(AVLNode node)
            {
                int r = 0, l = 0;

                if (node.right != null)
                    r = checkBalance(node.right) + 1;

                if (node.left != null)
                    l = checkBalance(node.left) + 1;

                System.Diagnostics.Debug.Assert(node._depthL == l, "Invalid Depth L (is" + node._depthL + " should be " + l + ")", node.ToString());
                System.Diagnostics.Debug.Assert(node._depthR == r, "Invalid Depth R (is" + node._depthR + " should be " + r + ")", node.ToString());

                return Math.Max(r,l);
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
                oldhead.right = node.left;
                node.left = oldhead;

                // update balances
                oldhead._depthR -= 2;

                node._depthL += 1;
                /*oldhead._balance -= 2;
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
                oldhead.left = node.right;
                node.right = oldhead;

                // update balances
                oldhead._depthL -= 2;

                node._depthR += 1;
                /*oldhead._balance;
                node._balance += 1;*/
            }

            public override string ToString()
            {
                return this.ToString(0);
                //return "(" + (left != null ? left.ToString() + ", " : "" ) + key.ToString() + " [" + balance + "]" + (right != null ? ", " + right.ToString() : "") + ")";
            }

            public string ToString(int count)
            {
                return (right != null ? right.ToString(count + 4) : new string(' ', count + 4) + "+ null") + "\n" + new string(' ', count) + (isLeft ? "L" : "R") + " " + key.ToString() + " (" + balance + " | L: " + _depthL + " - R: " + _depthR + ") \n" + (left != null ? left.ToString(count + 4) : new string(' ', count + 4) + "+ null");
            }

            internal static int getMaxDepth(AVLNode node)
            {
                if (node.right != null)
                    node._depthR = 1 + getMaxDepth(node.right);

                if (node.left != null)
                    node._depthL = 1 + getMaxDepth(node.left);

                return Math.Max(node._depthL, node._depthR);
            }

            internal static void balanceBubbleUp(AVLNode node)
            {
                while (node.head != null)
                {
                    if(node._depthL > node._depthR)
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
                        node.head.rebalance();
                    else
                        node = node.head;
                }

                Console.WriteLine("\n-\n" + node + "\n-\n");
                Console.ReadLine();
            }
        }
    }
}
