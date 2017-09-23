using LamestWebserver.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;

namespace LamestWebserver.Synchronization
{
    public class SynchronizedDictionary<T1, T2> : NullCheckable, IDictionary<T1, T2>
    {
        public IDictionary<T1, T2> InnerDictionary;
        private UsableWriteLock writeLock = new UsableWriteLock();

        public SynchronizedDictionary(IDictionary<T1, T2> dictionary)
        {
            InnerDictionary = dictionary;
        }

        public static implicit operator bool(SynchronizedDictionary<T1, T2> obj) => obj == null || obj.InnerDictionary == null;

        /// <inheritdoc />
        public T2 this[T1 key]
        {
            get
            {
                using (writeLock.LockRead())
                    return InnerDictionary[key];
            }

            set
            {
                using (writeLock.LockWrite())
                    InnerDictionary[key] = value;
            }
        }

        /// <inheritdoc />
        public int Count
        {
            get
            {
                using (writeLock.LockRead())
                    return InnerDictionary.Count;
            }
        }

        /// <inheritdoc />
        public bool IsReadOnly
        {
            get
            {
                using (writeLock.LockRead())
                    return InnerDictionary.IsReadOnly;
            }
        }

        /// <inheritdoc />
        public ICollection<T1> Keys
        {
            get
            {
                using (writeLock.LockRead())
                    return InnerDictionary.Keys;
            }
        }

        /// <inheritdoc />
        public ICollection<T2> Values
        {
            get
            {
                using (writeLock.LockRead())
                    return InnerDictionary.Values;
            }
        }

        /// <inheritdoc />
        public void Add(KeyValuePair<T1, T2> item)
        {
            using (writeLock.LockWrite())
                InnerDictionary.Add(item);
        }

        /// <inheritdoc />
        public void Add(T1 key, T2 value)
        {
            using (writeLock.LockWrite())
                InnerDictionary.Add(key, value);
        }

        /// <inheritdoc />
        public void Clear()
        {
            using (writeLock.LockWrite())
                InnerDictionary.Clear();
        }

        /// <inheritdoc />
        public bool Contains(KeyValuePair<T1, T2> item)
        {
            using (writeLock.LockRead())
                return InnerDictionary.Contains(item);
        }

        /// <inheritdoc />
        public bool ContainsKey(T1 key)
        {
            using (writeLock.LockRead())
                return InnerDictionary.ContainsKey(key);
        }

        /// <inheritdoc />
        public void CopyTo(KeyValuePair<T1, T2>[] array, int arrayIndex)
        {
            using (writeLock.LockRead())
                InnerDictionary.CopyTo(array, arrayIndex);
        }

        /// <inheritdoc />
        public IEnumerator<KeyValuePair<T1, T2>> GetEnumerator()
        {
            using (writeLock.LockRead())
                return InnerDictionary.GetEnumerator();
        }

        /// <inheritdoc />
        public bool Remove(KeyValuePair<T1, T2> item)
        {
            using (writeLock.LockWrite())
                return InnerDictionary.Remove(item);
        }

        /// <inheritdoc />
        public bool Remove(T1 key)
        {
            using (writeLock.LockWrite())
                return InnerDictionary.Remove(key);
        }

        /// <inheritdoc />
        public bool TryGetValue(T1 key, out T2 value)
        {
            using (writeLock.LockRead())
                return InnerDictionary.TryGetValue(key, out value);
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            using (writeLock.LockRead())
                return InnerDictionary.GetEnumerator();
        }
    }
}
