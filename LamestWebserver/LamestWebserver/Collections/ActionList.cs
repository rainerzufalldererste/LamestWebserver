using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LamestWebserver.Collections
{
    /// <summary>
    /// List with the ability to do a action every time you manipulate it.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ActionList<T> : IEnumerable<T>
    {
        /// <summary>
        /// Action that get executed after each manipulation.
        /// </summary>
        public Action action;

        private List<T> _internalList;

        /// <inheritdoc />
        public ActionList()
        {
            _internalList = new List<T>();
        }

        /// <inheritdoc />
        public ActionList(Action action)
        {
            _internalList = new List<T>();
            this.action = action;
        }

        /// <inheritdoc />
        public ActionList(IEnumerable<T> collection)
        {
            _internalList = new List<T>(collection);
        }

        /// <summary>
        /// Initialize a new ActionList with a capacity.
        /// </summary>
        /// <param name="capacity"></param>
        public ActionList(int capacity)
        {
            _internalList = new List<T>(capacity);
        }

        /// <inheritdoc />
        public T this[int index] { get => _internalList[index]; set => _internalList[index] = value; }

        /// <inheritdoc />
        public int Count => _internalList.Count;

        /// <inheritdoc />
        public bool IsReadOnly => false;

        /// <inheritdoc />
        public void Add(T item)
        {
            _internalList.Add(item);
            action();
        }

        /// <inheritdoc />
        public void Clear()
        {
            _internalList.Clear();
            action();
        }

        /// <inheritdoc />
        public bool Contains(T item) => _internalList.Contains(item);

        /// <inheritdoc />
        public void CopyTo(T[] array, int arrayIndex) => _internalList.CopyTo(array, arrayIndex);

        /// <inheritdoc />
        public IEnumerator<T> GetEnumerator() => _internalList.GetEnumerator();

        /// <inheritdoc />
        public int IndexOf(T item) => _internalList.IndexOf(item);

        /// <inheritdoc />
        public void Insert(int index, T item)
        {
            _internalList.Insert(index, item);
            action();
        }

        /// <inheritdoc />
        public bool Remove(T item)
        {
            bool ret = _internalList.Remove(item);
            action();
            return ret;
        }

        /// <inheritdoc />
        public void RemoveAt(int index)
        {
            _internalList.RemoveAt(index);
            action();
        }

        /// <inheritdoc />
        public void RemoveAll(Predicate<T> match)
        {
            _internalList.RemoveAll(match);
            action();
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() => _internalList.GetEnumerator();
    }
}
