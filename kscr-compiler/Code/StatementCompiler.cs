using KScr.Lib;
using KScr.Lib.Bytecode;
using KScr.Lib.Exception;
using KScr.Lib.Model;
using static KScr.Lib.Exception.CompilerError;
using static KScr.Lib.Model.TokenType;

namespace KScr.Compiler.Code
{
    public class StatementCompiler : AbstractCodeCompiler
    {
        public StatementCompiler() : this(null!)
        {
        }

        public StatementCompiler(ICompiler parent, bool endBeforeTerminator = false, params TokenType[] terminators) :
            base(parent, endBeforeTerminator, terminators)
        {
        }

        public override ICompiler? AcceptToken(RuntimeBase vm, ref CompilerContext ctx)
        {
            if (ctx.TokenIndex >= ctx.Tokens.Count)
            {
                _active = false;
                return this;
            }

            CompilerContext? subctx;
            bool hasParensBody;
            switch (ctx.Token.Type)
            {
                case Return:
                case Throw:
                    bool isReturn = ctx.Token.Type == Return;
                    ctx.Statement = new Statement
                    {
                        Type = StatementComponentType.Code,
                        CodeType = isReturn ? BytecodeType.Return : BytecodeType.Throw,
                        TargetType = isReturn
                            ? Lib.Bytecode.Class.VoidType.DefaultInstance
                            : Lib.Bytecode.Class.ThrowableType.DefaultInstance
                    };

                    // compile exception expression
                    ctx.TokenIndex += 1;
                    subctx = new CompilerContext(ctx, CompilerType.CodeExpression);
                    subctx.Statement = new Statement
                    {
                        Type = StatementComponentType.Expression,
                        CodeType = isReturn ? BytecodeType.Return : BytecodeType.Throw,
                        TargetType = isReturn
                            ? Lib.Bytecode.Class.VoidType.DefaultInstance
                            : Lib.Bytecode.Class.ThrowableType.DefaultInstance
                    };
                    CompilerLoop(vm, new ExpressionCompiler(this), ref subctx);
                    ctx.Component = new StatementComponent
                    {
                        Type = StatementComponentType.Code,
                        CodeType = isReturn ? BytecodeType.Return : BytecodeType.Throw,
                        SubStatement = subctx.Statement,
                        SourcefilePosition = ctx.Token.SourcefilePosition
                    };
                    ctx.TokenIndex = subctx.TokenIndex - 1;
                    _doneAnything = true;
                    break;
                case If:
                    if (ctx.NextToken?.Type != ParRoundOpen)
                        throw new CompilerException(ctx.Token.SourcefilePosition, InvalidToken, 
                            ctx.Class.FullName, ctx.Token.String(), "missing condition");
                    ctx.Statement = new Statement
                    {
                        Type = StatementComponentType.Code,
                        CodeType = BytecodeType.StmtIf
                    };
                    ctx.Component = new StatementComponent
                    {
                        Type = StatementComponentType.Code,
                        CodeType = BytecodeType.StmtIf,
                        SourcefilePosition = ctx.Token.SourcefilePosition
                    };

                    ctx.TokenIndex += 2;
                    // parse condition
                    subctx = new CompilerContext(ctx, CompilerType.CodeExpression);
                    subctx.Statement = new Statement
                    {
                        Type = StatementComponentType.Code,
                        CodeType = BytecodeType.StmtCond
                    };
                    CompilerLoop(vm, new ExpressionCompiler(this, false,
                        ParRoundClose), ref subctx);
                    ctx.LastComponent!.SubStatement = subctx.Statement;
                    ctx.TokenIndex = subctx.TokenIndex;

                    ctx.TokenIndex += 1;
                    // parse body
                    subctx = new CompilerContext(ctx, CompilerType.CodeStatement);
                    hasParensBody = ctx.Token.Type == ParAccOpen;
                    CompilerLoop(vm, new StatementCompiler(this, false,
                        hasParensBody ? ParAccClose : Terminator), ref subctx);
                    ctx.LastComponent!.InnerCode = subctx.ExecutableCode;
                    ctx.TokenIndex = subctx.TokenIndex - (hasParensBody ? 0 : 1);
                    _doneAnything = true;
                    break;
                case Else:
                    if (ctx.LastComponent?.CodeType != BytecodeType.StmtIf)
                        throw new CompilerException(ctx.Token.SourcefilePosition, InvalidToken, 
                            ctx.Class.FullName, ctx.Token.String(), "must be trailing an if-Statement");
                    ctx.TokenIndex += 1;
                    // parse body
                    subctx = new CompilerContext(ctx, CompilerType.CodeStatement);
                    hasParensBody = ctx.Token.Type == ParAccOpen;
                    CompilerLoop(vm, new StatementCompiler(this, false,
                        hasParensBody ? ParAccClose : Terminator), ref subctx);
                    ctx.LastComponent!.SubComponent = new StatementComponent
                    {
                        Type = StatementComponentType.Code,
                        CodeType = BytecodeType.StmtElse,
                        InnerCode = subctx.ExecutableCode,
                        SourcefilePosition = ctx.Token.SourcefilePosition
                    };
                    ctx.TokenIndex = subctx.TokenIndex;
                    _doneAnything = true;
                    break;
                case For:
                    if (ctx.NextToken?.Type != ParRoundOpen)
                        throw new CompilerException(ctx.Token.SourcefilePosition, InvalidToken, 
                            ctx.Class.FullName, ctx.Token.String(), "missing specification");
                    ctx.Statement = new Statement
                    {
                        Type = StatementComponentType.Code,
                        CodeType = BytecodeType.StmtFor
                    };
                    ctx.Component = new StatementComponent
                    {
                        Type = StatementComponentType.Code,
                        CodeType = BytecodeType.StmtFor,
                        SourcefilePosition = ctx.Token.SourcefilePosition
                    };

                    // parse start statement
                    ctx.TokenIndex += 2;
                    subctx = new CompilerContext(ctx, CompilerType.CodeStatement);
                    CompilerLoop(vm, new StatementCompiler(this, false, Terminator), ref subctx);
                    ctx.LastComponent!.SubStatement = subctx.Statement;
                    ctx.TokenIndex = subctx.TokenIndex + 1;

                    // parse continue-check
                    subctx = new CompilerContext(ctx, CompilerType.CodeExpression);
                    subctx.Statement = new Statement
                    {
                        Type = StatementComponentType.Code,
                        CodeType = BytecodeType.StmtCond,
                        TargetType = Lib.Bytecode.Class.NumericType.DefaultInstance
                    };
                    CompilerLoop(vm, new ExpressionCompiler(this), ref subctx);
                    ctx.LastComponent!.SubComponent = subctx.Component;
                    ctx.TokenIndex = subctx.TokenIndex + 1;

                    // parse accumulator
                    subctx = new CompilerContext(ctx, CompilerType.CodeStatement);
                    subctx.Statement = new Statement();
                    subctx.Statement = new Statement
                    {
                        Type = StatementComponentType.Code,
                        CodeType = BytecodeType.StmtCond,
                        TargetType = Lib.Bytecode.Class.NumericType.DefaultInstance
                    };
                    CompilerLoop(vm, new StatementCompiler(this, false, ParRoundClose), ref subctx);
                    ctx.LastComponent!.AltStatement = subctx.Statement;
                    ctx.TokenIndex = subctx.TokenIndex + 1;

                    // parse body
                    subctx = new CompilerContext(ctx, CompilerType.CodeStatement);
                    hasParensBody = ctx.Token.Type == ParAccOpen;
                    CompilerLoop(vm, new StatementCompiler(this, false,
                        hasParensBody ? ParAccClose : Terminator), ref subctx);
                    ctx.LastComponent!.InnerCode = subctx.ExecutableCode;
                    ctx.TokenIndex = subctx.TokenIndex;
                    _doneAnything = true;
                    break;
                case ForEach:
                    if (ctx.NextToken?.Type != ParRoundOpen)
                        throw new CompilerException(ctx.Token.SourcefilePosition, InvalidToken, 
                            ctx.Class.FullName, ctx.Token.String(), "missing specification");
                    ctx.Statement = new Statement
                    {
                        Type = StatementComponentType.Code,
                        CodeType = BytecodeType.StmtForEach
                    };
                    ctx.Component = new StatementComponent
                    {
                        Type = StatementComponentType.Code,
                        CodeType = BytecodeType.StmtForEach,
                        SourcefilePosition = ctx.Token.SourcefilePosition
                    };

                    ctx.TokenIndex += 2;
                    // parse n's name
                    if (ctx.Token.Type != Word)
                        throw new CompilerException(ctx.Token.SourcefilePosition, InvalidToken, 
                            ctx.Class.FullName, "foreach.n", "missing n Identifier");
                    ctx.LastComponent!.Arg = ctx.Token.Arg!;

                    // parse range
                    ctx.TokenIndex += 2;
                    if (ctx.PrevToken!.Type != Colon)
                        throw new CompilerException(ctx.Token.SourcefilePosition, InvalidToken, 
                            ctx.Class.FullName, "foreach.colon", "missing delimiter colon");
                    subctx = new CompilerContext(ctx, CompilerType.CodeExpression);
                    subctx.Statement = new Statement
                    {
                        Type = StatementComponentType.Code,
                        CodeType = BytecodeType.Expression,
                        TargetType = Lib.Bytecode.Class.IterableType.DefaultInstance
                    };
                    CompilerLoop(vm, new ExpressionCompiler(this, false, ParRoundClose), ref subctx);
                    ctx.LastComponent!.SubStatement = subctx.Statement;
                    ctx.TokenIndex = subctx.TokenIndex;

                    // parse body
                    ctx.TokenIndex += 1;
                    subctx = new CompilerContext(ctx, CompilerType.CodeStatement);
                    hasParensBody = ctx.Token.Type == ParAccOpen;
                    CompilerLoop(vm, new StatementCompiler(this, false,
                        hasParensBody ? ParAccClose : Terminator), ref subctx);
                    ctx.LastComponent!.InnerCode = subctx.ExecutableCode;
                    ctx.TokenIndex = subctx.TokenIndex;
                    _doneAnything = true;
                    break;
                case While:
                    if (ctx.NextToken?.Type != ParRoundOpen)
                        throw new CompilerException(ctx.Token.SourcefilePosition, InvalidToken, 
                            ctx.Class.FullName, ctx.Token.String(), "missing condition");
                    ctx.Statement = new Statement
                    {
                        Type = StatementComponentType.Code,
                        CodeType = BytecodeType.StmtWhile
                    };
                    ctx.Component = new StatementComponent
                    {
                        Type = StatementComponentType.Code,
                        CodeType = BytecodeType.StmtWhile,
                        SourcefilePosition = ctx.Token.SourcefilePosition
                    };

                    // parse condition
                    ctx.TokenIndex += 2;
                    subctx = new CompilerContext(ctx, CompilerType.CodeExpression);
                    subctx.Statement = new Statement
                    {
                        Type = StatementComponentType.Expression,
                        CodeType = BytecodeType.StmtCond
                    };
                    CompilerLoop(vm, new ExpressionCompiler(this, false, ParRoundClose), ref subctx);
                    ctx.LastComponent!.SubStatement = subctx.Statement;
                    ctx.TokenIndex = subctx.TokenIndex + 1;

                    // parse body
                    subctx = new CompilerContext(ctx, CompilerType.CodeStatement);
                    hasParensBody = ctx.Token.Type == ParAccOpen;
                    CompilerLoop(vm, new StatementCompiler(this, false,
                        hasParensBody ? ParAccClose : Terminator), ref subctx);
                    ctx.LastComponent!.InnerCode = subctx.ExecutableCode;
                    ctx.TokenIndex = subctx.TokenIndex;
                    _doneAnything = true;
                    break;
                case Do:
                    ctx.Statement = new Statement
                    {
                        Type = StatementComponentType.Code,
                        CodeType = BytecodeType.StmtDo
                    };
                    ctx.Component = new StatementComponent
                    {
                        Type = StatementComponentType.Code,
                        CodeType = BytecodeType.StmtDo,
                        SourcefilePosition = ctx.Token.SourcefilePosition
                    };
                    ctx.TokenIndex += 1;

                    hasParensBody = ctx.Token!.Type == ParAccOpen;
                    // parse body
                    subctx = new CompilerContext(ctx, CompilerType.CodeStatement);
                    CompilerLoop(vm, new ExpressionCompiler(this, false,
                        hasParensBody ? ParAccClose : Terminator), ref subctx);
                    ctx.LastComponent!.InnerCode = subctx.ExecutableCode;
                    ctx.TokenIndex = subctx.TokenIndex + 1;

                    // parse condition
                    ctx.TokenIndex += hasParensBody ? 2 : 1;
                    subctx = new CompilerContext(ctx, CompilerType.CodeExpression);
                    subctx.Statement = new Statement
                    {
                        Type = StatementComponentType.Expression,
                        CodeType = BytecodeType.StmtCond
                    };
                    CompilerLoop(vm, new ExpressionCompiler(this, false, ParRoundClose), ref subctx);
                    ctx.LastComponent!.SubStatement = subctx.Statement;
                    ctx.TokenIndex = subctx.TokenIndex + 1;
                    _doneAnything = true;
                    break;
                case Finally:
                    if (ctx.Statement == null)
                        throw new CompilerException(ctx.Token.SourcefilePosition, UnexpectedToken, 
                            ctx.Class.FullName, ctx.Token.String(), "must be trailing statement");

                    // parse body
                    ctx.TokenIndex += 1;
                    subctx = new CompilerContext(ctx, CompilerType.CodeStatement);
                    hasParensBody = ctx.Token.Type == ParAccOpen;
                    CompilerLoop(vm, new ExpressionCompiler(this, false,
                        hasParensBody ? ParAccClose : Terminator), ref subctx);
                    ctx.Statement.Finally = subctx.ExecutableCode;
                    ctx.TokenIndex = subctx.TokenIndex;
                    _doneAnything = true;
                    break;
            }

            return base.AcceptToken(vm, ref ctx);
        }
    }
}