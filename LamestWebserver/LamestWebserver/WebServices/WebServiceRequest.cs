using System;
using System.Linq;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using System.Xml;
using System.Xml.Schema;
using LamestWebserver.Serialization;
using LamestWebserver.Core;

namespace LamestWebserver.WebServices
{
    /// <summary>
    /// A request to a WebService.
    /// </summary>
    [Serializable]
    public class WebServiceRequest : NullCheckable, ISerializable, IXmlSerializable
    {
        private WebServiceHandler _webServiceHandler = null;

        internal WebServiceHandler WebServiceHandler
        {
            get
            {
                return _webServiceHandler;
            }

            set
            {
                if (_webServiceHandler)
                    throw new InvalidOperationException("The WebServiceHandler of this WebServiceRequest can't be set multiple times.");

                if (!value)
                    throw new NullReferenceException(nameof(value));

                _webServiceHandler = value;
            }
        }

        internal bool IsRemoteRequest = false;

        /// <summary>
        /// The namespace of the requested type.
        /// </summary>
        public string Namespace;

        /// <summary>
        /// The name of the requested type.
        /// </summary>
        public string Type;

        /// <summary>
        /// The name of the method that will be requested.
        /// </summary>
        public string Method;

        /// <summary>
        /// The parameters of the method call.
        /// </summary>
        public object[] Parameters;

        /// <summary>
        /// The names of the parameter types of the method definition.
        /// </summary>
        public string[] MethodParameterTypes;

        /// <summary>
        /// The names of the types that were passed as parameters to the method.
        /// </summary>
        public string[] ParameterTypes;
        
        internal Type[] _methodParameterTypes;
        internal Type[] _parameterTypes;

        /// <summary>
        /// Deserialization constructor.
        /// </summary>
        public WebServiceRequest() { }

        /// <summary>
        /// Deserialization constructor.
        /// </summary>
        /// <param name="info">SerializationInfo.</param>
        /// <param name="context">StreamingContext.</param>
        public WebServiceRequest(SerializationInfo info, StreamingContext context)
        {
            Namespace = info.GetString(nameof(Namespace));
            Type = info.GetString(nameof(Type));
            Method = info.GetString(nameof(Method));
            Parameters = (object[])info.GetValue(nameof(Parameters), typeof(object[]));
            MethodParameterTypes = (string[])info.GetValue(nameof(MethodParameterTypes), typeof(string[]));
            ParameterTypes = (string[])info.GetValue(nameof(ParameterTypes), typeof(string[]));

            _methodParameterTypes = (from p in MethodParameterTypes select System.Type.GetType(p)).ToArray();
            _parameterTypes = (from p in ParameterTypes select System.Type.GetType(p)).ToArray();
        }

        /// <summary>
        /// Builds a request to a specified method of a specified type using the given parameters.
        /// </summary>
        /// <typeparam name="T">The Type of the method to call.</typeparam>
        /// <param name="method">The name of the method to call.</param>
        /// <param name="methodParameterTypes">The types of the method definition.</param>
        /// <param name="parameters">The parameters to pass to the method.</param>
        /// <returns>Returns a WebServiceRequest containing the given specification.</returns>
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

        /// <inheritdoc />
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(Namespace), Namespace);
            info.AddValue(nameof(Type), Type);
            info.AddValue(nameof(Method), Method);
            info.AddValue(nameof(Parameters), Parameters);
            info.AddValue(nameof(MethodParameterTypes), MethodParameterTypes);
            info.AddValue(nameof(ParameterTypes), ParameterTypes);
        }

        /// <inheritdoc />
        public XmlSchema GetSchema() => null;

        /// <inheritdoc />
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

        /// <inheritdoc />
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
