﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace LamestWebserver.Collections
{
    public class QueuedAVLTree<TKey, TValue> : IDictionary<TKey, TValue> where TKey : IEquatable<TKey>, IComparable
    {
        internal AVLNode head;
        private int count = 0;
        private int maxCount;
        internal Queue queue;

        public QueuedAVLTree()
        {
            maxCount = 4096;
            queue = new Queue(maxCount);
        }

        public QueuedAVLTree(int maxSize)
        {
            maxCount = maxSize;
            queue = new Queue(maxCount);
        }

        public TValue this[TKey key]
        {
            get
            {
                if (head == null)
                    return default(TValue);
                else
                {
                    AVLNode node = head;
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
                Add(key, value);
            }
        }

        public int Count
        {
            get
            {
                return count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public ICollection<TKey> Keys
        {
            get
            {
                if (head != null) return head.getSortedKeys();
                else return new List<TKey>();
            }
        }

        public ICollection<TValue> Values
        {
            get
            {
                if (head != null) return head.getSorted();
                else return new List<TValue>();
            }
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            Add(item.Key, item.Value);
        }

        public void Add(TKey key, TValue value)
        {
            if (head == null)
            {
                head = new AVLNode(key, value, this);
                count++;
            }
            else
                AVLNode.AddItem(head, key, value, this, ref count);
        }

        public void Clear()
        {
            head = null;
            count = 0;

            queue.Clear();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            if (head == null)
                return false;
            else
            {
                AVLNode node = head;
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
                        return item.Value.Equals(node.value);
                    }
                }
            }
        }

        public bool ContainsKey(TKey key)
        {
            if (head == null)
                return false;

            AVLNode node = head;
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

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            if (head == null)
                return false;

            return AVLNode.FindRemoveItem(head, this, item, ref count);
        }

        public bool Remove(TKey key)
        {
            if (head == null)
                return false;

            return AVLNode.FindRemoveKey(head, this, key, ref count);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            value = this[key];
            return ContainsKey(key);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(count), count);
            info.AddValue(nameof(head), head);
        }

        public void Validate()
        {
            if (head != null)
            {
                AVLNode.checkNodes(head);
                int size = AVLNode.getCount(head);

                System.Diagnostics.Debug.Assert(size == queue.validateCount(), "The queue and tree sizes are not equal.");

                System.Diagnostics.Debug.Assert(size == count, "The elementCount is " + count + " but should be " + size);
                System.Diagnostics.Debug.Assert(size == queue.size, "The queue size is " + queue.size + " but should be " + size);
            }
            else
            {
                System.Diagnostics.Debug.Assert(count == queue.validateCount(), "The queue and tree sizes are not equal.");

                System.Diagnostics.Debug.Assert(count == 0, "The elementCount is " + count + " but should be 0");
                System.Diagnostics.Debug.Assert(queue.size == 0, "The queue size is " + queue.size + " but should be 0");
            }
        }

        public class AVLNode
        {
            internal AVLNode head, left, right;
            internal int balance { get { return -_depthL + _depthR; } }
            internal int _depthL = 0;
            internal int _depthR = 0;
            internal TKey key;
            internal TValue value;
            internal bool isLeft = true;

            internal QueueElement linkedElement;
            
            public AVLNode(TKey key, TValue value, QueuedAVLTree<TKey, TValue> tree)
            {
                this.key = key;
                this.value = value;
                this.linkedElement = tree.queue.Enqueue(this, tree);
            }

            private void rebalance(QueuedAVLTree<TKey, TValue> tree)
            {
                if (Math.Abs(balance) > 2)
                {
                    if (balance < -2)
                    {
                        left.rebalance(tree);
                    }
                    else
                    {
                        right.rebalance(tree);
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
                        rotl(right, tree);
                    }
                    else
                    {
                        rotr(right.left, tree);

                        rotl(right, tree);
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
                        rotr(left, tree);
                    }
                    else
                    {
                        rotl(left.right, tree);

                        rotr(left, tree);
                    }
                }

#if TEST
                checkNodeSelf(this);
#endif
            }

            private static void checkHeads(AVLNode node, TKey key)
            {
                if (node.head != null)
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
                while (node.head != null)
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
                if (node.right != null)
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

                return Math.Max(r, l);
            }

            private static void rotl(AVLNode node, QueuedAVLTree<TKey, TValue> tree)
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
                    tree.head = node;
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

                updateDepth(oldhead);
                updateDepth(node);
            }

            private static void rotr(AVLNode node, QueuedAVLTree<TKey, TValue> tree)
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
                    tree.head = node;
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

                // update balances
                updateDepth(oldhead);
                updateDepth(node);
            }

            private static void updateDepth(AVLNode node)
            {
                node._depthL = node.left == null ? 0 : Math.Max(node.left._depthL, node.left._depthR) + 1;
                node._depthR = node.right == null ? 0 : Math.Max(node.right._depthL, node.right._depthR) + 1;
            }

            public override string ToString()
            {
                return this.ToString(0);
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
            internal static void balanceBubbleUp(AVLNode node, QueuedAVLTree<TKey, TValue> tree)
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
                        node.head.rebalance(tree);
                    else
                        node = node.head;
                }
            }

            /// <summary>
            /// Called after removing a node - can handle more than 2 or -2 balances on self
            /// </summary>
            internal static void balanceSelfBubbleUp(AVLNode node, QueuedAVLTree<TKey, TValue> tree)
            {
                while (node != null)
                {
                    node._depthL = node.left == null ? 0 : (getMaxDepth(node.left) + 1);
                    node._depthR = node.right == null ? 0 : (getMaxDepth(node.right) + 1);

                    if (Math.Abs(node.balance) > 1)
                        node.rebalance(tree);
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

            internal static bool FindRemoveKey(AVLNode node, QueuedAVLTree<TKey, TValue> tree, TKey key, ref int elementCount)
            {
                int compare = key.CompareTo(node.key);

                while (true)
                {
                    if (compare < 0)
                    {
                        node = node.left;

                        if (node != null)
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
                        AVLNode.RemoveNode(node, tree);

                        return true;
                    }
                }
            }


            internal static bool FindRemoveItem(AVLNode node, QueuedAVLTree<TKey, TValue> tree, KeyValuePair<TKey, TValue> item, ref int elementCount)
            {
                int compare = item.Key.CompareTo(node.key);

                while (true)
                {
                    if (compare < 0)
                    {
                        node = node.left;

                        if (node != null)
                        {
                            compare = item.Key.CompareTo(node.key);
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
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (node.value.Equals(item.Value))
                        {
                            AVLNode.RemoveNode(node, tree);

                            return true;
                        }

                        return false;
                    }
                }
            }

            internal static void RemoveNode(AVLNode node, QueuedAVLTree<TKey, TValue> tree, bool callFromQueue = false)
            {
                if(!callFromQueue)
                    tree.queue.Dequeue(node.linkedElement, tree);

                if (node.right == null && node.left == null) // no children
                {
                    if (node.head == null) // was the top node
                    {
                        tree.head = null;
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

                        AVLNode.balanceSelfBubbleUp(node.head, tree);
                    }
                }
                else if (node.right == null || node.left == null) // one child
                {
                    AVLNode child = node.right != null ? node.right : node.left;

                    if (node.head == null) // was the top node
                    {
                        tree.head = child;
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

                        AVLNode.balanceSelfBubbleUp(node.head, tree);
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
                        tree.head = child;
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
                        AVLNode.balanceSelfBubbleUp(child, tree);
                    }
                    else
                    {
                        child.right.head = child;
                        AVLNode.balanceSelfBubbleUp(childhead, tree);
                    }
                }

                tree.count--;
            }

            internal static void AddItem(AVLNode headNode, TKey key, TValue value, QueuedAVLTree<TKey, TValue> tree, ref int elementCount)
            {
                int compare = key.CompareTo(headNode.key);

                if(!tree.ContainsKey(key))
                {
                    tree.queue.checkRoom(tree);

                    if (tree.head == null)
                    {
                        tree.head = new AVLNode(key, value, tree);
                        elementCount++;
                        goto LukeIDeletedYourFather;
                    }
                }
                
                while (true)
                {
                    if (compare < 0)
                    {
                        if (headNode.left == null)
                        {
                            headNode.left = new AVLNode(key: key, value: value, tree: tree) { head = headNode, isLeft = true };
                            headNode._depthL = 1;
                            AVLNode.balanceBubbleUp(headNode, tree);
                            elementCount++;
                            break;
                        }
                        else
                        {
                            headNode = headNode.left;
                            compare = key.CompareTo(headNode.key);
                        }
                    }
                    else if (compare > 0)
                    {
                        if (headNode.right == null)
                        {
                            headNode.right = new AVLNode(key: key, value: value, tree: tree) { head = headNode, isLeft = false };
                            headNode._depthR = 1;
                            AVLNode.balanceBubbleUp(headNode, tree);
                            elementCount++;
                            break;
                        }
                        else
                        {
                            headNode = headNode.right;
                            compare = key.CompareTo(headNode.key);
                        }
                    }
                    else
                    {
                        headNode.value = value;
                        break;
                    }
                }

                LukeIDeletedYourFather:;
            }
        }

        public class QueueElement
        {
            internal QueueElement next, previous;
            private AVLNode linkedNode;

            public QueueElement(QueueElement head, AVLNode linkedNode)
            {
                this.linkedNode = linkedNode;

                next = head.next;
                head.next.previous = this;
                previous = head;
                head.next = this;
            }

            internal QueueElement() { }

            internal static void Remove(QueueElement element, QueuedAVLTree<TKey, TValue> tree, bool callFromQueue = false)
            {
                element.next.previous = element.previous;
                element.previous.next = element.next;

                if (callFromQueue && element.linkedNode != null)
                    AVLNode.RemoveNode(element.linkedNode, tree, true);
            }
        }

        public class Queue
        {
            QueueElement first, last;
            internal int size = 0;
            int maxSize;

            public Queue(int maxSize)
            {
                if (maxSize <= 0)
                    throw new InvalidOperationException(maxSize + " is an invalid Size for a Queue");

                this.maxSize = maxSize;

                first = new QueueElement();
                last = new QueueElement();

                first.next = last;
                last.previous = first;
            }

            public QueueElement Enqueue(AVLNode node, QueuedAVLTree<TKey, TValue> tree)
            {
                if (size > maxSize)
                    Dequeue(last.previous, tree, true);

                QueueElement element = new QueueElement(first, node);
                size++;

                return element;
            }

            public void Dequeue(QueueElement element, QueuedAVLTree<TKey, TValue> tree, bool callFromEnqueue = false)
            {
                QueueElement.Remove(element, tree, callFromEnqueue);
                size--;
            }

            internal int validateCount()
            {
                int count = 0;

                QueueElement qe = first.next;

                while (qe.next != null)
                {
                    qe = qe.next;
                    count++;
                }

                System.Diagnostics.Debug.Assert(count == size, "Queue size is " + size + " but should be " + count);

                return count;
            }

            internal void Clear()
            {
                first = new QueueElement();
                last = new QueueElement();

                first.next = last;
                last.previous = first;

                size = 0;
            }

            internal void checkRoom(QueuedAVLTree<TKey, TValue> tree)
            {
                if (size + 1 > maxSize)
                    Dequeue(last.previous, tree, true);
            }
        }
    }
}