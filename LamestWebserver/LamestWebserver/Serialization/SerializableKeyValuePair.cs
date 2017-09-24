using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace LamestWebserver.Serialization
{
    /// <summary>
    /// A serializable alternative to KeyValuePair.
    /// </summary>
    /// <typeparam name="TKey">the Type of the Key</typeparam>
    /// <typeparam name="TValue">the Type of the Value</typeparam>
    [Serializable]
    public struct SerializableKeyValuePair<TKey, TValue> : ISerializable, IXmlSerializable
    {
        /// <summary>
        /// The Key
        /// </summary>
        public TKey Key { get; set; }

        /// <summary>
        /// The Value
        /// </summary>
        public TValue Value { get; set; }

        /// <summary>
        /// Constructs a new SerializableKeyValuePair
        /// </summary>
        /// <param name="key">the Key</param>
        /// <param name="value">the Value</param>
        public SerializableKeyValuePair(TKey key, TValue value)
        {
            Key = key;
            Value = value;
        }
        
        /// <summary>
        /// Deserialization constructor.
        /// </summary>
        public SerializableKeyValuePair(SerializationInfo info, StreamingContext context)
        {
            Key = (TKey)info.GetValue(nameof(Key), typeof(TKey));
            Value = (TValue)info.GetValue(nameof(Value), typeof(TValue));
        }

        /// <summary>
        /// Casts a KeyValuePair to a SerializableKeyValuePair
        /// </summary>
        /// <param name="input">the KeyValuePair</param>
        /// <returns>the Entry</returns>
        public static implicit operator SerializableKeyValuePair<TKey, TValue> (KeyValuePair<TKey, TValue> input)
        {
            return new SerializableKeyValuePair<TKey, TValue>(input.Key, input.Value);
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
