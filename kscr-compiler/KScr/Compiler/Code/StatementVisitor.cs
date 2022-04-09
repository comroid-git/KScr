using System;
using KScr.Antlr;
using KScr.Core;
using KScr.Core.Bytecode;
using KScr.Core.Model;

namespace KScr.Compiler.Code;

public class StatementVisitor : AbstractVisitor<Statement>
{
    public StatementVisitor(RuntimeBase vm, CompilerContext ctx) : base(vm, ctx)
    {
    }

    public override Statement VisitDeclaration(KScrParser.DeclarationContext context)
    {
        return new()
        {
            Type = StatementComponentType.Declaration,
            CodeType = BytecodeType.Declaration,
            Main =
            {
                VisitExpression(context)
            }
        };
    }

    public override Statement VisitStmtAssign(KScrParser.StmtAssignContext context)
    {
        return new()
        {
            Type = StatementComponentType.Code,
            CodeType = BytecodeType.Assignment,
            Main =
            {
                context.mutation().binaryop() is { } op
                    ? new StatementComponent
                    {
                        Type = StatementComponentType.Operator,
                        CodeType = BytecodeType.Assignment,
                        ByteArg = (ulong)(VisitOperator(op) | Operator.Compound | Operator.Binary),
                        SubComponent = VisitExpression(context.left),
                        AltComponent = VisitExpression(context.mutation().expr())
                    }
                    : new StatementComponent
                    {
                        Type = StatementComponentType.Code,
                        CodeType = BytecodeType.Assignment,
                        SubComponent = VisitExpression(context.left),
                        AltComponent = VisitExpression(context.mutation())
                    }
            }
        };
    }

    public override Statement VisitReturnStatement(KScrParser.ReturnStatementContext context)
    {
        return new()
        {
            Type = StatementComponentType.Code,
            CodeType = BytecodeType.Return,
            Main =
            {
                new StatementComponent
                {
                    Type = StatementComponentType.Code,
                    CodeType = BytecodeType.Return,
                    SubComponent = VisitExpression(context.expr())
                }
            }
        };
    }

    public override Statement VisitStmtCallMember(KScrParser.StmtCallMemberContext context)
    {
        var expr = VisitExpression(context.expr());
        expr.PostComponent = new StatementComponent
        {
            Type = StatementComponentType.Expression,
            CodeType = BytecodeType.Call,
            Arg = context.idPart().GetText(),
            SubStatement = VisitArguments(context.arguments())
        };
        return new Statement
        {
            Type = StatementComponentType.Expression,
            CodeType = BytecodeType.Call,
            Main = { expr }
        };
    }

    public override Statement VisitStmtThrow(KScrParser.StmtThrowContext context)
    {
        return new()
        {
            Type = StatementComponentType.Code,
            CodeType = BytecodeType.Throw,
            Main =
            {
                VisitExpression(context.throwStatement())
            }
        };
    }

    public override Statement VisitPipeStatement(KScrParser.PipeStatementContext context)
    {
        var stmt = new Statement
        {
            Type = StatementComponentType.Pipe,
            Main = { VisitExpression(context.expr()) }
        };
        if (context.pipeReadStatement() is { Length: > 0 } prs)
            foreach (var pr in prs)
                stmt.Main.Add(new StatementComponent
                {
                    Type = StatementComponentType.Consumer,
                    SubComponent = VisitExpression(pr.expr())
                });
        else if (context.pipeWriteStatement() is { Length: > 0 } pws)
            foreach (var pw in pws)
                stmt.Main.Add(new StatementComponent
                {
                    Type = StatementComponentType.Emitter,
                    SubComponent = VisitExpression(pw.expr())
                });
        return stmt;
    }

    public override Statement VisitStmtTryCatch(KScrParser.StmtTryCatchContext context)
    {
        throw new NotImplementedException("Compiling of statement " + context + " is not supported");
    }

    public override Statement VisitStmtTryWithRes(KScrParser.StmtTryWithResContext context)
    {
        throw new NotImplementedException("Compiling of statement " + context + " is not supported");
    }

    public override Statement VisitMarkStatement(KScrParser.MarkStatementContext context)
    {
        return new()
        {
            Type = StatementComponentType.Code,
            CodeType = BytecodeType.StmtMark,
            Main =
            {
                new StatementComponent
                {
                    Type = StatementComponentType.Code,
                    CodeType = BytecodeType.StmtMark,
                    Arg = context.idPart().GetText()
                }
            }
        };
    }

