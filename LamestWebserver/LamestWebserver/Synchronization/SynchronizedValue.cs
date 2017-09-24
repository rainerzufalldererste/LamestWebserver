using LamestWebserver.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LamestWebserver.Synchronization
{
    /// <summary>
    /// Provides synchonized access to a variable.
    /// </summary>
    /// <typeparam name="T">The Type of the variable.</typeparam>
    [Serializable]
    public class SynchronizedValue<T> : NullCheckable
    {
        private T _value;
        private UsableWriteLock writeLock = new UsableWriteLock();

        /// <summary>
        /// The synchronized Value.
        /// </summary>
        public T Value
        {
            get
            {
                using (writeLock.LockRead())
                    return _value;
            }
            set
            {
                using (writeLock.LockWrite())
                    _value = value;
            }
        }
        
        /// <summary>
        /// Empty or Deserialition Constructor.
        /// </summary>
        public SynchronizedValue()
        {

        }

        /// <summary>
        /// Constructs a new SynchronizedValue object and initializes the internal Value.
        /// </summary>
        /// <param name="value">The initial value.</param>
        public SynchronizedValue(T value) : this()
        {
            Value = value;
        }

        /// <summary>
        /// Reads from the Value synchronously.
        /// (You can easily get unsynchronized access using this cast if you set a variable 'T x' to this and then start using 'x' instead of this SynchronizedValue&lt;T&gt;)
        /// </summary>
        /// <param name="syncValue">The SynchronizedValue to read from.</param>
        public static implicit operator T (SynchronizedValue<T> syncValue) 
        {
            return syncValue.Value;
        }
    }
}
