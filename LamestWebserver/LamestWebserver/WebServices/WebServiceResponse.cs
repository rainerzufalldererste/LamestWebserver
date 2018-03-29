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
using System.Xml;
using System.Xml.Schema;
using LamestWebserver.Collections;
using LamestWebserver.Synchronization;
using LamestWebserver.WebServices.Generators;
using LamestWebserver.Serialization;

namespace LamestWebserver.WebServices
{
    [Serializable]
    public class WebServiceResponse : ISerializable, IXmlSerializable
    {
        public WebServiceReturnType ReturnType = WebServiceReturnType.ReturnVoid;
        public string ReturnValueType;
        public object ReturnValue = null;
        public Exception ExceptionThrown = null;
        public string StringifiedException;

        private Type returnValueType;

        public WebServiceResponse() { }

        public WebServiceResponse(SerializationInfo info, StreamingContext context)
        {
            ReturnType = (WebServiceReturnType)info.GetValue(nameof(ReturnType), typeof(WebServiceReturnType));

            switch (ReturnType)
            {
                case WebServiceReturnType.ReturnVoid:
                    break;

                case WebServiceReturnType.ExceptionThrown:
                    ReturnValueType = info.GetString(nameof(ReturnValueType));
                    returnValueType = Type.GetType(ReturnValueType);
                    StringifiedException = info.GetString(nameof(StringifiedException));

                    try
                    {
                        ExceptionThrown = (Exception)info.GetValue(nameof(ExceptionThrown), returnValueType);
                    }
                    catch (Exception e)
                    {
                        ExceptionThrown = new SerializationException($"Failed to deserialize Exception of Type '{ReturnValueType}'. Information about the original Exception:\n\"{StringifiedException}\".\nException in when trying to deserialize:\n\"{e.ToString()}\".");
                    }

                    break;

                case WebServiceReturnType.ReturnValue:
                    ReturnValueType = info.GetString(nameof(ReturnValueType));
                    returnValueType = Type.GetType(ReturnValueType);
                    ReturnValue = info.GetValue(nameof(ReturnValue), returnValueType);
                    break;

                default:
                    throw new NotImplementedException($"Unhandled case: {nameof(ReturnType)} is '{ReturnType}'.");
                    break;
            }
        }

        public static WebServiceResponse Return() => new WebServiceResponse();

        public static WebServiceResponse Return<T>(T value)
            => new WebServiceResponse()
            {
                returnValueType = typeof(T),
                ReturnValueType = typeof(T).FullName,
                ReturnValue = value,
                ReturnType = WebServiceReturnType.ReturnValue
            };

        public static WebServiceResponse Exception(Exception exception)
            => new WebServiceResponse()
            {
                ReturnValueType = exception.GetType().FullName,
                ExceptionThrown = exception,
                StringifiedException = exception.ToString(),
                ReturnType = WebServiceReturnType.ExceptionThrown
            };

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(ReturnType), ReturnType);

            switch(ReturnType)
            {
                case WebServiceReturnType.ReturnVoid:
                    break;

                case WebServiceReturnType.ExceptionThrown:
                    info.AddValue(nameof(ReturnValueType), ReturnValueType);
                    info.AddValue(nameof(StringifiedException), StringifiedException);

                    try
                    {
                        info.AddValue(nameof(ExceptionThrown), ExceptionThrown);
                    }
                    catch { }

                    break;

                case WebServiceReturnType.ReturnValue:
                    info.AddValue(nameof(ReturnValueType), ReturnValueType);
                    info.AddValue(nameof(ReturnValue), ReturnValue);
                    break;

                default:
                    throw new NotImplementedException($"Unhandled case: {nameof(ReturnType)} is '{ReturnType}'.");
                    break;
            }

            info.AddValue(nameof(ReturnValueType), ReturnValueType);
            info.AddValue(nameof(ReturnValueType), ReturnValueType);
            info.AddValue(nameof(ReturnValueType), ReturnValueType);
        }

        public XmlSchema GetSchema() => null;

        public void ReadXml(XmlReader reader)
        {
            reader.ReadStartElement();
            reader.ReadStartElement(nameof(WebServiceResponse));

            reader.ReadStartElement(nameof(ReturnType));
            ReturnType = reader.ReadElement<WebServiceReturnType>();
            reader.ReadToEndElement(nameof(ReturnType));

            switch(ReturnType)
            {
                case WebServiceReturnType.ReturnVoid:
                    break;

                case WebServiceReturnType.ExceptionThrown:
                    reader.ReadStartElement(nameof(ReturnValueType));
                    ReturnValueType = reader.ReadElement<string>();
                    returnValueType = Type.GetType(ReturnValueType);
                    reader.ReadToEndElement(nameof(ReturnValueType));

                    reader.ReadStartElement(nameof(StringifiedException));
                    StringifiedException = reader.ReadElement<string>();
                    reader.ReadToEndElement(nameof(StringifiedException));

                    try
                    {
                        reader.ReadStartElement(nameof(ExceptionThrown));
                        ExceptionThrown = (Exception)reader.ReadElement(returnValueType);
                        reader.ReadToEndElement(nameof(ExceptionThrown));
                    }
                    catch (Exception e)
                    {
                        ExceptionThrown = new SerializationException($"Failed to deserialize Exception of Type '{ReturnValueType}'. Information about the original Exception:\n\"{StringifiedException}\".\nException in when trying to deserialize:\n\"{e.ToString()}\".");
                    }

                    break;

                case WebServiceReturnType.ReturnValue:
                    reader.ReadStartElement(nameof(ReturnValueType));
                    ReturnValueType = reader.ReadElement<string>();
                    returnValueType = Type.GetType(ReturnValueType);
                    reader.ReadToEndElement(nameof(ReturnValueType));

                    reader.ReadStartElement(nameof(ReturnValue));
                    ReturnValue = reader.ReadElement(returnValueType);
                    reader.ReadToEndElement(nameof(ReturnValue));
                    break;

                default:
                    throw new NotImplementedException($"Unhandled case: {nameof(ReturnType)} is '{ReturnType}'.");
                    break;
            }

            reader.ReadToEndElement(nameof(WebServiceResponse));
            reader.ReadEndElement();
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement(nameof(WebServiceResponse));

            writer.WriteElement(nameof(ReturnType), ReturnType);

            switch (ReturnType)
            {
                case WebServiceReturnType.ReturnVoid:
                    break;

                case WebServiceReturnType.ExceptionThrown:
                    writer.WriteElement(nameof(ReturnValueType), ReturnValueType);
                    writer.WriteElement(nameof(StringifiedException), StringifiedException);

                    try
                    {
                        writer.WriteElement(nameof(ExceptionThrown), ExceptionThrown);
                    }
                    catch { }

                    break;

                case WebServiceReturnType.ReturnValue:
                    writer.WriteElement(nameof(ReturnValueType), ReturnValueType);
                    writer.WriteElement(nameof(ReturnValue), ReturnValue);
                    break;

                default:
                    throw new NotImplementedException($"Unhandled case: {nameof(ReturnType)} is '{ReturnType}'.");
                    break;
            }

            writer.WriteEndElement();
        }
    }

    public enum WebServiceReturnType : byte
    {
        ReturnVoid, ExceptionThrown, ReturnValue
    }
}
