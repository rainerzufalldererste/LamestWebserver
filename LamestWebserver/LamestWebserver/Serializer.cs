using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace LamestWebserver
{
    public static class Serializer
    {
        /// <summary>
        /// Caches XMLSerializers to prevent MemoryLeaks.
        /// 
        /// Source: http://codereview.stackexchange.com/questions/24861/caching-xmlserializer-in-appdomain
        /// </summary>
        /// <param name="type">type parameter of the Serializer</param>
        /// <returns></returns>
        public static XmlSerializer GetXmlSerializer(Type type)
        {
            var cache = AppDomain.CurrentDomain;
            var key = string.Format(CultureInfo.InvariantCulture, "CachedXmlSerializer:{0}", type);

            var serializer = cache.GetData(key) as XmlSerializer;

            if (serializer == null)
            {
                serializer = new XmlSerializer(type);
                cache.SetData(key, serializer);
            }

            return serializer;
        }

        /// <summary>
        /// Retrieves XML-Serialized data from a file.
        /// </summary>
        /// <typeparam name="T">The Type of the data to deserialize</typeparam>
        /// <param name="filename">The name of the file</param>
        /// <returns>The deserialized object</returns>
        public static T getData<T>(string filename)
        {
            string xmlString = File.ReadAllText(filename);

            using (MemoryStream memStream = new MemoryStream(Encoding.Unicode.GetBytes(xmlString))) // Yes, it actually seems like this is all really necessary here :/
            {
                XmlSerializer serializer = GetXmlSerializer(typeof(T));
                return (T)serializer.Deserialize(memStream);
            }
        }

        /// <summary>
        /// Writes an Object to an XML-File.
        /// </summary>
        /// <typeparam name="T">The Type of the Object</typeparam>
        /// <param name="data">The Object</param>
        /// <param name="filename">The name of the file to write</param>
        public static void writeData<T>(T data, string filename)
        {
            if(!File.Exists(filename) && !string.IsNullOrWhiteSpace(Path.GetDirectoryName(filename)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filename));
            }

            StringBuilder output = new StringBuilder();

            using (StringWriter textWriter = new StringWriter(output))
            {
                XmlSerializer serializer = GetXmlSerializer(typeof(T));
                serializer.Serialize(textWriter, data);
            }

            File.WriteAllText(filename, output.ToString());
        }

        /// <summary>
        /// Retrieves Binary-Serialized data from a file.
        /// </summary>
        /// <typeparam name="T">The Type of the data to deserialize</typeparam>
        /// <param name="filename">The name of the file</param>
        /// <returns>The deserialized object</returns>
        public static T getBinaryData<T>(string filename)
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
        public static void writeBinaryData<T>(T data, string filename)
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
                /*
                Console.Write("Searching for '" + name + "' at " + reader.Name + " (" + reader.NodeType + ") ");

                for (int attInd = 0; attInd < reader.AttributeCount; attInd++)
                {
                    reader.MoveToAttribute(attInd);
                    Console.Write("(" + reader.Name + " | ");
                    Console.Write(reader.Value + "),");
                }

                Console.WriteLine();

                reader.MoveToElement();*/
                
                if (!reader.Read())
                    return default(T);
            }

            /*
            Console.Write("Found '" + name + "' at " + reader.Name + " (" + reader.NodeType + ") ");

            for (int attInd = 0; attInd < reader.AttributeCount; attInd++)
            {
                reader.MoveToAttribute(attInd);
                Console.Write("(" + reader.Name + " | ");
                Console.Write(reader.Value + "),");
            }

            Console.WriteLine();

            reader.MoveToElement();*/

            if (reader.GetAttribute("xsi:nil") == "true")
            {
                // Console.WriteLine("Was NULL.");
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

        public static void ReadToEndElement(this XmlReader reader, string endElement)
        {
            if (reader.Name != null && reader.NodeType == XmlNodeType.EndElement && reader.Name == endElement)
            {
                reader.ReadEndElement();
                return;
            }

            while (reader.Read() && !(reader.NodeType == XmlNodeType.EndElement && reader.Name == endElement))
                /*Console.WriteLine("Searching for EndElement '" + endElement + "' only found '" + reader.Name + "' (" + reader.NodeType + ")")*/;

            reader.ReadEndElement();
        }

        /*

        public static void WriteElementToList<T>(this XmlWriter writer, string name, T value, List<SerializableKeyValuePair<string, object>> list)
        {
            list.Add(new SerializableKeyValuePair<string, object>(name, value));
        }

        public static void WriteElementList(this XmlWriter writer, List<SerializableKeyValuePair<string, object>> list)
        {
            WriteElement(writer, "list", list);
        }

        public static T GetElementFromList<T>(this XmlReader reader, string name)
        {
            List<SerializableKeyValuePair<string, object>> list = new List<SerializableKeyValuePair<string, object>>();

            list = reader.ReadElement<List<SerializableKeyValuePair<string, object>>>("list");

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Key == name)
                    return (T)list[i].Value;
            }

            return default(T);
        }

        */
    }
}
