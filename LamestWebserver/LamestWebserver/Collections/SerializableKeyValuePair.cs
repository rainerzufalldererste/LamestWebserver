using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using LamestWebserver.Serialization;

namespace LamestWebserver.Collections
{
    /// <summary>
    /// A serializable Alternative to a KeyValuePair
    /// </summary>
    /// <typeparam name="TKey">the Type of the Key</typeparam>
    /// <typeparam name="TValue">the Type of the Value</typeparam>
    [Serializable]
    public struct SerializableKeyValuePair<TKey, TValue> : ISerializable, IXmlSerializable
    {
        /// <summary>
        /// The Key
        /// </summary>
        public TKey Key;

        /// <summary>
        /// The Value
        /// </summary>
        public TValue Value;

        /// <summary>
        /// Constructs a new Serializable Key Value Pair
        /// </summary>
        /// <param name="Key">the Key</param>
        /// <param name="Value">the Value</param>
        public SerializableKeyValuePair(TKey Key, TValue Value) : this()
        {
            this.Key = Key;
            this.Value = Value;
        }

        /// <summary>
        /// Casts a KeyValuePair to a SerializableKeyValuePair
        /// </summary>
        /// <param name="input">the KeyValuePair</param>
        public static explicit operator SerializableKeyValuePair<TKey, TValue>(KeyValuePair<TKey, TValue> input)
        {
            return new SerializableKeyValuePair<TKey, TValue>(input.Key, input.Value);
        }

        /// <summary>
        /// Casts a SerializableKeyValuePair to a KeyValuePair
        /// </summary>
        /// <param name="input">the SerializableKeyValuePair</param>
        public static implicit operator KeyValuePair<TKey, TValue>(SerializableKeyValuePair<TKey, TValue> input)
        {
            return new KeyValuePair<TKey, TValue>(input.Key, input.Value);
        }

        /// <inheritdoc />
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(Key), Key);
            info.AddValue(nameof(Value), Value);
        }

        /// <inheritdoc />
        public XmlSchema GetSchema()
        {
            return null;
        }

        /// <inheritdoc />
        public void ReadXml(XmlReader reader)
        {
            Key = reader.ReadElement<TKey>(nameof(Key));
            Value = reader.ReadElement<TValue>(nameof(Value));
            reader.Read();
        }

        /// <inheritdoc />
        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement("SerializableKeyValuePair");
            writer.WriteElement(nameof(Key), Key);
            writer.WriteElement(nameof(Value), Value);
            writer.WriteEndElement();
        }
    }
}
