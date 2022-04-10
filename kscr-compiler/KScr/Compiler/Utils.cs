using System;
using System.Diagnostics;
using Antlr4.Runtime;
using KScr.Antlr;
using KScr.Core.Bytecode;
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

    public static string GetName(this KScrParser.MemberContext member) => member.RuleIndex switch
    {
        KScrParser.RULE_initDecl => Method.StaticInitializerName,
        KScrParser.RULE_constructorDecl => Method.ConstructorName,
        KScrParser.RULE_propertyDecl => (member as KScrParser.MemPropContext)!.propertyDecl().idPart().GetText(),
        KScrParser.RULE_methodDecl => (member as KScrParser.MemMtdContext)!.methodDecl().idPart().GetText(),
        KScrParser.RULE_classDecl => (member as KScrParser.MemClsContext)!.classDecl().idPart().GetText(),
        _ => throw new ArgumentOutOfRangeException(nameof(member.RuleIndex), member.RuleIndex,
            "Invalid Member ruleIndex")
    };
}