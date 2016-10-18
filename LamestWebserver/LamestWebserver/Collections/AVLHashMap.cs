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

        internal object[] HashMap { get; private set; }

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

        public void Validate()
        {
            int size = 0;

            for (int i = 0; i < HashMap.Length; i++)
            {
                if (HashMap[i] is AVLNode)
                {
                    AVLNode.checkNodes((AVLNode)HashMap[i]);
                    size += AVLNode.getCount((AVLNode)HashMap[i]);
                }
                else if (HashMap[i] != null)
                    size++;
            }

            System.Diagnostics.Debug.Assert(size == elementCount, "The elementCount is " + elementCount + " but should be " + size);
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
                    //Console.WriteLine("\n------------\n------------\n------------\n" + "Searching for '" + key + "'");
                    
                    AVLNode node = (AVLNode)HashMap[hash];
                    int compare = key.CompareTo(node.key);

                    while (true)
                    {
                        //Console.WriteLine(node + "\n------------\n");

                        if (compare < 0)
                        {
                            //Console.WriteLine(key + " < \n" + node.key + " (" + compare + ")");
                            node = node.left;

                            if (node != null)
                                compare = key.CompareTo(node.key);
                            else
                                return default(TValue);
                        }
                        else if (compare > 0)
                        {
                            //Console.WriteLine(key + " > \n" + node.key + " (" + compare + ")");
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
            if (this[key] == null || this[key].Equals(default(TValue)))
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

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            // TODO: Search for exact finds!
            return Remove(item.Key);
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
                    elementCount++;
                }
            }
            else
            {
                // TODO: Add to AVLTree; if exists don't add to elementCount and Dequeue old item if maxSize.HasValue; if !exists add to elementCount
                AVLNode node = (AVLNode)HashMap[hash];
                int compare = item.Key.CompareTo(node.key);

                while (true)
                {
                    if (compare < 0)
                    {
                        if (node.left == null)
                        {
                            node.left = new AVLNode(key: item.Key, value: item.Value) { head = node, isLeft = true };
                            node._depthL = 1;
                            /*Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("\n" + node.ToString() + "\n");
                            Console.ForegroundColor = ConsoleColor.Gray;*/
                            AVLNode.balanceBubbleUp(node, HashMap, hash);
                            /*Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.WriteLine("\n" + node.ToString() + "\n");
                            Console.ForegroundColor = ConsoleColor.Gray;*/
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
                            /*Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("\n" + node.ToString() + "\n");
                            Console.ForegroundColor = ConsoleColor.Gray;*/
                            AVLNode.balanceBubbleUp(node, HashMap, hash);
                            /*Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.WriteLine("\n" + node.ToString() + "\n");
                            Console.ForegroundColor = ConsoleColor.Gray;*/
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

#if TEST
                AVLNode.checkNodes(node);
#endif

                /*
                Action<AVLNode> checkNotZero = null;
                checkNotZero = (AVLNode n) => { if (n != null) { System.Diagnostics.Debug.Assert(!n.value.Equals((TValue)((object)0)), "value is 0 for" + item.Value); checkNotZero(n.right); checkNotZero(n.left); } };

                checkNotZero(node);*/
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

        public bool Remove(TKey key)
        {
            int hash = Math.Abs(key.GetHashCode()) % size;
            
            if (HashMap[hash] is KeyValuePair<TKey, TValue>)
            {
                if (((KeyValuePair<TKey, TValue>)HashMap[hash]).Key.Equals(key))
                {
                    HashMap[hash] = null;
                    elementCount--;
                    return true;
                }
                else return false;
            }
            else if (HashMap[hash] is AVLNode)
            {
                AVLNode node = (AVLNode)HashMap[hash];
                int compare = key.CompareTo(node.key);

                while(true)
                {
                    if(compare < 0)
                    {
                        node = node.left;

                        if(node != null)
                        {
                            compare = key.CompareTo(node.key);
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
                            compare = key.CompareTo(node.key);
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if(node.right == null && node.left == null) // no children
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

                                AVLNode.balanceSelfBubbleUp(node.head, HashMap, hash);
                            }
                        }
                        else if(node.right == null || node.left == null) // one child
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

                                AVLNode.balanceSelfBubbleUp(node.head, HashMap, hash);
                            }
                        }
                        else // two children :O
                        {
                            AVLNode child = node.right, childhead = node.head;

                            while(child.left != null)
                            {
                                childhead = child;
                                child = child.left;
                            }

                            if (childhead == node.head)
                            {
                                //if (childhead == node.head) child.right keeps child.right
                                //childhead.right = child; // childhead is node.head! // @FIXME: this is redundant and can be dropped.

                                //child.left = node.left;// @FIXME: this is redundant and can be dropped.
                                //child.left.head = child;// @FIXME: this is redundant and can be dropped.
                            }
                            else
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

                            if(node.head == null)
                            {
                                HashMap[hash] = child;
                            }
                            else
                            {
                                if(node.isLeft)
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
                                // child.right = null;
                                // child._depthR = 0;
                                AVLNode.balanceSelfBubbleUp(child, HashMap, hash);
                            }
                            else
                            {
                                /*
                                // we already do that in the line below, right? (child.right.head = child;)
                                if(childhead.head == node)
                                {
                                    childhead.head = child;
                                }*/

                                child.right.head = child;
                                AVLNode.balanceSelfBubbleUp(childhead, HashMap, hash);
                            }

                            // AVLNode.balanceSelfBubbleUp(childhead, HashMap, hash);
                        }
#region oldcode

                        /*if(node.isLeft)
                        {
                            AVLNode child = node.right;

                            if (node.right == null)
                            {
                                if (node.left != null)
                                {
                                    if (node.head == null) // is root node
                                    {
                                        HashMap[hash] = node.left;
                                        node.left.head = null;
                                    }
                                    else
                                    {
                                        node.head._depthL--;
                                        node.head.left = node.left;
                                        node.left.head = node.head;
                                        // node.left.isLeft = true; // Redundant
                                        AVLNode.balanceBubbleUp(node.head, HashMap, hash);
                                    }
                                }
                                else // else: node has no children
                                {
                                    if (node.head == null)
                                    {
                                        HashMap[hash] = null;
                                    }
                                    else
                                    {
                                        node.head.left = null;
                                        node.head._depthL = 0;
                                        AVLNode.balanceBubbleUp(node.head, HashMap, hash);
                                    }
                                }
                            }
                            else if (node.left == null)
                            {
                                // We know that node.right can't be null
                                if (node.head == null) // is root node
                                {
                                    HashMap[hash] = node.right;
                                    node.right.head = null;
                                }
                                else
                                {
                                    node.head._depthL--;
                                    node.head.left = node.right;
                                    node.right.head = node.head;
                                    node.right.isLeft = false;
                                    AVLNode.balanceBubbleUp(node.head, HashMap, hash);
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
                        }*/

                        #endregion
                        elementCount--;
                        return true;
                    }
                }
            }

            return false; // Redundant
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

        public class AVLNode
        {
            internal AVLNode head, left, right;
            internal int balance { get { return -_depthL + _depthR ; } }
            internal int _depthL = 0;
            internal int _depthR = 0;
            internal TKey key;
            internal TValue value;
            internal bool isLeft = true;

            public AVLNode(TKey key, TValue value)
            {
                this.key = key;
                this.value = value;
            }
            
            private void rebalance(object[] HashMap, int index)
            {
                /*AVLNode displayNode = this;

                while (displayNode.head != null)
                    displayNode = displayNode.head;

                Console.ForegroundColor = ConsoleColor.Gray;

                Console.WriteLine(displayNode.ToString());*/
                if (Math.Abs(balance) > 2)
                {
                    if(balance < -2)
                    {
                        left.rebalance(HashMap, index);
                    }
                    else
                    {
                        right.rebalance(HashMap, index);
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
                        /*
                        this._balance -= 2;
                        right._balance -= 1;
                        */
                        //Console.WriteLine("rotl");
                        rotl(right, HashMap, index);

                        /*Console.Write("(ROTL)");*/
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
                        //Console.WriteLine("rotr");

                        rotr(right.left, HashMap, index);

                        /*Console.ForegroundColor = ConsoleColor.Yellow;

                        while (displayNode.head != null)
                            displayNode = displayNode.head;

                        Console.WriteLine("\n==ROTR===\n" + displayNode + "\n\n\n");

                        Console.ForegroundColor = ConsoleColor.DarkYellow;*/
                        //Console.WriteLine("rotl");

                        rotl(right, HashMap, index);

                        /*Console.Write("(ROTL)");*/
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
                        /*
                        this._balance += 2;
                        left._balance += 1;
                        */
                        //Console.WriteLine("rotr");
                        rotr(left, HashMap, index);

                        /*Console.Write("(ROTL)");*/
                    }
                    else
                    {
                        //Console.WriteLine("rotl");
                        /*
                        this.value =        (TValue)(object)9999;
                        left.value =        (TValue)(object)99999;
                        left.right.value =  (TValue)(object)999999;
                        */
                        /*this._balance += 2;
                        left.right._balance = -left._balance - 1;
                        left._balance -= 1;*/

                        rotl(left.right, HashMap, index);
                        //Console.WriteLine("rotr");

                        /*Console.ForegroundColor = ConsoleColor.Yellow;

                        while (displayNode.head != null)
                            displayNode = displayNode.head;

                        Console.WriteLine("\n==ROTL===\n" + displayNode + "\n\n\n");

                        Console.ForegroundColor = ConsoleColor.DarkYellow;*/

                        rotr(left, HashMap, index);

                        /*Console.Write("(ROTR)");*/
                    }
                }

                /*while (displayNode.head != null)
                    displayNode = displayNode.head;

                Console.WriteLine("\n=====\n" + displayNode + "\n\n\n");

                checkNodes(displayNode);*/

                /*AVLNode displaynode = this;

                while (displaynode.head != null)
                    displaynode = displaynode.head;

                checkOrder(displaynode);
                checkHeads(displaynode, default(TKey));
                Console.WriteLine("Keys OK!\n\n");*/
                //Console.WriteLine("\n");

#if TEST
                checkNodeSelf(this);
#endif
            }

            private static void checkHeads(AVLNode node, TKey key)
            {
                if(node.head != null)
                {
                    System.Diagnostics.Debug.Assert(node.head.key.Equals(key), "Mismatching Head Keys: (is " + node.head.key + " should be " + key + ")", node.head.ToString());
                }

                if (node.right != null)
                    checkHeads(node.right, node.key);

                if (node.left != null)
                    checkHeads(node.left, node.key);
            }

            private static void checkOrder(AVLNode node)
            {
                var list = node.getSortedKeys();

                for (int i = 0; i < list.Count - 1; i++)
                {
                    System.Diagnostics.Debug.Assert(list[i].CompareTo(list[i + 1]) < 0, "Unordered Keys");
                }
            }

            internal static void checkNodes(AVLNode node)
            {
                while(node.head != null)
                {
                    node = node.head;
                }

                checkSide(node);
                checkBalance(node);
                checkOrder(node);
                checkHeads(node, default(TKey));
            }

            private static void checkNodeSelf(AVLNode node)
            {
                checkSide(node);
                checkBalance(node);
                checkOrder(node);

                if (node.head != null)
                    checkHeads(node, node.head.key);
                else
                    checkHeads(node, default(TKey));
            }

            private static void checkSide(AVLNode node)
            {
                if(node.right != null)
                {
                    System.Diagnostics.Debug.Assert(!node.right.isLeft, "The Node to the Right is not marked as !isLeft", node.ToString());
                    checkSide(node.right);
                }

                if (node.left != null)
                {
                    System.Diagnostics.Debug.Assert(node.left.isLeft, "The Node to the Left is not marked as isLeft", node.ToString());
                    checkSide(node.left);
                }
            }

            private static int checkBalance(AVLNode node)
            {
                int r = 0, l = 0;

                if (node.right != null)
                    r = checkBalance(node.right) + 1;

                if (node.left != null)
                    l = checkBalance(node.left) + 1;

                System.Diagnostics.Debug.Assert(node._depthL == l, "Invalid Depth L (is " + node._depthL + " should be " + l + ")", node.ToString());
                System.Diagnostics.Debug.Assert(node._depthR == r, "Invalid Depth R (is " + node._depthR + " should be " + r + ")", node.ToString());

                return Math.Max(r,l);
            }

            private static void rotl(AVLNode node, object[] HashMap, int index)
            {
                /*Console.ForegroundColor = ConsoleColor.DarkGreen;
                if(node.head != null)
                    Console.WriteLine("\n\n" + node.head.ToString() + "\n--");
                else
                    Console.WriteLine("\n\n" + node.ToString() + "\n--");
                Console.ForegroundColor = ConsoleColor.Gray;*/

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

                // update balances
                /*if (node._depthR == 0 && node._depthL == 0 || oldhead.balance > 0)
                    oldhead._depthR -= 1;
                else
                    oldhead._depthR -= 2;*/
                updateDepth(oldhead);
                updateDepth(node);

                //node._depthL += 1;
                /*oldhead._balance -= 2;
                node._balance -= 1;*/

                /*Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.WriteLine("\nROT L:\n" + node.ToString() + "\n");
                Console.ForegroundColor = ConsoleColor.Gray;*/

                //balanceBubbleUp(node);
                //checkNodes(node);
            }

            private static void rotr(AVLNode node, object[] HashMap, int index)
            {
                /*Console.ForegroundColor = ConsoleColor.DarkCyan;
                if (node.head != null)
                    Console.WriteLine("\n\n" + node.head.ToString() + "\n--");
                else
                    Console.WriteLine("\n\n" + node.ToString() + "\n--");
                Console.ForegroundColor = ConsoleColor.Gray;*/

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

                if(oldhead.left != null)
                { 
                    oldhead.left.isLeft = true;
                    oldhead.left.head = oldhead;
                }

                oldhead.isLeft = false;

                // update balances
                /*if (node._depthR == 0 && node._depthL == 0 || oldhead.balance < 0)
                    oldhead._depthL -= 1;
                else
                    oldhead._depthL -= 2;*/
                updateDepth(oldhead);
                updateDepth(node);

                //node._depthR += 1;
                /*oldhead._balance;
                node._balance += 1;*/

                /*Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.WriteLine("\nROT R:\n" + node.ToString() + "\n");
                Console.ForegroundColor = ConsoleColor.Gray;*/

                //balanceBubbleUp(node);
                //checkNodes(node);
            }

            private static void updateDepth(AVLNode node)
            {
                node._depthL = node.left == null ? 0 : Math.Max(node.left._depthL, node.left._depthR) + 1;
                node._depthR = node.right == null ? 0 : Math.Max(node.right._depthL, node.right._depthR) + 1;
            }

            public override string ToString()
            {
                return this.ToString(0);
                //return "(" + (left != null ? left.ToString() + ", " : "" ) + key.ToString() + " [" + balance + "]" + (right != null ? ", " + right.ToString() : "") + ")";
            }

            public string ToString(int count)
            {
                return (right != null ? right.ToString(count + 4) : new string(' ', count + 4) + "+ null") + "\n" + new string(' ', count) + (isLeft ? "L" : "R") + " \"" + key.ToString() + "\" : \"" + value.ToString() + "\" (" + balance + " | L" + _depthL + "R" + _depthR + ") \n" + (left != null ? left.ToString(count + 4) : new string(' ', count + 4) + "+ null");
            }

            internal static int getMaxDepth(AVLNode node)
            {
                if (node.right != null)
                    node._depthR = 1 + getMaxDepth(node.right);

                if (node.left != null)
                    node._depthL = 1 + getMaxDepth(node.left);

                return Math.Max(node._depthL, node._depthR);
            }

            /// <summary>
            /// Called after adding a node
            /// </summary>
            internal static void balanceBubbleUp(AVLNode node, object[] HashMap, int index)
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
                        node.head.rebalance(HashMap, index);
                    else
                        node = node.head;
                }
            }

            /// <summary>
            /// Called after removing a node - can handle more than 2 or -2 balances on self
            /// </summary>
            internal static void balanceSelfBubbleUp(AVLNode node, object[] HashMap, int index)
            {
                while (node != null)
                {
                    node._depthL = node.left == null ? 0 : (getMaxDepth(node.left) + 1);
                    node._depthR = node.right == null ? 0 : (getMaxDepth(node.right) + 1);

                    if (Math.Abs(node.balance) > 1)
                        node.rebalance(HashMap, index);
                    else
                        node = node.head;
                }
            }

            public List<TValue> getSorted()
            {
                List<TValue> ret = new List<TValue>();

                if (left != null)
                    ret.AddRange(left.getSorted());

                ret.Add(this.value);

                if (right != null)
                    ret.AddRange(right.getSorted());

                return ret;
            }

            public List<TKey> getSortedKeys()
            {
                List<TKey> ret = new List<TKey>();

                if (left != null)
                    ret.AddRange(left.getSortedKeys());

                ret.Add(this.key);

                if (right != null)
                    ret.AddRange(right.getSortedKeys());

                return ret;
            }

            public static int getCount(AVLNode node)
            {
                return 1 + (node.left == null ? 0 : getCount(node.left)) + (node.right == null ? 0 : getCount(node.right));
            }
        }
    }
}
