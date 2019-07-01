using System;
using System.Runtime.Serialization;

namespace Tangram.Address.Exceptions
{
    public class InvalidChecksumException : InvalidAddressException
    {
        public InvalidChecksumException()
        {
        }

        public InvalidChecksumException(string message)
            : base(message)
        {
        }

        public InvalidChecksumException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected InvalidChecksumException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            ResourceReferenceProperty = info.GetString("ResourceReferenceProperty");
        }
    }
}
