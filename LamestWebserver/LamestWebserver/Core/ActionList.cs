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
    public class ActionList<T> : IList<T>
    {
        /// <summary>
        /// Action that get executed after each manipulation
        /// </summary>
        public Action action;

        internal List<T> internalList;

        /// <summary>
        /// Initilize a new ActionList
        /// </summary>
        public ActionList()
        {
            internalList = new List<T>();
        }

        /// <summary>
        /// Initilize a new ActionList with a Action
        /// </summary>
        public ActionList(Action action)
        {
            internalList = new List<T>();
            this.action = action;
        }

        /// <summary>
        /// Initilize a new ActionList 
        /// </summary>
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
        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public T this[int index] { get => internalList[index]; set => internalList[index] = value; }

        /// <summary>
        /// 
        /// </summary>
        public int Count => internalList.Count;

        /// <summary>
        /// 
        /// </summary>
        public bool IsReadOnly => false;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        public void Add(T item)
        {
            internalList.Add(item);
            action();
        }

        /// <summary>
        /// 
        /// </summary>
        public void Clear()
        {
            internalList.Clear();
            action();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Contains(T item)
        {
            return internalList.Contains(item);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="array"></param>
        /// <param name="arrayIndex"></param>
        public void CopyTo(T[] array, int arrayIndex)
        {
            internalList.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public IEnumerator<T> GetEnumerator()
        {
            return internalList.GetEnumerator();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public int IndexOf(T item)
        {
            return internalList.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            internalList.Insert(index, item);
            action();
        }

        public bool Remove(T item)
        {

            bool ret = internalList.Remove(item);
            action();
            return ret;
        }

        public void RemoveAt(int index)
        {
            internalList.RemoveAt(index);
            action();
        }

        public void RemoveAll(Predicate<T> match)
        {
            internalList.RemoveAll(match);
            action();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return internalList.GetEnumerator();
        }
    }
}
