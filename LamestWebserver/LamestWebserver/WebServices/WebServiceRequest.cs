using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.CodeDom;
using System.Net;
using System.Reflection;
using System.Xml.Serialization;
using LamestWebserver.Collections;
using LamestWebserver.Synchronization;
using LamestWebserver.WebServices.Generators;
using System.Xml;
using System.Xml.Schema;
using LamestWebserver.Serialization;

namespace LamestWebserver.WebServices
{
    [Serializable]
    public class WebServiceRequest : ISerializable, IXmlSerializable
    {
        public string Namespace;
        public string Type;
        public string Method;
        public object[] Parameters;
        public string[] MethodParameterTypes;
        public string[] ParameterTypes;

        internal Type[] _methodParameterTypes;
        internal Type[] _parameterTypes;

        public WebServiceRequest() { }

        public WebServiceRequest(SerializationInfo info, StreamingContext context)
        {
            Namespace = info.GetString(nameof(Namespace));
            Type = info.GetString(nameof(Type));
            Method = info.GetString(nameof(Method));
            Parameters = (object[])info.GetValue(nameof(Parameters), typeof(object[]));
            MethodParameterTypes = (string[])info.GetValue(nameof(Parameters), typeof(string[]));
            ParameterTypes = (string[])info.GetValue(nameof(Parameters), typeof(string[]));

            _methodParameterTypes = (from p in MethodParameterTypes select System.Type.GetType(p)).ToArray();
            _parameterTypes = (from p in ParameterTypes select System.Type.GetType(p)).ToArray();
        }

        public static WebServiceRequest Request<T>(string method, Type[] methodParameterTypes, params object[] parameters)
            => new WebServiceRequest()
            {
                Method = method,
                Namespace = typeof(T).Namespace,
                Type = typeof(T).Name,
                Parameters = parameters,
                _methodParameterTypes = methodParameterTypes,
                MethodParameterTypes = (from m in methodParameterTypes select m.Namespace + "." + m.Name).ToArray(),
                _parameterTypes = (from p in parameters select p.GetType()).ToArray(),
                ParameterTypes = (from p in parameters select p.GetType().Namespace + "." + p.GetType().Name).ToArray()
            };

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(Namespace), Namespace);
            info.AddValue(nameof(Type), Type);
            info.AddValue(nameof(Method), Method);
            info.AddValue(nameof(Parameters), Parameters);
            info.AddValue(nameof(MethodParameterTypes), MethodParameterTypes);
            info.AddValue(nameof(ParameterTypes), ParameterTypes);
        }

        public XmlSchema GetSchema() => null;

        public void ReadXml(XmlReader reader)
        {
            reader.ReadStartElement();
            reader.ReadStartElement(nameof(WebServiceRequest));

            reader.ReadStartElement(nameof(Namespace));
            Namespace = reader.ReadElement<string>();
            reader.ReadToEndElement(nameof(Namespace));

            reader.ReadStartElement(nameof(Type));
            Type = reader.ReadElement<string>();
            reader.ReadToEndElement(nameof(Type));

            reader.ReadStartElement(nameof(Method));
            Method = reader.ReadElement<string>();
            reader.ReadToEndElement(nameof(Method));

            reader.ReadStartElement(nameof(Parameters));
            Parameters = reader.ReadElement<object[]>();
            reader.ReadToEndElement(nameof(Parameters));

            reader.ReadStartElement(nameof(MethodParameterTypes));
            MethodParameterTypes = reader.ReadElement<string[]>();
            reader.ReadToEndElement(nameof(MethodParameterTypes));

            reader.ReadStartElement(nameof(ParameterTypes));
            ParameterTypes = reader.ReadElement<string[]>();
            reader.ReadToEndElement(nameof(ParameterTypes));

            reader.ReadToEndElement(nameof(WebServiceRequest));
            reader.ReadEndElement();

            // Get Types
            _methodParameterTypes = (from p in MethodParameterTypes select System.Type.GetType(p)).ToArray();
            _parameterTypes = (from p in ParameterTypes select System.Type.GetType(p)).ToArray();
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement(nameof(WebServiceRequest));

            writer.WriteElement(nameof(Namespace), Namespace);
            writer.WriteElement(nameof(Type), Type);
            writer.WriteElement(nameof(Method), Method);
            writer.WriteElement(nameof(Parameters), Parameters);
            writer.WriteElement(nameof(MethodParameterTypes), MethodParameterTypes);
            writer.WriteElement(nameof(ParameterTypes), ParameterTypes);

            writer.WriteEndElement();
        }
    }
}
