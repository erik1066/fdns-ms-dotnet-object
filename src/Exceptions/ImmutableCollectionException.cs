using System;
using System.Runtime.Serialization;

namespace Foundation.ObjectService.Exceptions
{
#pragma warning disable 1591 // disables the warnings about missing Xml code comments
    [Serializable]
    public class ImmutableCollectionException : Exception
    {
        public string ExceptionMessage { get; set; }

        protected ImmutableCollectionException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        public ImmutableCollectionException() : base() { }

        public ImmutableCollectionException(string exceptionMessage) : base(exceptionMessage)
        {
            ExceptionMessage = exceptionMessage;
        }

        public ImmutableCollectionException(string exceptionMessage, string message) : base(message)
        {
            ExceptionMessage = exceptionMessage;
        }

        public ImmutableCollectionException(string exceptionMessage, string message, Exception innerException) : base(message, innerException)
        {
            ExceptionMessage = exceptionMessage;
        }
    }
#pragma warning restore 1591
}