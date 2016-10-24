using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace LamestWebserver.Collections
{
    [Serializable]
    public struct SerializableKeyValuePair<TKey, TValue> : ISerializable
    {
        public TKey Key;
        public TValue Value;

        public SerializableKeyValuePair(TKey Key, TValue Value) : this()
        {
            this.Key = Key;
            this.Value = Value;
        }

        public static explicit operator SerializableKeyValuePair<TKey, TValue>(KeyValuePair<TKey, TValue> input)
        {
            return new SerializableKeyValuePair<TKey, TValue>(input.Key, input.Value);
        }

        public static implicit operator KeyValuePair<TKey, TValue>(SerializableKeyValuePair<TKey, TValue> input)
        {
            return new KeyValuePair<TKey, TValue>(input.Key, input.Value);
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(Key), Key);
            info.AddValue(nameof(Value), Value);
        }
    }
}
