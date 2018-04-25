using System;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using System.Xml;
using System.Xml.Schema;
using LamestWebserver.Serialization;
using LamestWebserver.Core;

namespace LamestWebserver.WebServices
{
    /// <summary>
    /// A response from a WebService retrieving the result of a method call.
    /// </summary>
    [Serializable]
    public class WebServiceResponse : NullCheckable, ISerializable, IXmlSerializable
    {
        /// <summary>
        /// The return type of the method call.
        /// </summary>
        public EWebServiceReturnType ReturnType = EWebServiceReturnType.ReturnVoid;

        /// <summary>
        /// The type of the returned value (if any).
        /// </summary>
        public string ReturnValueType;

        /// <summary>
        /// The returned value (if any).
        /// </summary>
        public object ReturnValue = null;
        
        /// <summary>
        /// The thrown exception (if any).
        /// </summary>
        public Exception ExceptionThrown = null;

        /// <summary>
        /// The thrown exception as string (if any).
        /// </summary>
        public string StringifiedException;

        private Type returnValueType;

        /// <summary>
        /// Deserialization constructor.
        /// </summary>
        public WebServiceResponse() { }

        /// <summary>
        /// Deserialization constructor.
        /// </summary>
        /// <param name="info">SerializationInfo.</param>
        /// <param name="context">StreamingContext.</param>
        public WebServiceResponse(SerializationInfo info, StreamingContext context)
        {
            ReturnType = (EWebServiceReturnType)info.GetValue(nameof(ReturnType), typeof(EWebServiceReturnType));

            switch (ReturnType)
            {
                case EWebServiceReturnType.ReturnVoid:
                    break;

                case EWebServiceReturnType.ExceptionThrown:
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

                case EWebServiceReturnType.ReturnValue:
                    ReturnValueType = info.GetString(nameof(ReturnValueType));
                    returnValueType = Type.GetType(ReturnValueType);
                    ReturnValue = info.GetValue(nameof(ReturnValue), returnValueType);
                    break;

                default:
                    throw new NotImplementedException($"Unhandled case: {nameof(ReturnType)} is '{ReturnType}'.");
            }
        }

        /// <summary>
        /// Builds a new WebServiceResponse for a method that returned `void`.
        /// </summary>
        /// <returns>The corresponding WebServiceResponse.</returns>
        public static WebServiceResponse Return() => new WebServiceResponse();

        /// <summary>
        /// Builds a new WebServiceResponse for a method that returned a value.
        /// </summary>
        /// <returns>The corresponding WebServiceResponse.</returns>
        public static WebServiceResponse Return<T>(T value)
            => new WebServiceResponse()
            {
                returnValueType = typeof(T),
                ReturnValueType = typeof(T).FullName,
                ReturnValue = value,
                ReturnType = EWebServiceReturnType.ReturnValue
            };

        /// <summary>
        /// Builds a new WebServiceResponse for a method that threw an exception.
        /// </summary>
        /// <returns>The corresponding WebServiceResponse.</returns>
        public static WebServiceResponse Exception(Exception exception)
            => new WebServiceResponse()
            {
                ReturnValueType = exception.GetType().FullName,
                ExceptionThrown = exception,
                StringifiedException = exception.SafeToString(),
                ReturnType = EWebServiceReturnType.ExceptionThrown
            };

        /// <inheritdoc />
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(ReturnType), ReturnType);

            switch(ReturnType)
            {
                case EWebServiceReturnType.ReturnVoid:
                    break;

                case EWebServiceReturnType.ExceptionThrown:
                    info.AddValue(nameof(ReturnValueType), ReturnValueType);
                    info.AddValue(nameof(StringifiedException), StringifiedException);

                    try
                    {
                        info.AddValue(nameof(ExceptionThrown), ExceptionThrown);
                    }
                    catch { }

                    break;

                case EWebServiceReturnType.ReturnValue:
                    info.AddValue(nameof(ReturnValueType), ReturnValueType);
                    info.AddValue(nameof(ReturnValue), ReturnValue);
                    break;

                default:
                    throw new NotImplementedException($"Unhandled case: {nameof(ReturnType)} is '{ReturnType}'.");
            }

            info.AddValue(nameof(ReturnValueType), ReturnValueType);
            info.AddValue(nameof(ReturnValueType), ReturnValueType);
            info.AddValue(nameof(ReturnValueType), ReturnValueType);
        }

        /// <inheritdoc />
        public XmlSchema GetSchema() => null;

        /// <inheritdoc />
        public void ReadXml(XmlReader reader)
        {
            reader.ReadStartElement();
            reader.ReadStartElement(nameof(WebServiceResponse));

            reader.ReadStartElement(nameof(ReturnType));
            ReturnType = reader.ReadElement<EWebServiceReturnType>();
            reader.ReadToEndElement(nameof(ReturnType));

            switch(ReturnType)
            {
                case EWebServiceReturnType.ReturnVoid:
                    break;

                case EWebServiceReturnType.ExceptionThrown:
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

                case EWebServiceReturnType.ReturnValue:
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
            }

            reader.ReadToEndElement(nameof(WebServiceResponse));
            reader.ReadEndElement();
        }

        /// <inheritdoc />
        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement(nameof(WebServiceResponse));

            writer.WriteElement(nameof(ReturnType), ReturnType);

            switch (ReturnType)
            {
                case EWebServiceReturnType.ReturnVoid:
                    break;

                case EWebServiceReturnType.ExceptionThrown:
                    writer.WriteElement(nameof(ReturnValueType), ReturnValueType);
                    writer.WriteElement(nameof(StringifiedException), StringifiedException);

                    try
                    {
                        writer.WriteElement(nameof(ExceptionThrown), ExceptionThrown);
                    }
                    catch { }

                    break;

                case EWebServiceReturnType.ReturnValue:
                    writer.WriteElement(nameof(ReturnValueType), ReturnValueType);
                    writer.WriteElement(nameof(ReturnValue), ReturnValue);
                    break;

                default:
                    throw new NotImplementedException($"Unhandled case: {nameof(ReturnType)} is '{ReturnType}'.");
            }

            writer.WriteEndElement();
        }
    }

    /// <summary>
    /// The return type of a WebServiceResponse.
    /// </summary>
    public enum EWebServiceReturnType : byte
    {
        /// <summary>
        /// The method returned `void`.
        /// </summary>
        ReturnVoid,

        /// <summary>
        /// The method returned a value.
        /// </summary>
        ReturnValue,

        /// <summary>
        /// The method threw an exception.
        /// </summary>
        ExceptionThrown
    }
}
