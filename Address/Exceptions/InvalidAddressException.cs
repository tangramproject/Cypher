using System;
using System.Runtime.Serialization;

namespace Tangram.Address.Exceptions
{
    public class InvalidAddressException : TangramException
    {
        public InvalidAddressException()
        {
        }

        public InvalidAddressException(string message)
            : base(message)
        {
        }

        public InvalidAddressException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected InvalidAddressException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            ResourceReferenceProperty = info.GetString("ResourceReferenceProperty");
        }
    }
}
