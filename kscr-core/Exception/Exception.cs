using System;
using System.IO;
using KScr.Core.Std;
using KScr.Core.Store;

namespace KScr.Core.Exception;

public class RuntimeException : System.Exception
{
    internal readonly IObject Obj;

    public RuntimeException(string? message, IObject? obj = null) : base(message)
    {
        Obj = obj ?? IObject.Null;
    }

    public RuntimeException(string? message, System.Exception? innerException) : base(
        message ?? innerException?.Message, innerException)
    {
        Obj = IObject.Null;
    }
}

public class FatalException :
#if DEBUG
    System.Exception
#else
    RuntimeException
#endif
{
    public FatalException(string? message) : base(message)
    {
    }

    public FatalException(string? message, System.Exception? innerException) : base(message ?? innerException?.Message,
        innerException)
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
    public StackTraceException(CallLocation srcPos, string local, RuntimeException innerCause,
        string? message = null)
        : base(message ?? "<...>", innerCause)
    {
        CallLoc = srcPos;
        Local = local;
        InnerCause = innerCause;
    }

    public StackTraceException(CallLocation srcPos, string local, StackTraceException innerTrace,
        string? message = null)
        : base(message ?? "<...>", innerTrace)
    {
        CallLoc = srcPos;
        Local = local;
        InnerCause = innerTrace.InnerCause;
    }

    public RuntimeException InnerCause { get; }
    public string Local { get; }
    public string BaseMessage => base.Message;
    public CallLocation CallLoc { get; }

    public override string Message => $"{CallLoc.SourceName}" + (CallLoc.SourceRow == 0
        ? string.Empty
        : $" [line {CallLoc.SourceRow} pos {CallLoc.SourceColumn}]"); // + BaseMessage;

    public void PrintStackTrace()
    {
        WriteStackTrace(Console.Out);
    }

    public void WriteStackTrace(TextWriter @out)
    {
        @out.WriteLine($"An exception occurred:\t{InnerCause.Message}");
        foreach (var stackTraceElement in Stack.StackTrace)
            @out.WriteLine($"\tat\t{stackTraceElement.Message}");
    }
}