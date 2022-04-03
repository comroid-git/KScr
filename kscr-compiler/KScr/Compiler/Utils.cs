using System;
using Antlr4.Runtime;
using KScr.Antlr;
using KScr.Compiler.Class;
using KScr.Core.Bytecode;
using KScr.Core.Model;

namespace KScr.Compiler;

public static class Utils
{
    public static SourcefilePosition ToSrcPos(ParserRuleContext context) => new()
    {
        SourcefilePath = context.Start.TokenSource.SourceName,
        SourcefileLine = context.Start.Line,
        SourcefileCursor = context.Start.TokenIndex
    };
}
