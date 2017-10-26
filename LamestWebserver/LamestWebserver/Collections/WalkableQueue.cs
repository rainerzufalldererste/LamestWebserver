using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LamestWebserver.Collections
{
    public class WalkableQueue<T> : IEnumerable<T>
    {
        public List<T> InternalList;
        public int Position = 0;

        public T Current => InternalList[Position];

        public WalkableQueue(params T[] objs)
        {
            InternalList = new List<T>(objs);
        }

        public WalkableQueue(List<T> objs)
        {
            InternalList = objs;
        }

        public void Push(T obj)
        {
            InternalList.Add(obj);
        }

        public T Peek()
        {
            if (InternalList.Count <= Position)
                return default(T);

            return InternalList[Position];
        }

        public T Pop()
        {
            if (InternalList.Count <= Position)
                return default(T);
            
            return InternalList[Position++];
        }

        public void Clear()
        {
            Position = 0;
            InternalList.Clear();
        }

        public List<T> GetPassed() => InternalList.GetRange(0, Position);

        public List<T> GetConsumable() => InternalList.GetRange(Position, InternalList.Count - Position);

        public List<T> GetAll() => InternalList;

        public void ResetPosition()
        {
            Position = 0;
        }

        public bool AtEnd() => Position >= InternalList.Count - 1;

        public IEnumerator<T> GetEnumerator() => InternalList.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => InternalList.GetEnumerator();
    }
}
