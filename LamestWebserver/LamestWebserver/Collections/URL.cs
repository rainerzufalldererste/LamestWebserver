using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LamestWebserver.Collections
{
    public class URL<T>
    {
        private T[] _folders;
        private string _delimiter;

        public int Count => _folders.Length;

        public T this[int index]
        {
            get
            {
                if (index >= Count)
                    throw new IndexOutOfRangeException();

                return _folders[index];
            }
        }
        
        public URL(IEnumerable<T> folders, string delimiter = "/")
        {
            if (folders is T[])
                _folders = (T[])folders;
            else
                _folders = folders.ToArray();
        }

        public URL<T> Append(T item)
        {
            T[] innerFolders = new T[_folders.Length + 1];

            Array.Copy(_folders, innerFolders, _folders.Length);
            innerFolders[innerFolders.Length - 1] = item;

            return new URL<T>(innerFolders, _delimiter);
        }

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
    }
}
