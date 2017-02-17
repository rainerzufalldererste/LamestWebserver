using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LamestWebserver.Serialization
{
    public static class Serializer
    {
        /// <summary>
        /// Retrieves XML-Serialized data from a file.
        /// </summary>
        /// <typeparam name="T">The Type of the data to deserialize</typeparam>
        /// <param name="filename">The name of the file</param>
        /// <returns>The deserialized object</returns>
        public static T ReadXmlData<T>(string filename)
        {
            string xmlString = File.ReadAllText(filename);

            using (MemoryStream memStream = new MemoryStream(Encoding.Unicode.GetBytes(xmlString))) // Yes, it actually seems like this is all really necessary here :/
            {
                XmlSerializer serializer = XmlSerializationTools.GetXmlSerializer(typeof(T));
                return (T)serializer.Deserialize(memStream);
            }
        }

        /// <summary>
        /// Writes an Object to an XML-File.
        /// </summary>
        /// <typeparam name="T">The Type of the Object</typeparam>
        /// <param name="data">The Object</param>
        /// <param name="filename">The name of the file to write</param>
        public static void WriteXmlData<T>(T data, string filename)
        {
            if(!File.Exists(filename) && !string.IsNullOrWhiteSpace(Path.GetDirectoryName(filename)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filename));
            }

            StringBuilder output = new StringBuilder();

            using (StringWriter textWriter = new StringWriter(output))
            {
                XmlSerializer serializer = XmlSerializationTools.GetXmlSerializer(typeof(T));
                serializer.Serialize(textWriter, data);
            }

            File.WriteAllText(filename, output.ToString());
        }

        /// <summary>
        /// Retrieves JSON-Serialized data from a file.
        /// </summary>
        /// <typeparam name="T">The Type of the data to deserialize</typeparam>
        /// <param name="filename">The name of the file</param>
        /// <returns>The deserialized object</returns>
        public static T ReadJsonData<T>(string filename) where T : new()
        {
            string jsonString = File.ReadAllText(filename);

            return (T) JsonConvert.DeserializeObject(jsonString, typeof(T));
        }

        /// <summary>
        /// Writes an Object to an JSON-File.
        /// </summary>
        /// <typeparam name="T">The Type of the Object</typeparam>
        /// <param name="data">The Object</param>
        /// <param name="filename">The name of the file to write</param>
        public static void WriteJsonData<T>(T data, string filename)
        {
            if (!File.Exists(filename) && !string.IsNullOrWhiteSpace(Path.GetDirectoryName(filename)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filename));
            }

            File.WriteAllText(filename, JsonConvert.SerializeObject(data));
        }

        /// <summary>
        /// Retrieves Binary-Serialized data from a file.
        /// </summary>
        /// <typeparam name="T">The Type of the data to deserialize</typeparam>
        /// <param name="filename">The name of the file</param>
        /// <returns>The deserialized object</returns>
        public static T ReadBinaryData<T>(string filename)
        {
            byte[] binaryData = File.ReadAllBytes(filename);

            using (MemoryStream memStream = new MemoryStream(binaryData))
            {
                BinaryFormatter formatter = new BinaryFormatter();

                return (T)formatter.Deserialize(memStream);
            }
        }

        /// <summary>
        /// Writes an Object to a Binary-File.
        /// </summary>
        /// <typeparam name="T">The Type of the Object</typeparam>
        /// <param name="data">The Object</param>
        /// <param name="filename">The name of the file to write</param>
        public static void WriteBinaryData<T>(T data, string filename)
        {
            if (!File.Exists(filename) && !string.IsNullOrWhiteSpace(Path.GetDirectoryName(filename)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filename));
            }

            using (FileStream fs = new FileStream(filename, FileMode.Create))
            {
                BinaryFormatter formatter = new BinaryFormatter();

                formatter.Serialize(fs, data);
            }
        }
    }

    public static class XmlSerializationTools
    {
        private static readonly Hashtable _cachedXmlSerialiazers = new Hashtable();

        /// <summary>
        /// Caches XMLSerializers to prevent MemoryLeaks.
        /// 
        /// Source: http://codereview.stackexchange.com/questions/24861/caching-xmlserializer-in-appdomain & https://msdn.microsoft.com/en-us/library/system.xml.serialization.xmlserializer(v=vs.110).aspx
        /// </summary>
        /// <param name="type">type parameter of the Serializer</param>
        /// <returns>An XML-Serializer created with the given type argument</returns>
        public static XmlSerializer GetXmlSerializer(Type type)
        {
            var key = type;

            var serializer = _cachedXmlSerialiazers[key] as XmlSerializer;

            if (serializer == null)
            {
                serializer = new XmlSerializer(type);
                _cachedXmlSerialiazers[key] = serializer;
            }

            return serializer;
        }

        /// <summary>
        /// Source: http://stackoverflow.com/questions/2441673/reading-xml-with-xmlreader-in-c-sharp
        /// </summary>
        /// <param name="reader">the XMLReader</param>
        /// <param name="elementName">the name of the Element</param>
        /// <returns></returns>
        public static IEnumerable<XElement> GetElementsNamed(this XmlReader reader, string elementName)
        {
            reader.MoveToContent(); // will not advance reader if already on a content node; if successful, ReadState is Interactive
            reader.Read();          // this is needed, even with MoveToContent and ReadState.Interactive
            while (!reader.EOF && reader.ReadState == ReadState.Interactive)
            {
                // corrected for bug noted by Wes below...
                if (reader.NodeType == XmlNodeType.Element && reader.Name.Equals(elementName))
                {
                    // this advances the reader...so it's either XNode.ReadFrom() or reader.Read(), but not both
                    var matchedElement = XNode.ReadFrom(reader) as XElement;
                    if (matchedElement != null)
                        yield return matchedElement;
                }
                else
                    reader.Read();
            }
        }

        /// <summary>
        /// Source: http://stackoverflow.com/questions/2441673/reading-xml-with-xmlreader-in-c-sharp
        /// </summary>
        /// <param name="reader">the XMLReader</param>
        /// <param name="elementName">the name of the Element</param>
        /// <returns></returns>
        public static XElement GetElementNamed(this XmlReader reader, string elementName)
        {
            reader.MoveToContent(); // will not advance reader if already on a content node; if successful, ReadState is Interactive
            reader.Read();          // this is needed, even with MoveToContent and ReadState.Interactive
            while (!reader.EOF && reader.ReadState == ReadState.Interactive)
            {
                // corrected for bug noted by Wes below...
                if (reader.NodeType == XmlNodeType.Element && reader.Name.Equals(elementName))
                {
                    // this advances the reader...so it's either XNode.ReadFrom() or reader.Read(), but not both
                    var matchedElement = XNode.ReadFrom(reader) as XElement;
                    if (matchedElement != null)
                        return matchedElement;
                }
                else
                    reader.Read();
            }

            return null;
        }

        /// <summary>
        /// Writes an object to the xmlWriter
        /// </summary>
        /// <typeparam name="T">The type of the element</typeparam>
        /// <param name="writer">the current writer</param>
        /// <param name="name">the name of the object</param>
        /// <param name="value">the value of the object</param>
        public static void WriteElement<T>(this XmlWriter writer, string name, T value)
        {
            var serializer = new XmlSerializer(typeof(T), new XmlRootAttribute(name));
            serializer.Serialize(writer, value);
        }

        /// <summary>
        /// searches and reads an object from a xmlReader
        /// </summary>
        /// <typeparam name="T">The type of the element</typeparam>
        /// <param name="reader">the current reader</param>
        /// <param name="name">the name of the object</param>
        public static T ReadElement<T>(this XmlReader reader, string name = null)
        {
            while (name != null && reader.Name != name)
            {
                if (!reader.Read())
                    return default(T);
            }

            if (reader.GetAttribute("xsi:nil") == "true")
            {
                return default(T);
            }

            return ReadLowerElement<T>(reader);
        }

        /// <summary>
        /// reads an object from a xmlReader
        /// </summary>
        /// <typeparam name="T">The type of the element</typeparam>
        /// <param name="reader">the current reader</param>
        public static T ReadLowerElement<T>(this XmlReader reader)
        {
            if (reader.NodeType == XmlNodeType.Element)
            {
                XmlSerializer serializer;

                serializer = new XmlSerializer(typeof(T), new XmlRootAttribute(reader.Name));

                T ret = (T)serializer.Deserialize(reader);

                return ret;
            }
            else
            {
                reader.Read();

                T ret = (T)reader.ReadContentAs(typeof(T), null);

                reader.Read();

                return ret;
            }
        }

        /// <summary>
        /// Reads an XmlReader to a specified EndElement
        /// </summary>
        /// <param name="reader">the XmlReader</param>
        /// <param name="endElement">the name of the EndElement tag</param>
        public static void ReadToEndElement(this XmlReader reader, string endElement)
        {
            if (reader.Name != null && reader.NodeType == XmlNodeType.EndElement && reader.Name == endElement)
            {
                reader.ReadEndElement();
                return;
            }

            reader.ReadEndElement();
        }
    }
}
