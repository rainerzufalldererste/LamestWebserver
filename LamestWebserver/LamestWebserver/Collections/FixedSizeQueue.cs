using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LamestWebserver.Collections
{
    public class FixedSizeQueue<T> : IEnumerable<T>
    {
        private T[] _data;
        private int _startPosition = 0;
        private int _maxSize = 0;

        public int MaximumSize
        {
            get => _maxSize;

            set
            {
                T[] newData = new T[value];

                if (_data != null)
                {
                    CopyTo(newData, 0);
                    _data = newData;
                }

                _maxSize = value;
            }
        }

        public int Count { get; private set; } = 0;
        
        public FixedSizeQueue(int maximumSize)
        {
            MaximumSize = maximumSize;
            _startPosition = 0;
        }

        public void Push(T element)
        {
            _startPosition--;

            if (_startPosition < 0)
                _startPosition = _maxSize - 1;

            _data[_startPosition] = element;

            if (Count < _maxSize)
                Count++;
        }

        public T this [int index]
        {
            get
            {
                if (index > _maxSize || index < 0)
                    throw new IndexOutOfRangeException();

                return _data[(_startPosition + index) % _maxSize];
            }
        }

        public bool IsReadOnly => true;

        public void Clear()
        {
            for (int i = 0; i < _maxSize; i++)
                _data[i] = default(T);

            Count = 0;
            _startPosition = _maxSize - 1;
        }

        public bool Contains(T item)
        {
            for (int i = _startPosition; i < _maxSize; i++)
                if (_data[i].Equals(item))
                    return true;

            for (int i = 0; i < _startPosition; i++)
                if (_data[i].Equals(item))
                    return true;

            return false;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
### COPY FROM BITLIST!!!

            for (int i = _startPosition; i > -0; i--)
                array[arrayIndex++] = _data[i];

            for (int i = _maxSize - 1; i > _startPosition; i--)
                array[arrayIndex++] = _data[i];
        }

        public IEnumerator<T> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
}
