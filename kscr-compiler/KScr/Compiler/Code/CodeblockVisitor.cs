﻿using KScr.Antlr;
using KScr.Core;
using KScr.Core.Bytecode;
using KScr.Core.Model;

namespace KScr.Compiler.Code;

public class CodeblockVisitor : AbstractVisitor<ExecutableCode>
{
    public CodeblockVisitor(RuntimeBase vm, CompilerContext ctx) : base(vm, ctx)
    {
    }

    public override ExecutableCode VisitStatements(KScrParser.StatementsContext context)
    {
        var code = new ExecutableCode();
        foreach (var stmt in context.statement())
            code.Main.Add(VisitStatement(stmt));
        return code;
    }

    public override ExecutableCode VisitUniformBlock(KScrParser.UniformBlockContext context)
    {
        var code = new ExecutableCode();
        Statement stmt;
        if (context.expr() != null)
        {
            stmt = new Statement
            {
                Type = StatementComponentType.Expression,
                CodeType = BytecodeType.Parentheses
            };
            stmt.Main.Add(VisitExpression(context.expr()));
        }
        else stmt = VisitStatement(context.statement());
        code.Main.Add(stmt);
        return code;
    }

    public override ExecutableCode VisitNoBlock(KScrParser.NoBlockContext context)
    {
        return new ExecutableCode();
    }
}