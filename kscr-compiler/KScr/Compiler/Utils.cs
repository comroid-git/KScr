using Antlr4.Runtime;
using KScr.Core.Model;

namespace KScr.Compiler;

public static class Utils
{
    public static SourcefilePosition ToSrcPos(ParserRuleContext context)
    {
        return new SourcefilePosition
        {
            SourcefilePath = context.Start.TokenSource.SourceName,
            SourcefileLine = context.Start.Line,
            SourcefileCursor = context.Start.TokenIndex
        };
    }
}