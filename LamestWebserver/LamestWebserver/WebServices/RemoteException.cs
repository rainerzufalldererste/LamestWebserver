using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace LamestWebserver.WebServices
{
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
}
