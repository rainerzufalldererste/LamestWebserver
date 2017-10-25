using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LamestWebserver.Core;

#if x86
using size_t = System.UInt32;
#elif x64
using size_t = System.UInt64;
#else
using size_t = System.UInt64;
#endif

namespace LamestWebserver.Collections
{
    /// <summary>
    /// A special - very storage effective - List for Bits.
    /// </summary>
    public class BitList : NullCheckable, IList<bool>
    {
        private List<size_t> _data = new List<size_t>();
        private int _position = 0;

        private const int BitsInSizeT = sizeof(size_t) << 3;

        /// <inheritdoc />
        public bool this[int index]
        {
            get
            {
                if (index < _position)
                    return ((size_t)_data[(int)((size_t)index >> (sizeof(size_t) - 2))] & (size_t)((size_t)1 << (int)(index % BitsInSizeT))) != (size_t)0;

                throw new IndexOutOfRangeException();
            }

            set
            {
                if (index < _position)
                {
                    int i = (int)((size_t)index >> (sizeof(size_t) - 2));
                    int j = (int)((size_t)index % BitsInSizeT);
                    size_t data = _data[i];

                    data &= ~((size_t)1 << j);

                    if (value)
                        data |= ((size_t)1 << j);

                    _data[i] = data;

                    return;
                }

                throw new IndexOutOfRangeException();
            }
        }

        /// <inheritdoc />
        public int Count => _position;

        /// <inheritdoc />
        public bool IsReadOnly => false;

        /// <inheritdoc />
        public void Add(bool item)
        {
            int i = _position >> (sizeof(size_t) - 2);

            if (i >= _data.Count)
                _data.Add(0);

            if (!item)
            {
                _position++;
                return;
            }

            _data[i] |= (size_t)((size_t)1 << (_position % BitsInSizeT));

            _position++;
        }

        /// <inheritdoc />
        public void Clear()
        {
            _position = 0;
            _data.Clear();
        }

        /// <inheritdoc />
        public bool Contains(bool item)
        {
            if (item)
            {
                foreach (size_t num in _data)
                    if (num != 0)
                        return true;

                return false;
            }
            else
            {
                for (int i = 0; i < (_position - 1) >> (sizeof(size_t) - 2); i++)
                    if (_data[i] != size_t.MaxValue)
                        return true;

                size_t data = _data.Last();

                for (int i = 0; i < _position % BitsInSizeT; i++)
                    if ((size_t)(data & ((size_t)1 << i)) == (size_t)0)
                        return true;

                return false;
            }
        }

        /// <inheritdoc />
        public void CopyTo(bool[] array, int arrayIndex)
        {
            if (array == null)
                throw new NullReferenceException(nameof(array));

            if (arrayIndex < 0)
                throw new IndexOutOfRangeException($"The given {nameof(arrayIndex)} has to be greater than zero.");

            if (array.Length < Count + arrayIndex)
                throw new InvalidOperationException($"The given array is not large enough (also taking {nameof(arrayIndex)} into account) to contain this {nameof(BitList)}.");

            if (Count == 0)
                return;

            for (int i = 0; i < _data.Count - 1; i++)
                for (size_t j = 1; j > 0; j <<= 1)
                    array[arrayIndex++] = (size_t)(_data[i] & j) != (size_t)0;

            if(_position % BitsInSizeT != 0)
                for (int j = 0; j < _position % BitsInSizeT; j++)
                    array[arrayIndex++] = (size_t)(_data.Last() & (size_t)((size_t)1 << j)) != (size_t)0;
            else
                for (int j = 0; j < BitsInSizeT; j++)
                    array[arrayIndex++] = (size_t)(_data.Last() & (size_t)((size_t)1 << j)) != (size_t)0;
        }

        /// <inheritdoc />
        public IEnumerator<bool> GetEnumerator() => new BitListEnumerator(_data.GetEnumerator(), Count);

        /// <inheritdoc />
        public int IndexOf(bool item)
        {
            for (int i = 0; i < Count; i++)
                if (this[i] == item)
                    return i;

            return -1;
        }

