using LamestWebserver.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;

namespace LamestWebserver.Synchronization
{
    /// <summary>
    /// Provides synchronized access to an ICollection&lt;T&gt;.
    /// </summary>
    /// <typeparam name="T">The type of the Collection.</typeparam>
    /// <typeparam name="TCollectionType">The internal implementation of the Collection used.</typeparam>
    [Serializable]
    public class SynchronizedCollection<T, TCollectionType> : NullCheckable, ICollection<T> where TCollectionType : ICollection<T>, new()
    {
        /// <summary>
        /// The internal Collection for unsynchronized access.
        /// </summary>
        public TCollectionType InnerCollection { get; protected set; }

        private UsableWriteLock writeLock = new UsableWriteLock();

        /// <summary>
        /// Constructs a new SynchronizedCollection object and initializes the InnerCollection with it's default constructor.
        /// </summary>
        public SynchronizedCollection()
        {
            InnerCollection = new TCollectionType();
        }

        /// <summary>
        /// Constructs a new SynchronizedCollection.
        /// </summary>
        /// <param name="collection">the collection to use</param>
        public SynchronizedCollection(TCollectionType collection)
        {
            InnerCollection = collection;
        }

        /// <summary>
        /// Provides functionality like NullCheckable.
        /// </summary>
        /// <param name="obj">The current object.</param>
        public static implicit operator bool(SynchronizedCollection<T, TCollectionType> obj) => obj == null || obj.InnerCollection == null;


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
