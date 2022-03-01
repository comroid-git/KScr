using System.Runtime.Serialization;
using KScr.Lib.Model;

namespace KScr.Lib.Exception
{
    public class CompilerException : System.Exception, IStackTrace
    {
        public CompilerException(SourcefilePosition srcPos, string? message) : base(message)
        {
            SrcPos = srcPos;
        }

        public override string Message => base.Message + $"\n\tin File {SrcPos.SourcefilePath} line {SrcPos.SourcefileLine} pos {SrcPos.SourcefileCursor}";

        public SourcefilePosition SrcPos { get; }
    }
}