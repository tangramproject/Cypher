using System;
using System.Runtime.Serialization;

namespace Tangram.Address.Exceptions
{
    public class UnknownAddressVersionException : TangramException
    {
        public UnknownAddressVersionException()
        {
        }

        public UnknownAddressVersionException(string message)
            : base(message)
        {
        }

        public UnknownAddressVersionException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected UnknownAddressVersionException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            ResourceReferenceProperty = info.GetString("ResourceReferenceProperty");
        }
    }
}
