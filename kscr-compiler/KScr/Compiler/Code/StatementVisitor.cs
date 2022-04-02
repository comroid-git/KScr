using System;
using KScr.Antlr;
using KScr.Core;
using KScr.Core.Bytecode;

namespace KScr.Compiler.Code;

public class StatementVisitor : AbstractVisitor<Statement> {
    public StatementVisitor(RuntimeBase vm, KScrParser parser, CompilerContext ctx) : base(vm, parser, ctx)
    {
    }

    public override Statement VisitStmtDeclare(KScrParser.StmtDeclareContext context)
    {
        throw new NotImplementedException("Compiling of statement " + context + " is not supported");
    }

    public override Statement VisitStmtAssign(KScrParser.StmtAssignContext context)
    {
        throw new NotImplementedException("Compiling of statement " + context + " is not supported");
    }

    public override Statement VisitStmtReturn(KScrParser.StmtReturnContext context)
    {
        throw new NotImplementedException("Compiling of statement " + context + " is not supported");
    }

    public override Statement VisitStmtThrow(KScrParser.StmtThrowContext context)
    {
        throw new NotImplementedException("Compiling of statement " + context + " is not supported");
    }

    public override Statement VisitStmtCtor(KScrParser.StmtCtorContext context)
    {
        throw new NotImplementedException("Compiling of statement " + context + " is not supported");
    }

    public override Statement VisitStmtPipe(KScrParser.StmtPipeContext context)
    {
        throw new NotImplementedException("Compiling of statement " + context + " is not supported");
    }

    public override Statement VisitStmtTryCatch(KScrParser.StmtTryCatchContext context)
    {
        throw new NotImplementedException("Compiling of statement " + context + " is not supported");
    }

    public override Statement VisitStmtTryWithRes(KScrParser.StmtTryWithResContext context)
    {
        throw new NotImplementedException("Compiling of statement " + context + " is not supported");
    }

    public override Statement VisitStmtMark(KScrParser.StmtMarkContext context)
    {
        throw new NotImplementedException("Compiling of statement " + context + " is not supported");
    }

    public override Statement VisitStmtJump(KScrParser.StmtJumpContext context)
    {
        throw new NotImplementedException("Compiling of statement " + context + " is not supported");
    }

    public override Statement VisitStmtIf(KScrParser.StmtIfContext context)
    {
        throw new NotImplementedException("Compiling of statement " + context + " is not supported");
    }

    public override Statement VisitStmtWhile(KScrParser.StmtWhileContext context)
    {
        throw new NotImplementedException("Compiling of statement " + context + " is not supported");
    }

    public override Statement VisitStmtFor(KScrParser.StmtForContext context)
    {
        throw new NotImplementedException("Compiling of statement " + context + " is not supported");
    }

    public override Statement VisitStmtForeach(KScrParser.StmtForeachContext context)
    {
        throw new NotImplementedException("Compiling of statement " + context + " is not supported");
    }

    public override Statement VisitStmtSwitch(KScrParser.StmtSwitchContext context)
    {
        throw new NotImplementedException("Compiling of statement " + context + " is not supported");
    }

    public override Statement VisitStmtDoWhile(KScrParser.StmtDoWhileContext context)
    {
        throw new NotImplementedException("Compiling of statement " + context + " is not supported");
    }

    public override Statement VisitStmtEmpty(KScrParser.StmtEmptyContext context)
    {
        throw new NotImplementedException("Compiling of statement " + context + " is not supported");
    }

    protected override Statement VisitStatement(KScrParser.StatementContext stmt) => Visit(stmt);
}
