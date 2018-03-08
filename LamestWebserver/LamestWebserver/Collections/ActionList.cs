using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LamestWebserver.Collections
{
    /// <summary>
    /// List with the ability to do a action every time it's been change.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ActionList<T> : IEnumerable<T>
    {
        /// <summary>
        /// Action that get executed after each manipulation.
        /// </summary>
        public Action ActionToExecute;

        private List<T> _internalList;

        /// <summary>
        /// Constructs an empty ActionList.
        /// </summary>
        public ActionList()
        {
            _internalList = new List<T>();
        }
        
        /// <summary>
        /// Constructs an empty ActionList.
        /// </summary>
        /// <param name="action">The action to execute on change.</param>
        public ActionList(Action action)
        {
            if (action == null)
                throw new ArgumentNullException();

            _internalList = new List<T>();
            ActionToExecute = action;
        }

        /// <summary>
        /// Constructs an empty ActionList.
        /// </summary>
        /// <param name="collection">The collection to use as internal list.</param>
        /// <param name="action">The action to execute on change.</param>
        public ActionList(IEnumerable<T> collection, Action action)
        {
            if (collection == null)
                throw new ArgumentNullException();

            if (action == null)
                throw new ArgumentNullException();

            if (collection is List<T>)
                _internalList = (List<T>)collection;
            else
                _internalList = new List<T>(collection);

            ActionToExecute = action;
        }

        /// <inheritdoc />
        public T this[int index]
        {
            get
            {
                return _internalList[index];
            }

            set
            {
                _internalList[index] = value;
            }
        }

        /// <inheritdoc />
        public int Count => _internalList.Count;

        /// <inheritdoc />
        public bool IsReadOnly => false;

        /// <inheritdoc />
        public void Add(T item)
        {
            _internalList.Add(item);
            ActionToExecute();
        }

        /// <inheritdoc />
        public void Clear()
        {
            _internalList.Clear();
            ActionToExecute();
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
            ActionToExecute();
        }

        /// <inheritdoc />
        public bool Remove(T item)
        {
            bool ret = _internalList.Remove(item);

            if (ret)
                ActionToExecute();

            return ret;
        }

        /// <inheritdoc />
        public void RemoveAt(int index)
        {
            _internalList.RemoveAt(index);
            ActionToExecute();
        }

        /// <inheritdoc />
        public void RemoveAll(Predicate<T> match)
        {
            _internalList.RemoveAll(match);
            ActionToExecute();
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() => _internalList.GetEnumerator();
    }
}
