using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LamestWebserver.Collections
{
    /// <summary>
    /// A fixed collection of elements.
    /// </summary>
    /// <typeparam name="T">The type of the elements.</typeparam>
    public class URL<T> : IEnumerable<T>, IReadOnlyCollection<T>
    {
        private T[] _folders;
        private string _delimiter;

        /// <summary>
        /// Retrieves the amount of elements in this URL.
        /// </summary>
        public int Count => _folders.Length;

        /// <summary>
        /// Gets an element inside the URL.
        /// </summary>
        /// <param name="index">The index to get the element at.</param>
        /// <returns>Returns the element at this index.</returns>
        public T this[int index]
        {
            get
            {
                if (index >= Count)
                    throw new IndexOutOfRangeException();

                return _folders[index];
            }
        }
        
        /// <summary>
        /// Creates a new URL out of the given folders.
        /// </summary>
        /// <param name="folders">The folders of this URL.</param>
        /// <param name="delimiter">The delimiter to display the URL with.</param>
        public URL(IEnumerable<T> folders, string delimiter = "/")
        {
            if (folders is T[])
                _folders = (T[])folders;
            else
                _folders = folders.ToArray();

            _delimiter = delimiter;
        }

        /// <summary>
        /// Adds and item to the URL.
        /// </summary>
        /// <param name="item">The item to add.</param>
        /// <returns>A new URL with this element appended to.</returns>
        public URL<T> Append(T item)
        {
            T[] innerFolders = new T[_folders.Length + 1];

            Array.Copy(_folders, innerFolders, _folders.Length);
            innerFolders[innerFolders.Length - 1] = item;

            return new URL<T>(innerFolders, _delimiter);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            string ret = "";

            for (int i = 0; i < _folders.Length - 1; i++)
            {
                ret += _folders[i] + _delimiter;
            }

            if (_folders.Length > 0)
                ret += _folders.Last();

            return ret;
        }

        /// <inheritdoc />
        public IEnumerator<T> GetEnumerator() => _folders.ToList().GetEnumerator();

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() => _folders.GetEnumerator();
    }
}
