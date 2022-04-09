using KScr.Antlr;
using KScr.Core;
using KScr.Core.Bytecode;
using KScr.Core.Model;

namespace KScr.Compiler.Code;

public class CodeblockVisitor : AbstractVisitor<ExecutableCode>
{
    public CodeblockVisitor(RuntimeBase vm, CompilerContext ctx) : base(vm, ctx)
    {
    }

    public override ExecutableCode VisitNormalBlock(KScrParser.NormalBlockContext context)
    {
        var code = new ExecutableCode();
        foreach (var stmt in context.statement())
            code.Main.Add(VisitStatement(stmt));
        return code;
    }

    public override ExecutableCode VisitCodeStmtBlock(KScrParser.CodeStmtBlockContext context)
    {
        var code = new ExecutableCode();
        code.Main.Add(VisitStatement(context.statement()));
        return code;
    }

    public override ExecutableCode VisitMemberExprBlock(KScrParser.MemberExprBlockContext context)
    {
        var code = new ExecutableCode();
        var stmt = new Statement
        {
            Type = StatementComponentType.Expression,
            CodeType = BytecodeType.Parentheses
        };
        stmt.Main.Add(VisitExpression(context.expr()));
        code.Main.Add(stmt);
        return code;
    }

    public override ExecutableCode VisitNoBlock(KScrParser.NoBlockContext context)
    {
        return new ExecutableCode();
    }
}