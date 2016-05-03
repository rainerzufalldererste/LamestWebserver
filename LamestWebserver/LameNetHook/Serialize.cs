using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace LameNetHook
{
    public static class Serialize
    {
        public static T getData<T>(string filename)
        {
            string xmlString = File.ReadAllText(filename);

            using (MemoryStream memStream = new MemoryStream(Encoding.Unicode.GetBytes(xmlString)))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(T));
                return (T)serializer.Deserialize(memStream);
            }
        }

        public static void writeData<T>(T data, string filename)
        {
            StringBuilder output = new StringBuilder();

            using (StringWriter textWriter = new StringWriter(output))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(T));
                serializer.Serialize(textWriter, data);
            }

            System.IO.File.WriteAllText(filename, output.ToString());
        }
    }
}
