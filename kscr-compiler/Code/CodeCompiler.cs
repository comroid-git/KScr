using System;
using KScr.Lib;
using KScr.Lib.Bytecode;
using KScr.Lib.Exception;
using KScr.Lib.Model;
using KScr.Lib.Store;

namespace KScr.Compiler.Code
{
    public abstract class AbstractCodeCompiler : AbstractCompiler
    {
        protected bool _active = true;

        protected AbstractCodeCompiler(ICompiler parent) : base(parent)
        {
        }

        public override bool Active => _active;

        public override ICompiler? AcceptToken(RuntimeBase vm, ref CompilerContext ctx)
        {
            CompilerContext subctx;
            switch (ctx.Token.Type)
            {
                case TokenType.IdentVoid:
                    CompileDeclaration(ctx, Lib.Bytecode.Class.VoidType.DefaultInstance);
                    return this;
                case TokenType.IdentNum:
                    CompileDeclaration(ctx, Lib.Bytecode.Class.NumericType.DefaultInstance);
                    return this;
                case TokenType.IdentNumByte:
                    CompileDeclaration(ctx, Lib.Bytecode.Class.NumericByteType);
                    return this;
                case TokenType.IdentNumShort:
                    CompileDeclaration(ctx, Lib.Bytecode.Class.NumericShortType);
                    return this;
                case TokenType.IdentNumInt:
                    CompileDeclaration(ctx, Lib.Bytecode.Class.NumericIntType);
                    return this;
                case TokenType.IdentNumLong:
                    CompileDeclaration(ctx, Lib.Bytecode.Class.NumericLongType);
                    return this;
                case TokenType.IdentNumFloat:
                    CompileDeclaration(ctx, Lib.Bytecode.Class.NumericFloatType);
                    return this;
                case TokenType.IdentNumDouble:
                    CompileDeclaration(ctx, Lib.Bytecode.Class.NumericDoubleType);
                    return this;
                case TokenType.IdentStr:
                    CompileDeclaration(ctx, Lib.Bytecode.Class.StringType.DefaultInstance);
                    return this;
                case TokenType.New:
                    if (ctx.NextToken!.Type != TokenType.Word)
                        throw new CompilerException(ctx.Token.SourcefilePosition, "Invalid new-Statement; missing type identifier");
                    ctx.TokenIndex += 1;
                    var ctor = ctx.FindType(vm, ctx.FindCompoundWord(terminator: TokenType.ParRoundOpen))!;
                    if (!ctx.Statement.TargetType.CanHold(ctor))
                        throw new CompilerException(ctx.Token.SourcefilePosition, $"Invalid new-Statement; Cannot assign {ctor} to {ctx.Statement.TargetType}");
                    ctx.Component = new StatementComponent
                    {
                        Type = StatementComponentType.Expression,
                        CodeType = BytecodeType.ConstructorCall,
                        Arg = ctor.FullName,
                        SourcefilePosition = ctx.Token.SourcefilePosition
                    };
                    // method call, parse parameter expressions
                    ctx.TokenIndex += 1;
                    subctx = new CompilerContext(ctx, CompilerType.CodeParameterExpression);
                    CompilerLoop(vm, new ParameterExpressionCompiler(this), ref subctx); 
                    ctx.LastComponent!.SubComponent = subctx.Component;
                    ctx.TokenIndex = subctx.TokenIndex - 1;
                    break;
                case TokenType.Dot:
                    // member call
                    var comp = new StatementComponent
                    {
                        Type = StatementComponentType.Provider,
                        CodeType = BytecodeType.Call,
                        Arg = ctx.NextToken!.Arg!,
                        SourcefilePosition = ctx.Token.SourcefilePosition
                    };
                    ctx.LastComponent!.PostComponent = comp;
                    ctx.TokenIndex += 1;
                    if (ctx.NextToken?.Type == TokenType.ParRoundOpen)
                    {
                        // method call, parse parameter expressions
                        ctx.TokenIndex += 2;
                        subctx = new CompilerContext(ctx, CompilerType.CodeParameterExpression);
                        CompilerLoop(vm, new ParameterExpressionCompiler(this), ref subctx); 
                        ctx.LastComponent!.PostComponent!.SubComponent = subctx.Component;
                        ctx.TokenIndex = subctx.TokenIndex - 1;
                    }
                    break;
                case TokenType.Word:
                    var type = ctx.FindType(vm, ctx.Token.Arg!);
                    //todo: try use class member instead of type

                    if (type != null)
                    { // type expression
                        ctx.Component = new StatementComponent
                        {
                            Type = StatementComponentType.Expression,
                            CodeType = BytecodeType.TypeExpression,
                            Arg = type.FullName,
                            SourcefilePosition = ctx.Token.SourcefilePosition
                        };
                    } else
                    {
                        ctx.Component = new StatementComponent
                        {
                            Type = ctx.Statement.Type == StatementComponentType.Declaration
                                ? StatementComponentType.Declaration
                                : StatementComponentType.Provider,
                            CodeType = BytecodeType.ExpressionVariable,
                            Arg = ctx.Token.Arg!,
                            SourcefilePosition = ctx.Token.SourcefilePosition
                        };
                    }
                    break;
                case TokenType.This:
                    ctx.Component = new StatementComponent
                    {
                        Type = StatementComponentType.Provider,
                        VariableContext = VariableContext.This,
                        SourcefilePosition = ctx.Token.SourcefilePosition
                    };
                    break;
                case TokenType.StdIo:
                    ctx.Component = new StatementComponent
                    {
                        Type = StatementComponentType.Provider,
                        CodeType = BytecodeType.StdioExpression,
                        SourcefilePosition = ctx.Token.SourcefilePosition
                    };
                    break;
                // equality operators
                case TokenType.OperatorEquals:
                    if (ctx.NextToken!.Type != TokenType.OperatorEquals)
                        break;
                    ctx.TokenIndex += 1;
                    ctx.Component = new StatementComponent
                    {
                        Type = StatementComponentType.Operator,
                        ByteArg = (ulong)Operator.Equals,
                        SourcefilePosition = ctx.Token.SourcefilePosition
                    };
                    ctx.NextIntoSub = true;
                    break;
                case TokenType.OperatorPlus:
                    if (ctx.NextToken!.Type is TokenType.OperatorPlus)
                    {
                        if (ctx.PrevToken!.Type is TokenType.Word)
                        {
                            ctx.Component = new StatementComponent
                            {
                                Type = StatementComponentType.Operator,
                                ByteArg = (ulong)Operator.ReadIncrement,
                                SourcefilePosition = ctx.Token.SourcefilePosition
                            };
                            ctx.TokenIndex += 1;
                            break;
                        }
                        ctx.TokenIndex += 1;
                        if (ctx.NextToken!.Type is TokenType.Word)
                        {
                            ctx.Component = new StatementComponent
                            {
                                Type = StatementComponentType.Operator,
                                ByteArg = (ulong)Operator.IncrementRead,
                                SourcefilePosition = ctx.Token.SourcefilePosition
                            };
                            ctx.NextIntoSub = true;
                        } else ctx.TokenIndex -= 1;
                    }
                    else
                    {
                        // simple operator component; special cases handled in ExpressionCompiler
                        ctx.Component = new()
                        {
                            Type = StatementComponentType.Operator,
                            ByteArg = (ulong)Operator.Plus,
                            SourcefilePosition = ctx.Token.SourcefilePosition
                        };
                        ctx.NextIntoSub = true;
                    }

                    break;
                case TokenType.OperatorMinus:
                    /* TODO
                    if (ctx.NextToken!.Type is TokenType.Word or TokenType.LiteralNum)
                    {
                        ctx.Component = new StatementComponent
                        {
                            Type = StatementComponentType.Operator,
                            ByteArg = (ulong)Operator.ArithmeticNot
                        };
                        ctx.NextIntoSub = true;
                    }
                    else
                    */
                    if (ctx.NextToken!.Type is TokenType.OperatorMinus)
                    {
                        if (ctx.PrevToken!.Type is TokenType.Word)
                        {
                            ctx.Component = new StatementComponent
                            {
                                Type = StatementComponentType.Operator,
                                ByteArg = (ulong)Operator.ReadDecrement,
                                SourcefilePosition = ctx.Token.SourcefilePosition
                            };
                            ctx.TokenIndex += 1;
                            break;
                        }
                        ctx.TokenIndex += 1;
                        if (ctx.NextToken!.Type is TokenType.Word)
                        {
                            ctx.Component = new StatementComponent
                            {
                                Type = StatementComponentType.Operator,
                                ByteArg = (ulong)Operator.DecrementRead,
                                SourcefilePosition = ctx.Token.SourcefilePosition
                            };
                            ctx.NextIntoSub = true;
                        } else ctx.TokenIndex -= 1;
                    }
                    else
                    {
                        // simple operator component; special cases handled in ExpressionCompiler
                        ctx.Component = new()
                        {
                            Type = StatementComponentType.Operator,
                            ByteArg = (ulong)Operator.Minus,
                            SourcefilePosition = ctx.Token.SourcefilePosition
                        };
                        ctx.NextIntoSub = true;
                    }
                    break;
                case TokenType.Exclamation:
                    switch (ctx.NextToken!.Type)
                    {
                        case TokenType.OperatorEquals:
                            ctx.TokenIndex += 1;
                            ctx.Component = new StatementComponent
                            {
                                Type = StatementComponentType.Operator,
                                ByteArg = (ulong)Operator.NotEquals,
                                SourcefilePosition = ctx.Token.SourcefilePosition
                            };
                            ctx.NextIntoSub = true;
                            break;
                        case TokenType.Word or TokenType.LiteralFalse or TokenType.LiteralTrue:
                            ctx.Component = new StatementComponent
                            {
                                Type = StatementComponentType.Operator,
                                ByteArg = (ulong)Operator.LogicalNot,
                                SourcefilePosition = ctx.Token.SourcefilePosition
                            };
                            ctx.NextIntoSub = true;
                            break;
                    }
                    break;
                case TokenType.OperatorMultiply:
                case TokenType.OperatorDivide:
                case TokenType.OperatorModulus:
                case TokenType.Circumflex:
                    // simple operator component; special cases handled in ExpressionCompiler
                    ctx.Component = new()
                    {
                        Type = StatementComponentType.Operator,
                        ByteArg = (ulong)(ctx.Token.Type switch
                        {
                            TokenType.OperatorPlus => Operator.Plus,
                            TokenType.OperatorMinus => Operator.Minus,
                            TokenType.OperatorMultiply => Operator.Multiply,
                            TokenType.OperatorDivide => Operator.Divide,
                            TokenType.OperatorModulus => Operator.Modulus,
                            TokenType.Circumflex => Operator.Circumflex,
                            _ => throw new ArgumentOutOfRangeException()
                        }),
                        SourcefilePosition = ctx.Token.SourcefilePosition
                    };
                    ctx.NextIntoSub = true;
                    break;
                // pipe operands
                case TokenType.ParDiamondOpen:
                    if (ctx.NextToken!.Type == TokenType.ParDiamondOpen)
                    {
                        // compile Emitter
                        ctx.Component = new StatementComponent
                        {
                            Type = StatementComponentType.Emitter,
                            SourcefilePosition = ctx.Token.SourcefilePosition
                        };
                        ctx.TokenIndex += 2;
                        subctx = new CompilerContext(ctx, CompilerType.PipeEmitter);
                        subctx.Statement = new Statement
                        {
                            TargetType = Lib.Bytecode.Class.VoidType.DefaultInstance,
                            Type = StatementComponentType.Expression
                        };
                        CompilerLoop(vm, new ExpressionCompiler(this, false,
                            TokenType.Terminator, TokenType.ParDiamondOpen, TokenType.ParDiamondClose), ref subctx);
                        ctx.LastComponent!.SubStatement = subctx.Statement;
                        ctx.TokenIndex = subctx.TokenIndex - 1;
                    }
                    else
                    {
                        if (ctx.NextToken!.Type == TokenType.ParDiamondOpen)
                            break;
                        ctx.Component = new StatementComponent
                        {
                            Type = StatementComponentType.Operator,
                            ByteArg = (ulong)(ctx.NextToken!.Type == TokenType.OperatorEquals ? Operator.LesserEq : Operator.Lesser),
                            SourcefilePosition = ctx.Token.SourcefilePosition
                        };
                        ctx.NextIntoSub = true;
                    }

                    break;
                case TokenType.ParDiamondClose:
                    if (ctx.NextToken!.Type == TokenType.ParDiamondClose)
                    {
                        // compile Emitter
                        ctx.Component = new StatementComponent
                        {
                            Type = StatementComponentType.Consumer,
                            SourcefilePosition = ctx.Token.SourcefilePosition
                        };
                        ctx.TokenIndex += 2;
                        subctx = new CompilerContext(ctx, CompilerType.PipeConsumer);
                        CompilerLoop(vm, new ExpressionCompiler(this, false,
                            TokenType.Terminator, TokenType.ParDiamondOpen, TokenType.ParDiamondClose), ref subctx);
                        ctx.LastComponent!.SubStatement = subctx.Statement;
                        ctx.TokenIndex = subctx.TokenIndex - 1;
                    }
                    else
                    {
                        ctx.Component = new StatementComponent
                        {
                            Type = StatementComponentType.Operator,
                            ByteArg = (ulong)(ctx.NextToken!.Type == TokenType.OperatorEquals ? Operator.GreaterEq : Operator.Greater),
                            SourcefilePosition = ctx.Token.SourcefilePosition
                        };
                        ctx.NextIntoSub = true;
                    }

                    break;
                case TokenType.ParAccClose:
                case TokenType.Terminator:
                    /*
                    ctx.Component = new StatementComponent
                    {
                        Type = StatementComponentType.Provider,
                        CodeType = BytecodeType.Statement,
                        VariableContext = VariableContext.This
                    };
                    */
                    return this;
            }

            return this;
        }

        private static void CompileDeclaration(CompilerContext ctx, IClassInstance targetType)
        {
            if (ctx.NextToken?.Type != TokenType.Word)
                throw new CompilerException(ctx.Token.SourcefilePosition, "Invalid declaration; missing variable name");

            ctx.Statement = new Statement
            {
                Type = StatementComponentType.Declaration,
                TargetType = targetType
            }; /*
            ctx.Component = new StatementComponent
            {
                Type = StatementComponentType.Declaration,
                Arg = ctx.NextToken!.Arg!
            };*/
        }
    }
}