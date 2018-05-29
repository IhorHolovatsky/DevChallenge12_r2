using System;
using System.Runtime.Serialization;

namespace CssOptimizer.Domain.Exceptions
{
    public class NoAvailableInstacesException : Exception
    {
        public NoAvailableInstacesException()
        {

        }

        public NoAvailableInstacesException(string message) : base(message)
        {
        }

        public NoAvailableInstacesException(string message, Exception ex) : base(message, ex)
        {
        }

        protected NoAvailableInstacesException(SerializationInfo serializationInfo, StreamingContext streamingContext) : base(serializationInfo, streamingContext)
        {
        }
    }
}