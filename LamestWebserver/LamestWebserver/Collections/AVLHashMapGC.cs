using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace LamestWebserver.Collections
{
    public partial class AVLHashMapGC<TKey, TValue> : IDictionary<TKey, TValue>, ISerializable where TKey : IEqualityComparer, IComparable
    {
        private int size = 1024;
        protected int elementCount = 0;

        private object[] HashMap;
        
        private FriendlyQueue lastAdded = null;

        public AVLHashMapGC(int size = 1024, uint? maxSize = null)
        {
            this.size = size;
            if (maxSize.HasValue && maxSize > 0)
            {
                HashMap = new object[size];
                lastAdded = new FriendlyQueue(maxSize, this);
            }
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
                else if (HashMap[hash] is NodeKeyValuePair)
                {
                    if (((NodeKeyValuePair)HashMap[hash]).data.Key.Equals(key))
                        return ((NodeKeyValuePair)HashMap[hash]).data.Value;
                    else
                        return default(TValue);
                }
                else // if HashMap[hash] is an AVL Node search for it
                {
#if DEBUG
                    System.Diagnostics.Debug.Assert(HashMap[hash] is AVLNode);
#endif
                    AVLNode node = (AVLNode)HashMap[hash];
                    int compare = key.CompareTo(node.data.Key);

                    while(true)
                    {
                        if (compare < 0)
                        {
                            node = node.left;

                            if (node != null)
                                compare = key.CompareTo(node.data.Key);
                            else return default(TValue);
                        }
                        else if (compare > 0)
                        {
                            node = node.right;

                            if (node != null)
                                compare = key.CompareTo(node.data.Key);
                            else return default(TValue);
                        }
                        else return node.data.Value;
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
            try
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
            catch(Exception)
            {
                return false;
            }
        }

        public void Add(TKey key, TValue value)
        {
            Add(new KeyValuePair<TKey, TValue>(key, value));
        }

        public bool Remove(TKey key)
        {
            // TODO: If contained: size--int hash = Math.Abs(key.GetHashCode()) % size;

            int hash = Math.Abs(key.GetHashCode()) % size;

            if (HashMap[hash] == null)
            {
                return false;
            }
            else if (HashMap[hash] is NodeKeyValuePair)
            {
                if (((NodeKeyValuePair)HashMap[hash]).data.Key.Equals(key))
                {
                    if (((NodeKeyValuePair)HashMap[hash]).queueHeadNode != null)
                    {
                        if(!lastAdded.first.next.key.Equals(key) && !lastAdded.last.key.Equals(key))
                        {

                        }

                        ((NodeKeyValuePair)HashMap[hash]).queueHeadNode.next = ((NodeKeyValuePair)HashMap[hash]).queueHeadNode.next.next;
                        ((NodeKeyValuePair)HashMap[hash]).queueHeadNode.next.node.head.queueHeadNode = ((NodeKeyValuePair)HashMap[hash]).queueHeadNode;
                        HashMap[hash] = null;
                        lastAdded.size--;
                    }
                    else
                    {
                        lastAdded.Remove(key);
                    }

                    size--;
                    return true;
                }
                else
                    return false;
            }
            else // if HashMap[hash] is an AVL Node search for it
            {
#if DEBUG
                System.Diagnostics.Debug.Assert(HashMap[hash] is AVLNode);
#endif
                AVLNode node = (AVLNode)HashMap[hash];
                int compare = key.CompareTo(node.data.Key); 

                // TODO: Implement

                return false;
            }
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            try
            {
                value = this[key];
                return true;
            }
            catch(Exception)
            {
                value = default(TValue);
                return false;
            }
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            int hash = Math.Abs(item.Key.GetHashCode()) % size;

            if (HashMap[hash] == null)
            {
                HashMap[hash] = item;
                elementCount++;
            }
            else if(HashMap[hash] is NodeKeyValuePair)
            {
                if(((NodeKeyValuePair)HashMap[hash]).data.Key.Equals(item.Key))
                {
                    ((NodeKeyValuePair)HashMap[hash]).queueHeadNode.next = ((NodeKeyValuePair)HashMap[hash]).queueHeadNode.next.next;
                    HashMap[hash] = item;
                    lastAdded.Enqueue(item);
                }
                // TODO: Create AVL Tree and add; if exists don't add to elementCount and Dequeue old item if maxSize.HasValue; if !exists add to elementCount
            }
            else
            {
                // TODO: Add to AVLTree; if exists don't add to elementCount and Dequeue old item if maxSize.HasValue; if !exists add to elementCount
            }

            if (lastAdded != null)
            {
                lastAdded.Enqueue(item);
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
            catch(Exception)
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
            return Remove(item.Key);
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

        public AVLHashMapGC(SerializationInfo info, StreamingContext context)
        {
            size = info.GetInt32(nameof(size));
            elementCount = info.GetInt32(nameof(elementCount));
            HashMap = (object[])info.GetValue(nameof(HashMap), typeof(object[]));
        }

        internal class NodeKeyValuePair
        {
            public KeyValuePair<TKey, TValue> data;
            public FriendlyQueue.QueueElement queueHeadNode;
        }

        internal class AVLNode : ISerializable
        {
            internal AVLNode left, right, head;
            public uint depth;
            public int balance;
            public KeyValuePair<TKey, TValue> data;
            public bool isRight = false;
            public FriendlyQueue.QueueElement queueHeadNode;

            public AVLNode(KeyValuePair<TKey, TValue> data)
            {
                this.data = data;
            }

            public AVLNode(SerializationInfo info, StreamingContext context)
            {
                left = (AVLNode)info.GetValue(nameof(left), typeof(AVLNode));
                right = (AVLNode)info.GetValue(nameof(right), typeof(AVLNode));

                if (left != null)
                    left.head = this;

                if (right != null)
                    right.head = this;

                data = (KeyValuePair<TKey, TValue>)info.GetValue(nameof(data), typeof(KeyValuePair<TKey, TValue>));
                depth = info.GetUInt32(nameof(depth));
                balance = info.GetInt32(nameof(balance));
                isRight = info.GetBoolean(nameof(isRight));
            }

            public void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                info.AddValue(nameof(depth), depth);
                info.AddValue(nameof(balance), balance);
                info.AddValue(nameof(left), left);
                info.AddValue(nameof(right), right);
                info.AddValue(nameof(data), data);
                info.AddValue(nameof(isRight), right);

                // Chris: not "head" because it would break the data structure
            }

            internal void Remove_CALL_FROM_QUEUE()
            {

            }
        }

        internal class FriendlyQueue : ISerializable
        {
            internal QueueElement first, last;
            internal uint size = 0;
            internal uint? maxSize = null;
            private AVLHashMapGC<TKey, TValue> avlhashmap;

            public FriendlyQueue() { }

            public FriendlyQueue(uint? maxSize = null, AVLHashMapGC<TKey, TValue> master = null)
            {
                this.maxSize = maxSize;
                this.avlhashmap = master;
            }

            public void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                info.AddValue(nameof(first), first);
                info.AddValue(nameof(last), last);
                info.AddValue(nameof(size), size);
                info.AddValue(nameof(maxSize), maxSize);
            }

            public void Remove(TKey key)
            {
                if (first.key.Equals(key))
                {
                    first = first.next;
                }
                else if (last.key.Equals(key))
                {
                    QueueElement.Remove(key, first);
                    size--; // Chris: IT HAS ALREADY BEEN FOUND THROUGH SPECIFICATION IN THE IF STATEMENT
                    last = QueueElement.FindLast(first);
                }
                else
                    if (QueueElement.Remove(key, first))
                    size--;
            }

            public QueueElement Find(TKey key)
            {
                return QueueElement.Find(key, first);
            }

            public void Enqueue(KeyValuePair<TKey, TValue> item)
            {
                if (first == null)
                {
                    var tmp = new QueueElement(item.Key);
                    first = tmp;
                    last = tmp;
                    size++;

#if DEBUG
                    System.Diagnostics.Debug.Assert(size == 1);
#endif
                }
                else
                {
                    new QueueElement(item.Key, last);
                    size++;
                }

                if (maxSize.HasValue)
                {
                    if (size > maxSize.Value)
                    {
                        first.Remove_CALL_FROM_QUEUE(avlhashmap);
                        size--;
                    }
                }
            }

            public QueueElement Dequeue()
            {
                var ret = first;
                first = first.next;
                return ret;
            }

            public QueueElement Peek()
            {
                return first;
            }

            internal class QueueElement : ISerializable
            {
                internal QueueElement next = null;
                internal TKey key;
                internal AVLNode node = null;

                public QueueElement()
                {

                }

                public QueueElement(TKey item)
                {
                    this.key = item;
                }

                public QueueElement(TKey item, QueueElement father)
                {
                    this.key = item;

                    if (father.next != null)
                    {
                        var old = father.next;
                        this.next = old;
                        father.next = this;
                    }
                    else
                        father.next = this;
                }

                public static QueueElement Find(TKey key, QueueElement first)
                {
                    while (!first.key.Equals(key))
                    {
                        if (first.next == null)
                            return null;

                        first = first.next;
                    }

                    return first;
                }

                internal void Remove_CALL_FROM_QUEUE(AVLHashMapGC<TKey, TValue> avlhashmap)
                {
                    if (node != null && (node.head != null || node.right != null || node.left != null))
                    {
                        node.Remove_CALL_FROM_QUEUE();
                    }
                    else
                    {
                        int hash = Math.Abs(key.GetHashCode()) % avlhashmap.size;
#if DEBUG
                        System.Diagnostics.Debug.Assert(!(avlhashmap.HashMap[hash] is AVLNode) && avlhashmap.HashMap[hash] is NodeKeyValuePair);
#endif
                        avlhashmap.HashMap[hash] = null;
                        avlhashmap.size--;
                    }
                }

                public static bool Remove(TKey key, QueueElement first)
                {
                    while (first.next != null && !first.next.key.Equals(key))
                    {
                        first = first.next;
                    }

                    if (first.next != null && first.next.key.Equals(key))
                    {
                        first.next = first.next.next;
                        return true;
                    }

                    return false;
                }

                public void GetObjectData(SerializationInfo info, StreamingContext context)
                {
                    info.AddValue(nameof(next), next);
                    info.AddValue(nameof(key), key);
                }

                public static QueueElement FindLast(QueueElement first)
                {
                    while (first.next != null)
                    {
                        first = first.next;
                    }

                    return first;
                }
            }
        }
    }
}
