using System;
using System.Linq;
using KScr.Core.Model;
using KScr.Core.Store;

namespace KScr.Core.Exception;

public sealed class CompilerErrorMessage
{
    public static readonly CompilerErrorMessage Underlying = new("One or more errors occurred during compilation");
    public static readonly CompilerErrorMessage UnderlyingDetail = new("An internal error occurred;"+Environment.NewLine+"\t\t{0}");
    
    public static readonly CompilerErrorMessage UnexpectedToken = new("Unexpected Token <{1}> in class {0};"+Environment.NewLine+"\t\t{2}");
    public static readonly CompilerErrorMessage InvalidToken = new("Invalid Token <{1}> in class {0};"+Environment.NewLine+"\t\t{2}");
    public static readonly CompilerErrorMessage InvalidType = new("Invalid Type <{1}> in class {0};"+Environment.NewLine+"\t\t{2}");
    public static readonly CompilerErrorMessage Invalid = new("Invalid {1} in class {0};"+Environment.NewLine+"\t\t{2}");

    public static readonly CompilerErrorMessage CannotAssign = new("Cannot assign {1} to {0}");

    public static readonly CompilerErrorMessage SymbolNotFound = new("Symbol '{0}' not found in {1}");
    public static readonly CompilerErrorMessage TypeSymbolNotFound = new("Type '{0}' not found");

    public static readonly CompilerErrorMessage ClassPackageMissing = new("Missing package declaration in class {0}");
    public static readonly CompilerErrorMessage ClassNameMissing = new("Missing class name in class {0}");

    public static readonly CompilerErrorMessage ClassNameMismatch =
        new("Declared Class name {1} mismatches File name {0}");

    public static readonly CompilerErrorMessage ClassInvalidMemberType =
        new("Invalid member Type {1} in class {0};"+Environment.NewLine+"\t\t{2}");

    public static readonly CompilerErrorMessage ClassAbstractMemberNotImplemented =
        new("Class {0} does not implement the following abstract members:\n{1}");

    public readonly string Message;

    public CompilerErrorMessage(string message)
    {
        Message = message;
    }

    public string Format(object[] messageArgs)
    {
        return string.Format(Message, messageArgs);
    }
}

public class CompilerException : global::System.Exception, IStackTrace
{
    public CompilerException(SourcefilePosition srcPos, CompilerErrorMessage errorMessage,
        params object?[] messageArgs /* expected, actual */) : base(errorMessage.Format(messageArgs))
    {
        CallLoc = new CallLocation(srcPos);
    }

    public override string Message => $"{base.Message}" +
                                      (IsUnderlying &&
                                       CallLoc.SourceName == RuntimeBase.SystemSrcPos.SourcefilePath
                                          ? string.Empty
                                          : $"{Environment.NewLine}\t\tin file '{CallLoc.SourceName}' line {CallLoc.SourceRow} pos {CallLoc.SourceColumn}");

    public CallLocation CallLoc { get; }

    public bool IsUnderlying 
        => new[] { CompilerErrorMessage.Underlying.Message, CompilerErrorMessage.UnderlyingDetail.Message.Substring(0, 20) }
            .Any(base.Message.StartsWith);
}