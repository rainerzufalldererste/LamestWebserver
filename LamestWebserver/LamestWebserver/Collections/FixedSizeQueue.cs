using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LamestWebserver.Collections
{
    /// <summary>
    /// A Queue of fixed size that just wraps around and overrides the oldest elements if full.
    /// </summary>
    /// <typeparam name="T">The type of the queue elements.</typeparam>
    public class FixedSizeQueue<T> : IEnumerable<T>, IReadOnlyCollection<T>
    {
        private T[] _data;
        private int _startPosition = 0;
        private int _maxCapacity = 0;

        /// <summary>
        /// Gets or sets the maximum capacity of this FixedSizeQueue.
        /// </summary>
        public int MaximumCapacity
        {
            get
            {
                return _maxCapacity;
            }

            set
            {
                T[] newData = new T[value];

                if (_data != null)
                {
                    CopyTo(newData, 0);
                }

                _data = newData;
                _maxCapacity = value;
            }
        }

        /// <summary>
        /// The number of elements in this queue (capped by MaximumCapacity).
        /// </summary>
        public int Count { get; private set; } = 0;
        
        /// <summary>
        /// Constructs a new FixedSizeQueue.
        /// </summary>
        /// <param name="maximumCapacity">The maximum capacity of this Queue.</param>
        public FixedSizeQueue(int maximumCapacity)
        {
            MaximumCapacity = maximumCapacity;
            _startPosition = 0;
        }

        /// <summary>
        /// Adds an element to the queue.
        /// </summary>
        /// <param name="element">The element to add.</param>
        public void Push(T element)
        {
            _startPosition--;

            if (_startPosition < 0)
                _startPosition = _maxCapacity - 1;

            _data[_startPosition] = element;

            if (Count < _maxCapacity)
                Count++;
        }

        /// <summary>
        /// Gets an element from this queue at the given index. The last added entry will be index 0 and all others can be accessed in order by the index counting upwards.
        /// </summary>
        /// <param name="index">The index of the element to get.</param>
        /// <returns>The element at the given index.</returns>
        public T this [int index]
        {
            get
            {
                if (index >= Count || index < 0)
                    throw new IndexOutOfRangeException();

                return _data[(_startPosition + index) % _maxCapacity];
            }
        }

        /// <summary>
        /// Clears the FixesSizeQueue.
        /// </summary>
        public void Clear()
        {
            for (int i = 0; i < _maxCapacity; i++)
                _data[i] = default(T);

            Count = 0;
            _startPosition = _maxCapacity - 1;
        }

        /// <summary>
        /// Returns true if this FixedSizeQueue contains a given element.
        /// </summary>
        /// <param name="item">The element to look for.</param>
        /// <returns>Returns true if the element was found and false if not.</returns>
        public bool Contains(T item)
        {
            for (int i = 0; i < Count; i++)
                if (_data[(_startPosition + i) % _maxCapacity].Equals(item))
                    return true;

            return false;
        }

        /// <summary>
        /// Copies the contents of this FixedSizeQueue into an array.
        /// </summary>
        /// <param name="array">The array to copy to.</param>
        /// <param name="arrayIndex">The index to start with in the given array.</param>
        public void CopyTo(T[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));

            if (arrayIndex < 0)
                throw new IndexOutOfRangeException($"The given {nameof(arrayIndex)} has to be greater than zero.");

            if (array.Length < Count + arrayIndex)
                throw new InvalidOperationException($"The given array is not large enough (also taking {nameof(arrayIndex)} into account) to contain this {nameof(FixedSizeQueue<T>)}.");

            for (int i = 0; i < Count; i++)
                array[arrayIndex++] = _data[(_startPosition + i) % _maxCapacity];
        }

        /// <inheritdoc />
        public IEnumerator<T> GetEnumerator()
        {
            // TODO: Implement properly.

            T[] array = new T[Count];

            CopyTo(array, 0);

            return array.ToList().GetEnumerator();
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            // TODO: Implement properly.

            T[] array = new T[Count];

            CopyTo(array, 0);

            return array.GetEnumerator();
        }
    }
}
