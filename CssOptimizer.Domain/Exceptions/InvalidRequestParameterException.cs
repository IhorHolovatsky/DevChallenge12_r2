using System;
using System.Runtime.Serialization;

namespace CssOptimizer.Domain.Exceptions
{
    public class InvalidRequestParameterException : Exception
    {
        public InvalidRequestParameterException()
        {

        }

        public InvalidRequestParameterException(string message) : base(message)
        {
        }

        public InvalidRequestParameterException(string message, Exception ex) : base(message, ex)
        {
        }

        protected InvalidRequestParameterException(SerializationInfo serializationInfo, StreamingContext streamingContext) : base(serializationInfo, streamingContext)
        {
        }
    }
}