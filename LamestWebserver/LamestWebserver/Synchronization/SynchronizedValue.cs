using LamestWebserver.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LamestWebserver.Synchronization
{
    [Serializable]
    public class SynchronizedValue<T> : NullCheckable
    {
        private T _value;
        private UsableWriteLock writeLock = new UsableWriteLock();

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

        public SynchronizedValue()
        {

        }

        public SynchronizedValue(T value) : this()
        {
            Value = value;
        }

        public static implicit operator T (SynchronizedValue<T> syncValue) 
        {
            return syncValue.Value;
        }
    }
}