        /// <inheritdoc />
        public void Insert(int index, bool item)
        {
            if (index == Count)
            {
                Add(item);
                return;
            }
            else if (index > Count)
            {
                throw new IndexOutOfRangeException();
            }

            int indexi = index >> (sizeof(size_t) - 2);
            int indexj = index % BitsInSizeT;

            if (_position % BitsInSizeT == BitsInSizeT - 1)
                _data.Add((_data[_data.Count - 1] & (size_t)((size_t)1 << (BitsInSizeT - 1))) >> (BitsInSizeT - 1));
            else if(indexi + 1 < _data.Count)
                _data[_data.Count - 1] <<= 1;

            for (int i = _data.Count - 2; i > indexi; i--)
            {
                _data[i + 1] |= ((_data[i] & (size_t)((size_t)1 << (BitsInSizeT - 1))) >> (BitsInSizeT - 1));
                _data[i] <<= 1;
            }

            size_t data = _data[indexi];

            if(_data.Count > indexi + 1)
                _data[indexi + 1] |= ((data & (size_t)((size_t)1 << (BitsInSizeT - 1))) >> (BitsInSizeT - 1));

            _data[indexi] = 0;

            for (int j = 0; j < indexj; j++)
                _data[indexi] |= ((data & ((size_t)1 << j)));

            if (item)
                _data[indexi] |= ((size_t)1 << indexj);

            data <<= 1;

            for (int j = indexj + 1; j < BitsInSizeT; j++)
                _data[indexi] |= (data & ((size_t)1 << j));

            _position++;
        }

        /// <inheritdoc />
        public bool Remove(bool item)
        {
            int index = -1;

            for (int i = 0; i < Count; i++)
            {
                if (this[i] == item)
                {
                    index = i;
                    break;
                }
            }

            if (index < 0)
                return false;

            RemoveAt(index);

            return true;
        }

        /// <inheritdoc />
        public void RemoveAt(int index)
        {
            if (index >= Count)
                throw new IndexOutOfRangeException();

            int indexi = index >> (sizeof(size_t) - 2);
            int indexj = index % BitsInSizeT;

            size_t data = _data[indexi];

            _data[indexi] = 0;

            for (int j = 0; j < indexj; j++)
                _data[indexi] |= (size_t)(data & (size_t)((size_t)1 << j));

            for (int j = indexj + 1; j < BitsInSizeT; j++)
                _data[indexi] |= ((data & (size_t)((size_t)1 << j)) >> 1);

            for (int i = indexi + 1; i <= (_position - 1) >> (sizeof(size_t) - 2); i++)
            {
                _data[i - 1] |= ((size_t)(_data[i] & 1) << (BitsInSizeT - 1));
                _data[i] >>= 1;
            }

            _position--;

            if (_position % BitsInSizeT == 0)
                _data.RemoveAt(_data.Count - 1);
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() => new BitListEnumerator(_data.GetEnumerator(), Count);

        private class BitListEnumerator : IEnumerator, IEnumerator<bool>
        {
            private IEnumerator<size_t> _enumerator;
            private int _position = -1;
            private int _blocks = 0;
            private int _size;

            public BitListEnumerator(IEnumerator<size_t> enumerator, int size)
            {
                _enumerator = enumerator;
                _size = size;
            }

            object IEnumerator.Current => (_enumerator.Current & ((size_t)1 << _position)) != 0;

            bool IEnumerator<bool>.Current => (_enumerator.Current & ((size_t)1 << _position)) != 0;

            public bool MoveNext()
            {
                if (_position < 0 && !_enumerator.MoveNext()) // simply exists to call _enumerator.MoveNext() on first call.
                    return false;

                _position++;

                if (_position == BitsInSizeT)
                {
                    if (!_enumerator.MoveNext())
                        return false;

                    _position = 0;
                    _blocks++;
                }

                if (_blocks * BitsInSizeT + _position >= _size)
                    return false;

                return true;
            }

            public void Reset()
            {
                _position = 0;
                _blocks = 0;
            }

            public void Dispose()
            {
                _enumerator.Dispose();
            }
        }
    }
}
