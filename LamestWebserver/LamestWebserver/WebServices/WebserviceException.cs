using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace LamestWebserver.WebServices
{
    public abstract class WebServiceException : Exception
    {
        /// <inheritdoc />
        public WebServiceException() : base($"A {nameof(WebServiceException)} occured.") { }

        /// <inheritdoc />
        public WebServiceException(string message) : base(message) { }

        /// <inheritdoc />
        public WebServiceException(string message, Exception innerException) : base(message, innerException) { }

        /// <inheritdoc />
        protected WebServiceException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// An Exception for failed remote operations.
    /// </summary>
    public class RemoteException : Exception
    {
        /// <inheritdoc />
        public RemoteException() : base("An exception occured in the remote machine.") { }

        /// <inheritdoc />
        public RemoteException(string message) : base(message) { }

        /// <inheritdoc />
        public RemoteException(string message, Exception innerException) : base(message, innerException) { }

        /// <inheritdoc />
        protected RemoteException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    public class ServiceNotAvailableException : WebServiceException
    {
        /// <inheritdoc />
        public ServiceNotAvailableException() { }

        /// <inheritdoc />
        public ServiceNotAvailableException(string message) : base(message) { }

        /// <inheritdoc />
        public ServiceNotAvailableException(string message, Exception innerException) : base(message, innerException) { }

        /// <inheritdoc />
        protected ServiceNotAvailableException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    public class IncompatibleTypeException : WebServiceException
    {
        /// <inheritdoc />
        public IncompatibleTypeException() { }

        /// <inheritdoc />
        public IncompatibleTypeException(string message) : base(message) { }

        /// <inheritdoc />
        public IncompatibleTypeException(string message, Exception innerException) : base(message, innerException) { }

        /// <inheritdoc />
        protected IncompatibleTypeException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    public class WebServiceIncompatibleException : WebServiceException
    {
        /// <inheritdoc />
        public WebServiceIncompatibleException() { }

        /// <inheritdoc />
        public WebServiceIncompatibleException(string message) : base(message) { }

        /// <inheritdoc />
        public WebServiceIncompatibleException(string message, Exception innerException) : base(message, innerException) { }

        /// <inheritdoc />
        protected WebServiceIncompatibleException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