    public override Statement VisitJumpStatement(KScrParser.JumpStatementContext context)
    {
        return new()
        {
            Type = StatementComponentType.Code,
            CodeType = BytecodeType.StmtJump,
            Main =
            {
                new StatementComponent
                {
                    Type = StatementComponentType.Code,
                    CodeType = BytecodeType.StmtJump,
                    Arg = context.idPart().GetText()
                }
            }
        };
    }

    public override Statement VisitStmtIf(KScrParser.StmtIfContext context)
    {
        return new()
        {
            Type = StatementComponentType.Code,
            CodeType = BytecodeType.StmtIf,
            Main =
            {
                new StatementComponent
                {
                    Type = StatementComponentType.Code,
                    CodeType = BytecodeType.StmtIf,
                    SubComponent = VisitExpression(context.ifStatement().expr()),
                    InnerCode = VisitCode(context.ifStatement().codeBlock()),
                    AltComponent = context.ifStatement().elseStatement() is { } elseStmt
                        ? new StatementComponent
                        {
                            Type = StatementComponentType.Code,
                            CodeType = BytecodeType.StmtElse,
                            InnerCode = VisitCode(elseStmt.codeBlock())
                        }
                        : null
                }
            },
            Finally = VisitCode(context.finallyBlock())
        };
    }

    public override Statement VisitStmtWhile(KScrParser.StmtWhileContext context)
    {
        return new()
        {
            Type = StatementComponentType.Code,
            CodeType = BytecodeType.StmtWhile,
            Main =
            {
                new StatementComponent
                {
                    Type = StatementComponentType.Code,
                    CodeType = BytecodeType.StmtWhile,
                    SubComponent = VisitExpression(context.whileStatement().expr()),
                    InnerCode = VisitCode(context.whileStatement().codeBlock())
                }
            },
            Finally = VisitCode(context.finallyBlock())
        };
    }

    public override Statement VisitStmtDoWhile(KScrParser.StmtDoWhileContext context)
    {
        return new()
        {
            Type = StatementComponentType.Code,
            CodeType = BytecodeType.StmtDo,
            Main =
            {
                new StatementComponent
                {
                    Type = StatementComponentType.Code,
                    CodeType = BytecodeType.StmtDo,
                    SubComponent = VisitExpression(context.doWhile().expr()),
                    InnerCode = VisitCode(context.doWhile().codeBlock())
                }
            },
            Finally = VisitCode(context.finallyBlock())
        };
    }

    public override Statement VisitStmtFor(KScrParser.StmtForContext context)
    {
        return new()
        {
            Type = StatementComponentType.Code,
            CodeType = BytecodeType.StmtFor,
            Main =
            {
                new StatementComponent
                {
                    Type = StatementComponentType.Code,
                    CodeType = BytecodeType.StmtFor,
                    SubStatement = VisitStatement(context.forStatement().init),
                    SubComponent = VisitExpression(context.forStatement().cond),
                    AltComponent = VisitExpression(context.forStatement().acc),
                    InnerCode = VisitCode(context.forStatement().codeBlock())
                }
            },
            Finally = VisitCode(context.finallyBlock())
        };
    }

    public override Statement VisitStmtForeach(KScrParser.StmtForeachContext context)
    {
        return new()
        {
            Type = StatementComponentType.Code,
            CodeType = BytecodeType.StmtForEach,
            Main =
            {
                new StatementComponent
                {
                    Type = StatementComponentType.Code,
                    CodeType = BytecodeType.StmtForEach,
                    Arg = context.foreachStatement().idPart().GetText(),
                    SubComponent = VisitExpression(context.foreachStatement().expr()),
                    InnerCode = VisitCode(context.foreachStatement().codeBlock())
                }
            },
            Finally = VisitCode(context.finallyBlock())
        };
    }

    public override Statement VisitStmtSwitch(KScrParser.StmtSwitchContext context)
    {
        throw new NotImplementedException("Compiling of statement " + context + " is not supported");
    }

    public override Statement VisitStmtEmpty(KScrParser.StmtEmptyContext context)
    {
        return new();
    }
}