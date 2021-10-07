using System.Runtime.Serialization;

namespace KScr.Lib.Exception
{
    public class CompilerException : System.Exception
    {
        public CompilerException()
        {
        }

        protected CompilerException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public CompilerException(string? message) : base(message)
        {
        }

        public CompilerException(string? message, System.Exception? innerException) : base(message, innerException)
        {
        }
    }
}