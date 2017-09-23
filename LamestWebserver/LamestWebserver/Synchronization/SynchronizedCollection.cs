using LamestWebserver.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;

namespace LamestWebserver.Synchronization
{
    public class SynchronizedCollection<T> : NullCheckable, ICollection<T>
    {
        public ICollection<T> InnerCollection;
        private UsableWriteLock writeLock = new UsableWriteLock();

        public SynchronizedCollection(ICollection<T> collection)
        {
            InnerCollection = collection;
        }

        public static implicit operator bool(SynchronizedCollection<T> obj) => obj == null || obj.InnerCollection == null;


        /// <inheritdoc />
        public int Count
        {
            get
            {
                using(writeLock.LockRead())
                    return InnerCollection.Count;
            }
        }

        /// <inheritdoc />
        public bool IsReadOnly
        {
            get
            {
                using (writeLock.LockRead())
                    return InnerCollection.IsReadOnly;
            }
        }

        /// <inheritdoc />
        public void Add(T item)
        {
            using (writeLock.LockWrite())
                InnerCollection.Add(item);
        }

        /// <inheritdoc />
        public void Clear()
        {
            using (writeLock.LockWrite())
                InnerCollection.Clear();
        }

        /// <inheritdoc />
        public bool Contains(T item)
        {
            using (writeLock.LockRead())
                return InnerCollection.Contains(item);
        }

        /// <inheritdoc />
        public void CopyTo(T[] array, int arrayIndex)
        {
            using (writeLock.LockRead())
                InnerCollection.CopyTo(array, arrayIndex);
        }

        /// <inheritdoc />
        public IEnumerator<T> GetEnumerator()
        {
            using (writeLock.LockRead())
                return InnerCollection.GetEnumerator();
        }

        /// <inheritdoc />
        public bool Remove(T item)
        {
            using (writeLock.LockWrite())
                return InnerCollection.Remove(item);
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            using (writeLock.LockRead())
                return InnerCollection.GetEnumerator();
        }
    }
}
