using KScr.Core.Model;
using KScr.Core.Store;

namespace KScr.Core.Exception;

public sealed class CompilerError
{
    public static readonly CompilerError UnexpectedToken = new("Unexpected Token <{1}> in class {0}; {2}");
    public static readonly CompilerError InvalidToken = new("Invalid Token <{1}> in class {0}; {2}");
    public static readonly CompilerError InvalidType = new("Invalid Type <{1}> in class {0}; {2}");
    public static readonly CompilerError Invalid = new("Invalid {1} in class {0}; {2}");

    public static readonly CompilerError CannotAssign = new("Cannot assign type {1} to type {0}");

    public static readonly CompilerError SymbolNotFound = new("Symbol '{0}' not found in context {1}");
    public static readonly CompilerError TypeSymbolNotFound = new("Type '{0}' not found");

    public static readonly CompilerError ClassPackageMissing = new("Missing package declaration in class {0}");
    public static readonly CompilerError ClassNameMissing = new("Missing class name in class {0}");
    public static readonly CompilerError ClassNameMismatch = new("Declared Class name {1} mismatches File name {0}");
    public static readonly CompilerError ClassInvalidMemberType = new("Invalid member Type {1} in class {0}; {2}");

    public static readonly CompilerError ClassAbstractMemberNotImplemented =
        new("Class {0} does not implement the following abstract members:\n{1}");

    public readonly string Message;

    public CompilerError(string message)
    {
        Message = message;
    }

    public string Format(object[] messageArgs)
    {
        return string.Format(Message, messageArgs);
    }
}

public class CompilerException : System.Exception, IStackTrace
{
    public CompilerException(SourcefilePosition srcPos, CompilerError error,
        params object?[] messageArgs /* expected, actual */) : base(error.Format(messageArgs))
    {
        CallLoc = new CallLocation(srcPos);
    }

    public override string Message => base.Message +
                                      $"\n\tin File {CallLoc.SourceName} line {CallLoc.SourceLine} pos {CallLoc.SourceCursor}";

    public CallLocation CallLoc { get; }
}