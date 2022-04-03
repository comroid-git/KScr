using KScr.Antlr;
using KScr.Core;
using KScr.Core.Bytecode;
using KScr.Core.Model;

namespace KScr.Compiler.Code;

public class CodeblockVisitor : AbstractVisitor<ExecutableCode>
{
    public CodeblockVisitor(RuntimeBase vm, KScrParser parser, CompilerContext ctx) : base(vm, ctx)
    {
    }

    public override ExecutableCode VisitCodeBodyBlock(KScrParser.CodeBodyBlockContext context)
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

    public override ExecutableCode VisitCodeNoBlock(KScrParser.CodeNoBlockContext context) => new();

    protected override ExecutableCode VisitCode(KScrParser.CodeBlockContext code) => Visit(code);
}