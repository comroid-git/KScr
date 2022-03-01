using System.Runtime.Serialization;
using KScr.Lib.Model;

namespace KScr.Lib.Exception
{
    public class InternalException : System.Exception
    {
        public InternalException(string? message) : base(message)
        {
        }

        public InternalException(string? message, System.Exception? innerException) : base(message, innerException)
        {
        }
    }

    public interface IStackTrace
    {
        SourcefilePosition SrcPos { get; }
        System.Exception? InnerTrace { get; }
        string Message { get; }
    }

    public class StackTraceException : System.Exception, IStackTrace
    {
        public SourcefilePosition SrcPos { get; }
        public System.Exception? InnerTrace { get; }
        public string Local { get; }

        public override string Message => $"({SrcPos.SourcefilePath}:{SrcPos.SourcefileLine} {Local}) {base.Message}";

        public StackTraceException(SourcefilePosition srcPos, string local, System.Exception innerTrace)
            : base(innerTrace.Message, innerTrace)
        {
            SrcPos = srcPos;
            Local = local;
            InnerTrace = innerTrace;
        }

        public StackTraceException(SourcefilePosition srcPos, string local, StackTraceException innerTrace, string? message = null)
            : base(message ?? "<...>", innerTrace)
        {
            SrcPos = srcPos;
            Local = local;
        }
    }
}