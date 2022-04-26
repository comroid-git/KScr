using System;
using System.Diagnostics;
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
        return new Statement
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
        return new Statement
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
        return new Statement
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
        return new Statement
        {
            Type = StatementComponentType.Code,
            CodeType = BytecodeType.Throw,
            Main =
            {
                VisitExpression(context.throwStatement())
            }
        };
    }

    public override Statement VisitStmtPipeRead(KScrParser.StmtPipeReadContext context) => VisitPipeRead(context.pipe, context.expr());

    public override Statement VisitStmtPipeWrite(KScrParser.StmtPipeWriteContext context) => VisitPipeWrite(context.pipe, context.expr());

    public override Statement VisitStmtPipeListen(KScrParser.StmtPipeListenContext context) => VisitPipeListen(context.pipe, context.expr());

    public override Statement VisitMarkStatement(KScrParser.MarkStatementContext context)
    {
        return new Statement
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
        return new Statement
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

    public override Statement VisitStmtTryCatch(KScrParser.StmtTryCatchContext context)
    {
        var stmt = new Statement()
        {
            Type = StatementComponentType.Code,
            CodeType = BytecodeType.StmtTry,
            CatchFinally = context.catchBlocks() == null ? null : VisitCatchBlocks(context.catchBlocks())
        };
        stmt.Main.Add(new StatementComponent()
        {
            Type = StatementComponentType.Code,
            CodeType = BytecodeType.StmtTry,
            InnerCode = VisitCode(context.tryCatchStatement().codeBlock())
        });
        return stmt;
    }

    public override Statement VisitStmtTryWithRes(KScrParser.StmtTryWithResContext context)
    {
        var stmt = new Statement()
        {
            Type = StatementComponentType.Code,
            CodeType = BytecodeType.StmtTry,
            // catch and finally blocks
            CatchFinally = context.catchBlocks() == null ? null : VisitCatchBlocks(context.catchBlocks())
        };
        var defs = new Statement()
        {
            Type = StatementComponentType.Code,
            CodeType = BytecodeType.StmtTry
        };
        foreach (var decl in context.tryWithResourcesStatement().declaration())
        {// resource declarations
            defs.Main.Add(new StatementComponent()
            {
                Type = StatementComponentType.Code,
                CodeType = BytecodeType.StmtTry,
                Arg = decl.type().GetText() + ';' + decl.idPart().GetText(),
                SubComponent = VisitExpression(decl.expr())
            });
        }
        stmt.Main.Add(new StatementComponent()
        {
            Type = StatementComponentType.Code,
            CodeType = BytecodeType.StmtTry,
            SubStatement = defs,
            InnerCode = VisitCode(context.tryWithResourcesStatement().codeBlock())
        });
        return stmt;
    }

    public override Statement VisitStmtIf(KScrParser.StmtIfContext context)
    {
        return new Statement
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
            CatchFinally = context.catchBlocks() == null ? null : VisitCatchBlocks(context.catchBlocks())
        };
    }

    public override Statement VisitStmtWhile(KScrParser.StmtWhileContext context)
    {
        return new Statement
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
            CatchFinally = context.catchBlocks() == null ? null : VisitCatchBlocks(context.catchBlocks())
        };
    }

    public override Statement VisitStmtDoWhile(KScrParser.StmtDoWhileContext context)
    {
        return new Statement
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
            CatchFinally = context.catchBlocks() == null ? null : VisitCatchBlocks(context.catchBlocks())
        };
    }

    public override Statement VisitStmtFor(KScrParser.StmtForContext context)
    {
        return new Statement
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
            CatchFinally = context.catchBlocks() == null ? null : VisitCatchBlocks(context.catchBlocks())
        };
    }

    public override Statement VisitStmtForeach(KScrParser.StmtForeachContext context)
    {
        return new Statement
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
            CatchFinally = context.catchBlocks() == null ? null : VisitCatchBlocks(context.catchBlocks())
        };
    }

    public override Statement VisitSwitchStatement(KScrParser.SwitchStatementContext context) => new()
    {
        Type = StatementComponentType.Code,
        CodeType = BytecodeType.StmtSwitch,
        Main = { VisitExpression(context) }
    };

    public override Statement VisitStmtEmpty(KScrParser.StmtEmptyContext context)
    {
        return new Statement();
    }
}