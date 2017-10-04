using LamestWebserver.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using System.Xml;
using System.Xml.Schema;
using LamestWebserver.Serialization;

namespace LamestWebserver.Synchronization
{
    /// <summary>
    /// Provides synchronized access to an IDictionary&lt;TKey, TValue&gt;.
    /// </summary>
    /// <typeparam name="TKey">The type of the stored Keys.</typeparam>
    /// <typeparam name="TValue">The type of the stored Values.</typeparam>
    /// <typeparam name="TCollectionType">The internal implementation of the Dictionary used.</typeparam>
    [Serializable]
    public class SynchronizedDictionary<TKey, TValue, TCollectionType> : NullCheckable, IDictionary<TKey, TValue>, ISerializable, IXmlSerializable where TCollectionType : IDictionary<TKey, TValue>, new()
    {
        /// <summary>
        /// The internal Dictionary for unsynchronized access.
        /// </summary>
        public TCollectionType InnerDictionary { get; protected set; }

        private UsableWriteLock writeLock = new UsableWriteLock();

        /// <summary>
        /// Constructs a new SynchronizedDictionary object and initializes the InnerDictionary with it's default constructor.
        /// </summary>
        public SynchronizedDictionary()
        {
            InnerDictionary = new TCollectionType();
        }

        /// <summary>
        /// Constructs a new SynchronizedDictionary object and initializes the InnerDictionary.
        /// </summary>
        /// <param name="dictionary">The value to initialize the InnerDictionary with.</param>
        public SynchronizedDictionary(TCollectionType dictionary)
        {
            InnerDictionary = dictionary;
        }

        /// <summary>
        /// A Deserialization constructor.
        /// </summary>
        /// <param name="info">The serialization info.</param>
        /// <param name="context">The streaming context.</param>
        public SynchronizedDictionary(SerializationInfo info, StreamingContext context)
        {
            if (typeof(TCollectionType).GetInterfaces().Contains(typeof(ISerializable)))
            {
                InnerDictionary = (TCollectionType)info.GetValue(nameof(InnerDictionary), typeof(TCollectionType));
            }
            else
            {
                InnerDictionary = new TCollectionType();

                SerializableKeyValuePair<TKey, TValue>[] elements;
                elements = (SerializableKeyValuePair<TKey, TValue>[])info.GetValue(nameof(elements), typeof(SerializableKeyValuePair<TKey, TValue>[]));

                foreach (var e in elements)
                    this[e.Key] = e.Value;
            }
        }

        /// <summary>
        /// Provides functionality like NullCheckable.
        /// </summary>
        /// <param name="obj">The current object.</param>
        public static implicit operator bool(SynchronizedDictionary<TKey, TValue, TCollectionType> obj) => obj == null || obj.InnerDictionary == null;

        /// <inheritdoc />
        public TValue this[TKey key]
        {
            get
            {
                using (writeLock.LockRead())
                    return InnerDictionary[key];
            }

            set
            {
                using (writeLock.LockWrite())
                    InnerDictionary[key] = value;
            }
        }

        /// <inheritdoc />
        public int Count
        {
            get
            {
                using (writeLock.LockRead())
                    return InnerDictionary.Count;
            }
        }

        /// <inheritdoc />
        public bool IsReadOnly
        {
            get
            {
                using (writeLock.LockRead())
                    return InnerDictionary.IsReadOnly;
            }
        }

        /// <inheritdoc />
        public ICollection<TKey> Keys
        {
            get
            {
                using (writeLock.LockRead())
                    return InnerDictionary.Keys;
            }
        }

        /// <inheritdoc />
        public ICollection<TValue> Values
        {
            get
            {
                using (writeLock.LockRead())
                    return InnerDictionary.Values;
            }
        }

        /// <inheritdoc />
        public void Add(KeyValuePair<TKey, TValue> item)
        {
            using (writeLock.LockWrite())
                InnerDictionary.Add(item);
        }

        /// <inheritdoc />
        public void Add(TKey key, TValue value)
        {
            using (writeLock.LockWrite())
                InnerDictionary.Add(key, value);
        }

        /// <inheritdoc />
        public void Clear()
        {
            using (writeLock.LockWrite())
                InnerDictionary.Clear();
        }

        /// <inheritdoc />
        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            using (writeLock.LockRead())
                return InnerDictionary.Contains(item);
        }

        /// <inheritdoc />
        public bool ContainsKey(TKey key)
        {
            using (writeLock.LockRead())
                return InnerDictionary.ContainsKey(key);
        }

        /// <inheritdoc />
        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            using (writeLock.LockRead())
                InnerDictionary.CopyTo(array, arrayIndex);
        }

        /// <inheritdoc />
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            using (writeLock.LockRead())
                return InnerDictionary.GetEnumerator();
        }

        /// <inheritdoc />
        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            using (writeLock.LockWrite())
                return InnerDictionary.Remove(item);
        }

        /// <inheritdoc />
        public bool Remove(TKey key)
        {
            using (writeLock.LockWrite())
                return InnerDictionary.Remove(key);
        }

        /// <inheritdoc />
        public bool TryGetValue(TKey key, out TValue value)
        {
            using (writeLock.LockRead())
                return InnerDictionary.TryGetValue(key, out value);
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            using (writeLock.LockRead())
                return InnerDictionary.GetEnumerator();
        }

        /// <inheritdoc />
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            using (writeLock.LockRead())
            {
                if (InnerDictionary is ISerializable)
                {
                    info.AddValue(nameof(InnerDictionary), InnerDictionary);
                }
                else
                {
                    SerializableKeyValuePair<TKey, TValue>[] elements = new SerializableKeyValuePair<TKey, TValue>[Count];
                    int index = 0;

                    foreach (var element in this)
                        elements[index++] = element;

                    info.AddValue(nameof(elements), elements);
                }
            }
        }

        /// <inheritdoc />
        public XmlSchema GetSchema()
        {
            if (InnerDictionary is IXmlSerializable)
                return (InnerDictionary as IXmlSerializable).GetSchema();
            else
                return null;
        }

        /// <inheritdoc />
        public void ReadXml(XmlReader reader)
        {
            if (InnerDictionary is IXmlSerializable)
            {
                (InnerDictionary as IXmlSerializable).ReadXml(reader);
            }
            else
            {
                reader.ReadStartElement();
                reader.ReadStartElement();

                List<SerializableKeyValuePair<TKey, TValue>> elements = reader.ReadElement<List<SerializableKeyValuePair<TKey, TValue>>>();

                foreach (SerializableKeyValuePair<TKey, TValue> e in elements)
                    this[e.Key] = e.Value;

                reader.ReadToEndElement("AVLHashMap");
                reader.ReadEndElement();
            }
        }

        /// <inheritdoc />
        public void WriteXml(XmlWriter writer)
        {
            if (InnerDictionary is IXmlSerializable)
            {
                (InnerDictionary as IXmlSerializable).WriteXml(writer);
            }
            else
            {
                writer.WriteStartElement("SynchronizedDictionary");

                SerializableKeyValuePair<TKey, TValue>[] elements = new SerializableKeyValuePair<TKey, TValue>[Count];
                int index = 0;

                foreach (var element in this)
                {
                    elements[index++] = element;
                }

                writer.WriteElement("Elements", elements);
                writer.WriteEndElement();
            }
        }
    }
}
