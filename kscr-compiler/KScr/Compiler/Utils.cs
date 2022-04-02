using System;
using Antlr4.Runtime;
using KScr.Antlr;
using KScr.Compiler.Class;
using KScr.Core.Bytecode;
using KScr.Core.Model;

namespace KScr.Compiler;

public static class Utils
{
    public static SourcefilePosition ToSrcPos(Parser parser, ParserRuleContext context) => new()
    {
        SourcefilePath = context.ToInfoString(parser)
    };
}

public class PackageDeclVisitor : KScrParserBaseVisitor<string>
{
    public override string VisitPackageDecl(KScrParser.PackageDeclContext context) => context.id().ToString();
}
