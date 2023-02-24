using System;
using System.Collections.Generic;
using Antlr4.Runtime;
using KScr.Antlr;
using KScr.Compiler.Code;
using KScr.Core.Model;
using KScr.Core.System;

namespace KScr.Compiler;

public static class Utils
{
    public static SourcefilePosition ToSrcPos(this ParserRuleContext context)
    {
        return new SourcefilePosition
        {
            SourcefilePath = context.Start.TokenSource.SourceName,
            SourcefileLine = context.Start.Line,
            SourcefileCursor = context.Start.Column
        };
    }

    public static SourcefilePosition ToSrcPos(this IToken token, string? clsName = null) => new()
        { SourcefileLine = token.Line, SourcefileCursor = token.Column, SourcefilePath = clsName ?? "<unknown>" };

    public static IEnumerable<ITypeInfo> GetGenericsUses(this KScrParser.GenericTypeUsesContext context,
        CompilerRuntime vm, CompilerContext ctx, Core.System.Class target)
    {
        if (context?.n is { } n)
            yield return new TypeParameter(int.Parse(n.Text));
        var use = context?.type() ?? Array.Empty<KScrParser.TypeContext>();
        for (var i = 0; i < use.Length; i++)
            yield return new TypeParameter(target.TypeParameters[i].Name, TypeParameterSpecializationType.Extends,
                new TypeInfoVisitor(vm, ctx).Visit(use[i]).AsClass(vm));
    }
}