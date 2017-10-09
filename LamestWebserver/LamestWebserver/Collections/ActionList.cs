using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LamestWebserver.Core
{
    /// <summary>
    /// List with the ability to do a action everytime you manipulate it
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ActionList<T> : IEnumerable<T>
    {
        /// <summary>
        /// Action that get executed after each manipulation
        /// </summary>
        public Action action;

        internal List<T> internalList;

        /// <inheritdoc />
        public ActionList()
        {
            internalList = new List<T>();
        }

        /// <inheritdoc />
        public ActionList(Action action)
        {
            internalList = new List<T>();
            this.action = action;
        }

        /// <inheritdoc />
        public ActionList(IEnumerable<T> collection)
        {
            internalList = new List<T>(collection);
        }

        /// <summary>
        /// Initilize a new ActionList with a capacity
        /// </summary>
        /// <param name="capacity"></param>
        public ActionList(int capacity)
        {
            internalList = new List<T>(capacity);
        }

        /// <inheritdoc />
        public T this[int index] { get => internalList[index]; set => internalList[index] = value; }

        /// <inheritdoc />
        public int Count => internalList.Count;

        /// <inheritdoc />
        public bool IsReadOnly => false;

        /// <inheritdoc />
        public void Add(T item)
        {
            internalList.Add(item);
            action();
        }

        /// <inheritdoc />
        public void Clear()
        {
            internalList.Clear();
            action();
        }

        /// <inheritdoc />
        public bool Contains(T item) => internalList.Contains(item);

        /// <inheritdoc />
        public void CopyTo(T[] array, int arrayIndex) => internalList.CopyTo(array, arrayIndex);

        /// <inheritdoc />
        public IEnumerator<T> GetEnumerator() => internalList.GetEnumerator();

        /// <inheritdoc />
        public int IndexOf(T item) => internalList.IndexOf(item);

        /// <inheritdoc />
        public void Insert(int index, T item)
        {
            internalList.Insert(index, item);
            action();
        }

        /// <inheritdoc />
        public bool Remove(T item)
        {
            bool ret = internalList.Remove(item);
            action();
            return ret;
        }

        /// <inheritdoc />
        public void RemoveAt(int index)
        {
            internalList.RemoveAt(index);
            action();
        }

        /// <inheritdoc />
        public void RemoveAll(Predicate<T> match)
        {
            internalList.RemoveAll(match);
            action();
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() => internalList.GetEnumerator();
    }
}
