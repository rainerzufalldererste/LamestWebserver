using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace LamestWebserver.Collections
{
    public class AVLHashMap<TKey, TValue> : IDictionary<TKey, TValue>, ISerializable where TKey : IEqualityComparer
    {
        private int size = 1024;
        protected int elementCount = 0;

        private object[] HashMap;
        
        private FriendlyQueue<TKey, TValue> lastAdded = null;

        public AVLHashMap(int size = 1024, uint? maxSize = null)
        {
            this.size = size;
            if (maxSize.HasValue && maxSize > 0)
            {
                HashMap = new object[size];
                lastAdded = new FriendlyQueue<TKey, TValue>(maxSize, (KeyValuePair<TKey, TValue> item) => { Remove(item); });
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
                else if (HashMap[hash] is KeyValuePair<TKey, TValue>)
                {
                    if (((KeyValuePair<TKey, TValue>)HashMap[hash]).Key.Equals(key))
                        return ((KeyValuePair<TKey, TValue>)HashMap[hash]).Value;
                    else
                        return default(TValue);
                }
                else // if HashMap[hash] is an AVL Node search for it
                    throw new NotImplementedException();
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
            throw new NotImplementedException();
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
            else if(HashMap[hash] is KeyValuePair<TKey, TValue>)
            {
                if(((KeyValuePair<TKey, TValue>)HashMap[hash]).Key.Equals(item.Key))
                {
                    lastAdded.Remove(item.Key);
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
    }
}
