using KScr.Core.Store;

namespace KScr.Core.Exception
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
    
    public class FatalException : System.Exception
    {
        public FatalException(string? message) : base(message)
        {
        }

        public FatalException(string? message, System.Exception? innerException) : base(message, innerException)
        {
        }
    }

    public interface IStackTrace
    {
        CallLocation CallLoc { get; }
        string Message { get; }
    }

    public class StackTraceException : System.Exception, IStackTrace
    {
        public StackTraceException(CallLocation srcPos, string local, System.Exception innerTrace,
            string? message = null)
            : base(message ?? innerTrace.Message, innerTrace)
        {
            CallLoc = srcPos;
            Local = local;
            InnerTrace = innerTrace;
        }

        public StackTraceException(CallLocation srcPos, string local, StackTraceException innerTrace,
            string? message = null)
            : base(message ?? "<...>", innerTrace)
        {
            CallLoc = srcPos;
            Local = local;
        }

        public System.Exception? InnerTrace { get; }
        public string Local { get; }
        public CallLocation CallLoc { get; }

        public override string Message => $"({CallLoc.SourceName}" + (CallLoc.SourceLine == 0
            ? string.Empty
            : $" [line {CallLoc.SourceLine} pos {CallLoc.SourceCursor}]") + ") " + base.Message;
    }
}