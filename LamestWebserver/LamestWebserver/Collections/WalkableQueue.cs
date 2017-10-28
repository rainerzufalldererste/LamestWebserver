using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LamestWebserver.Core;

namespace LamestWebserver.Collections
{
    /// <summary>
    /// A Queue just moves the Position in an internal List forward so it's always possible to access every possible index at any given time even after moving through the Queue.
    /// </summary>
    /// <typeparam name="T">The type of </typeparam>
    public class WalkableQueue<T> : IEnumerable<T>, IReadOnlyCollection<T>
    {
        /// <summary>
        /// The internal list storing the queue entries.
        /// </summary>
        protected readonly List<T> _internalList;

        /// <summary>
        /// The current position in the Queue.
        /// </summary>
        public int Position { get; protected set; }

        /// <inheritdoc />
        public int Count => _internalList.Count;
    
        /// <summary>
        /// Retrieves the current Element of the Queue.
        /// </summary>
        public T Current
        {
            get
            {
                if (_internalList.Count <= Position)
                    return default(T);

                return _internalList[Position];
            }
        }

        /// <summary>
        /// Constructs a new WalkableQueue.
        /// </summary>
        /// <param name="objs">The entries to add to the queue.</param>
        public WalkableQueue(params T[] objs)
        {
            _internalList = new List<T>(objs);
        }

        /// <summary>
        /// Constructs a new WalkableQueue from a List;
        /// </summary>
        /// <param name="objs">The entries to add to the queue.</param>
        public WalkableQueue(List<T> objs)
        {
            _internalList = objs;
        }

        /// <summary>
        /// Gets or Sets the element at a given index from this queue.
        /// </summary>
        /// <param name="index">The index of the element.</param>
        /// <returns>The element at a given index.</returns>
        public T this[int index]
        {
            get
            {
                if (Position >= Count)
                    throw new IndexOutOfRangeException();

                return _internalList[Position];
            }

            set
            {
                if (Position >= Count)
                    throw new IndexOutOfRangeException();

                _internalList[Position] = value;
            }
        }

        /// <summary>
        /// Adds the given element to the queue.
        /// </summary>
        /// <param name="obj">The element to add to the queue.</param>
        public void Push(T obj)
        {
            _internalList.Add(obj);
        }

        /// <summary>
        /// Retrieves the next element in the queue without moving the Position forward.
        /// </summary>
        /// <returns>Returns the next element in the queue or default(T)</returns>
        public T Peek()
        {
            if (_internalList.Count <= Position)
                return default(T);

            return _internalList[Position];
        }

        /// <summary>
        /// Retrieves the next element in the queue by moving the Position forward.
        /// </summary>
        /// <returns>Returns the next element in the queue or default(T)</returns>
        public T Pop()
        {
            if (_internalList.Count <= Position)
                return default(T);
            
            return _internalList[Position++];
        }

        /// <summary>
        /// Clears the queue of all elements.
        /// </summary>
        public void Clear()
        {
            Position = 0;
            _internalList.Clear();
        }

        /// <summary>
        /// Retrieves all elements of the queue that we've already consumed.
        /// </summary>
        /// <returns>The elements as list.</returns>
        public List<T> GetPassed() => _internalList.GetRange(0, Position);

        /// <summary>
        /// Retrieves all elements of the queue that we've not consumed yet.
        /// </summary>
        /// <returns>The elements as list.</returns>
        public List<T> GetConsumable() => _internalList.GetRange(Position, _internalList.Count - Position);

        /// <summary>
        /// Retrieves all elements of the queue.
        /// </summary>
        /// <returns>The elements as list.</returns>
        public List<T> GetAll() => _internalList;

        /// <summary>
        /// Retrieves all elements of the queue from a given index with a given length.
        /// </summary>
        /// <param name="startIndex">The index to begin at.</param>
        /// <param name="count">The amount of elements to get.</param>
        /// <returns>The elements as list.</returns>
        public List<T> GetRange(int startIndex, int count) => _internalList.GetRange(startIndex, count);

        /// <summary>
        /// Resets the position of the queue to zero.
        /// </summary>
        public void ResetPosition()
        {
            Position = 0;
        }

        /// <summary>
        /// Retrieves true if the queue is already at it's end or false if not.
        /// </summary>
        /// <returns>Returns true if at end and false if not.</returns>
        public bool AtEnd() => Position >= _internalList.Count - 1;

        /// <inheritdoc />
        public IEnumerator<T> GetEnumerator() => _internalList.GetEnumerator();

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() => _internalList.GetEnumerator();
    }
}
