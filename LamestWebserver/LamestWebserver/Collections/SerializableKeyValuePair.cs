using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using LamestWebserver.Serialization;

namespace LamestWebserver.Collections
{
    [Serializable]
    public struct SerializableKeyValuePair<TKey, TValue> : ISerializable, IXmlSerializable
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

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            Key = reader.ReadElement<TKey>(nameof(Key));
            Value = reader.ReadElement<TValue>(nameof(Value));
            reader.Read();
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement("SerializableKeyValuePair");
            writer.WriteElement(nameof(Key), Key);
            writer.WriteElement(nameof(Value), Value);
            writer.WriteEndElement();
        }
    }
}
