using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace LamestWebserver.Collections
{
    public class FriendlyQueue<TKey, TValue> : ISerializable where TKey : IEqualityComparer
    {
        private QueueElement first, last;
        public uint size { get; private set; }
        public uint? maxSize = null;
        public event Action<KeyValuePair<TKey, TValue>> callbackOnDelete;

        public FriendlyQueue() { }

        public FriendlyQueue(uint? maxSize = null, Action<KeyValuePair<TKey, TValue>> callback = null)
        {
            this.maxSize = maxSize;

            if (callback != null)
                callbackOnDelete += callback;
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
            if (first.data.Key.Equals(key))
            {
                first = first.next;
            }
            else if (last.data.Key.Equals(key))
            {
                QueueElement.Remove(key, first);
                size--; // Chris: IT HAS ALREADY BEEN FOUND THROUGH SPECIFICATION IN THE IF STATEMENT
                last = QueueElement.FindLast(first);
            }
            else
                if (QueueElement.Remove(key, first))
                    size--;
        }

        public KeyValuePair<TKey, TValue> Find(TKey key)
        {
            return QueueElement.Find(key, first).data;
        }

        public void Enqueue(KeyValuePair<TKey, TValue> item)
        {
            if (first == null)
            {
                var tmp = new QueueElement(item);
                first = tmp;
                last = tmp;
                size++;

                System.Diagnostics.Debug.Assert(size == 1);
            }
            else
            {
                new QueueElement(item, last);
                size++;
            }

            if(maxSize.HasValue)
            {
                if(size > maxSize)
                {
                    callbackOnDelete(Dequeue());
                }
            }
        }

        public KeyValuePair<TKey, TValue> Dequeue()
        {
            var ret = first.data;
            first = first.next;
            return ret;
        }

        public KeyValuePair<TKey, TValue> Peek()
        {
            return first.data;
        }

        public class QueueElement : ISerializable
        {
            internal QueueElement next = null;
            internal KeyValuePair<TKey, TValue> data;

            public QueueElement()
            {

            }

            public QueueElement(KeyValuePair<TKey, TValue> item)
            {
                this.data = item;
            }

            public QueueElement(KeyValuePair<TKey, TValue> item, QueueElement father)
            {
                this.data = item;

                if(father.next != null)
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
                while(!first.data.Key.Equals(key))
                {
                    if (first.next == null)
                        return null;

                    first = first.next;
                }

                return first;
            }

            public static bool Remove(TKey key, QueueElement first)
            {
                while (first.next != null && !first.next.data.Key.Equals(key))
                {
                    first = first.next;
                }

                if (first.next != null && first.next.data.Key.Equals(key))
                {
                    first.next = first.next.next;
                    return true;
                }

                return false;
            }

            public void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                info.AddValue(nameof(next), next);
                info.AddValue(nameof(data), data);
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
