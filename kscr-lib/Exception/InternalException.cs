using System.Runtime.Serialization;

namespace KScr.Lib.Exception
{
    public class InternalException : System.Exception
    {
        public InternalException()
        {
        }

        protected InternalException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public InternalException(string? message) : base(message)
        {
        }

        public InternalException(string? message, System.Exception? innerException) : base(message, innerException)
        {
        }
    }
}