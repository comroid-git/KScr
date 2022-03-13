using KScr.Lib.Model;
using KScr.Lib.Store;

namespace KScr.Lib.Exception
{
    public class CompilerException : System.Exception, IStackTrace
    {
        public CompilerException(SourcefilePosition srcPos, string? message) : base(message)
        {
            CallLoc = new CallLocation(srcPos);
        }

        public override string Message => base.Message +
                                          $"\n\tin File {CallLoc.SourceName} line {CallLoc.SourceLine} pos {CallLoc.SourceCursor}";

        public CallLocation CallLoc { get; }
    }
}