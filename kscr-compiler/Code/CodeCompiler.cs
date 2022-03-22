using System;
using System.Linq;
using KScr.Lib;
using KScr.Lib.Bytecode;
using KScr.Lib.Core;
using KScr.Lib.Exception;
using KScr.Lib.Model;
using KScr.Lib.Store;
using static KScr.Lib.Model.TokenType;

namespace KScr.Compiler.Code
{
    public abstract class AbstractCodeCompiler : AbstractCompiler
    {
        private readonly bool _endBeforeTerminator;
        private readonly TokenType[] _terminators;
        protected bool _active = true;

        protected AbstractCodeCompiler(ICompiler parent, bool endBeforeTerminator, TokenType[] terminators) :
            base(parent)
        {
            _endBeforeTerminator = endBeforeTerminator;
            _terminators = terminators;
        }

        public override bool Active => _active;

        public override ICompiler? AcceptToken(RuntimeBase vm, ref CompilerContext ctx)
        {
            if (ctx.TokenIndex >= ctx.Tokens.Count)
            {
                _active = false;
                return this;
            }

            // todo do not handle token if any token was handled in super
            CompilerContext subctx;
            switch (ctx.Token.Type)
            {
                /*case TokenType.IdentVoid:
                    CompileDeclaration(ctx, Lib.Bytecode.Class.VoidType.DefaultInstance);
                    return this;*/
                case IdentNum:
                    CompileDeclaration(ctx, Lib.Bytecode.Class.NumericType.DefaultInstance);
                    return this;
                case IdentNumByte:
                    CompileDeclaration(ctx, Lib.Bytecode.Class.NumericByteType);
                    return this;
                case IdentNumShort:
                    CompileDeclaration(ctx, Lib.Bytecode.Class.NumericShortType);
                    return this;
                case IdentNumInt:
                    CompileDeclaration(ctx, Lib.Bytecode.Class.NumericIntType);
                    return this;
                case IdentNumLong:
                    CompileDeclaration(ctx, Lib.Bytecode.Class.NumericLongType);
                    return this;
                case IdentNumFloat:
                    CompileDeclaration(ctx, Lib.Bytecode.Class.NumericFloatType);
                    return this;
                case IdentNumDouble:
                    CompileDeclaration(ctx, Lib.Bytecode.Class.NumericDoubleType);
                    return this;
                case IdentStr:
                    CompileDeclaration(ctx, Lib.Bytecode.Class.StringType.DefaultInstance);
                    return this;
                case New:
                    if (ctx.NextToken!.Type != Word)
                        throw new CompilerException(ctx.Token.SourcefilePosition,
                            "Invalid new-Statement; missing type identifier");
                    ctx.TokenIndex += 1;
                    var ctor = ctx.FindType(vm, ctx.FindCompoundWord(terminator: ParRoundOpen))!;
                    if (!ctx.Statement.TargetType.CanHold(ctor))
                        throw new CompilerException(ctx.Token.SourcefilePosition,
                            $"Invalid new-Statement; Cannot assign {ctor} to {ctx.Statement.TargetType}");
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
                case Dot:
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
                    if (ctx.NextToken?.Type == ParRoundOpen)
                    {
                        // method call, parse parameter expressions
                        ctx.TokenIndex += 2;
                        subctx = new CompilerContext(ctx, CompilerType.CodeParameterExpression);
                        CompilerLoop(vm, new ParameterExpressionCompiler(this), ref subctx);
                        ctx.LastComponent!.PostComponent!.SubComponent = subctx.Component;
                        ctx.TokenIndex = subctx.TokenIndex - 1;
                    }

                    break;
                case Word:
                    var type = ctx.FindType(vm, ctx.Token.Arg!);
                    //todo: try use class member instead of type

                    if (type != null)
                    {
                        if (ctx.NextToken?.Type == Word)
                            // declaration
                            CompileDeclaration(ctx, type);
                        else
                            // type expression
                            ctx.Component = new StatementComponent
                            {
                                Type = StatementComponentType.Expression,
                                CodeType = BytecodeType.TypeExpression,
                                Arg = type.FullName,
                                SourcefilePosition = ctx.Token.SourcefilePosition
                            };
                    }
                    else
                    {
                        ctx.Component = new StatementComponent
                        {
                            Type = ctx.PrevToken?.Type is Word
                                or IdentNum
                                or IdentNumByte
                                or IdentNumShort
                                or IdentNumInt
                                or IdentNumLong
                                or IdentNumFloat
                                or IdentNumDouble
                                or IdentStr
                                or IdentVar
                                or IdentVoid
                                ? StatementComponentType.Declaration
                                : StatementComponentType.Provider,
                            CodeType = BytecodeType.ExpressionVariable,
                            Arg = ctx.Token.Arg!,
                            SourcefilePosition = ctx.Token.SourcefilePosition
                        };
                    }

                    break;
                case ParRoundOpen:
                    ctx.Component = new StatementComponent
                    {
                        Type = StatementComponentType.Expression,
                        CodeType = BytecodeType.Parentheses,
                        SourcefilePosition = ctx.Token.SourcefilePosition
                    };

                    // compile inner expression
                    subctx = new CompilerContext(ctx, CompilerType.CodeExpression);
                    subctx.TokenIndex += 1;
                    subctx.Statement = new Statement
                    {
                        Type = StatementComponentType.Expression,
                        TargetType = ctx.Statement.TargetType
                    };
                    CompilerLoop(vm, new ExpressionCompiler(this, false, ParRoundClose), ref subctx);
                    ctx.LastComponent!.SubStatement = subctx.Statement;
                    ctx.TokenIndex = subctx.TokenIndex;
                    // finished
                    //_active = false;
                    return this;
                case LiteralNull:
                    ctx.Component = new StatementComponent
                    {
                        Type = StatementComponentType.Expression,
                        CodeType = BytecodeType.Null,
                        SourcefilePosition = ctx.Token.SourcefilePosition
                    };
                    break;
                case LiteralNum:
                    //if (ctx.NextToken?.Type == TokenType.Tilde)
                    //    return this; // parse ranges completely
                    if (ctx.PrevToken?.Type != Tilde // fixme todo this may break with non-compile time constant range
                        && ctx.NextToken?.Type != Tilde 
                        && !ctx.Statement.TargetType.CanHold(Lib.Bytecode.Class.NumericType))
                        throw new CompilerException(ctx.Token.SourcefilePosition,
                            "Invalid Numeric literal; expected " + ctx.Statement.TargetType);
                    var numstr = Numeric.Compile(vm, ctx.Token.Arg!).Value!.ToString(IObject.ToString_LongName);
                    ctx.Component = new StatementComponent
                    {
                        Type = StatementComponentType.Expression,
                        CodeType = BytecodeType.LiteralNumeric,
                        Arg = numstr,
                        SourcefilePosition = ctx.Token.SourcefilePosition
                    };
                    break;
                case LiteralStr:
                    if (!ctx.Statement.TargetType.CanHold(Lib.Bytecode.Class.StringType))
                        throw new CompilerException(ctx.Token.SourcefilePosition,
                            "Invalid String literal; expected " + ctx.Statement.TargetType);
                    ctx.Component = new StatementComponent
                    {
                        Type = StatementComponentType.Expression,
                        CodeType = BytecodeType.LiteralString,
                        Arg = ctx.Token.Arg!,
                        SourcefilePosition = ctx.Token.SourcefilePosition
                    };
                    break;
                case LiteralTrue:
                    if (!ctx.Statement.TargetType.CanHold(Lib.Bytecode.Class.NumericType))
                        throw new CompilerException(ctx.Token.SourcefilePosition,
                            "Invalid Boolean literal; expected " + ctx.Statement.TargetType);
                    ctx.Component = new StatementComponent
                    {
                        Type = StatementComponentType.Expression,
                        CodeType = BytecodeType.LiteralTrue,
                        SourcefilePosition = ctx.Token.SourcefilePosition
                    };
                    break;
                case LiteralFalse:
                    if (!ctx.Statement.TargetType.CanHold(Lib.Bytecode.Class.NumericType))
                        throw new CompilerException(ctx.Token.SourcefilePosition,
                            "Invalid Boolean literal; expected " + ctx.Statement.TargetType);
                    ctx.Component = new StatementComponent
                    {
                        Type = StatementComponentType.Expression,
                        CodeType = BytecodeType.LiteralFalse,
                        SourcefilePosition = ctx.Token.SourcefilePosition
                    };
                    break;
                // range literal
                case Tilde:
                    ctx.Component = new StatementComponent
                    {
                        Type = StatementComponentType.Provider,
                        CodeType = BytecodeType.LiteralRange,
                        SourcefilePosition = ctx.Token.SourcefilePosition
                    };
                    ctx.NextIntoSub = true;
                    break;
                case This:
                    ctx.Component = new StatementComponent
                    {
                        Type = StatementComponentType.Provider,
                        VariableContext = VariableContext.This,
                        SourcefilePosition = ctx.Token.SourcefilePosition
                    };
                    break;
                case StdIo:
                    ctx.Component = new StatementComponent
                    {
                        Type = StatementComponentType.Provider,
                        CodeType = BytecodeType.StdioExpression,
                        SourcefilePosition = ctx.Token.SourcefilePosition
                    };
                    break;
                // equality operators
                case OperatorEquals:
                    if (ctx.Statement.Type == StatementComponentType.Declaration ||
                             ctx.Component.CodeType == BytecodeType.ExpressionVariable ||
                             ctx.Component.Type == StatementComponentType.Provider && 
                             ctx.NextToken?.Type is not OperatorEquals or ParDiamondClose or null)
                    {
                        // assignment
                        ctx.Component = new StatementComponent
                        {
                            Type = StatementComponentType.Setter,
                            SourcefilePosition = ctx.Token.SourcefilePosition
                        };

                        ctx.TokenIndex += 1;
                        // compile expression
                        subctx = new CompilerContext(ctx, CompilerType.CodeExpression);
                        subctx.Statement = new Statement
                        {
                            Type = StatementComponentType.Expression,
                            TargetType = ctx.Statement.TargetType
                        };
                        CompilerLoop(vm, new ExpressionCompiler(this), ref subctx);
                        ctx.LastComponent!.SubStatement = subctx.Statement;
                        ctx.TokenIndex = subctx.TokenIndex - 1;
                    } else if (ctx.NextToken!.Type == OperatorEquals)
                    {
                        ctx.TokenIndex += 1;
                        ctx.Component = new StatementComponent
                        {
                            Type = StatementComponentType.Operator,
                            ByteArg = (ulong) Operator.Equals,
                            SourcefilePosition = ctx.Token.SourcefilePosition
                        };
                        ctx.NextIntoSub = true;
                    }
                    break;
                case OperatorPlus:
                    if (ctx.NextToken!.Type is OperatorPlus)
                    {
                        if (ctx.PrevToken!.Type is Word)
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
                        if (ctx.NextToken!.Type is Word)
                        {
                            ctx.Component = new StatementComponent
                            {
                                Type = StatementComponentType.Operator,
                                ByteArg = (ulong)Operator.IncrementRead,
                                SourcefilePosition = ctx.Token.SourcefilePosition
                            };
                            ctx.NextIntoSub = true;
                        }
                        else
                        {
                            ctx.TokenIndex -= 1;
                        }
                    }
                    else if (ctx.NextToken!.Type == OperatorEquals)
                    {
                        // simple operator component
                        ctx.Component = new StatementComponent
                        {
                            Type = StatementComponentType.Operator,
                            ByteArg = (ulong)(Operator.Plus | Operator.Compound),
                            SourcefilePosition = ctx.Token.SourcefilePosition
                        };
                        ctx.TokenIndex += 1;
                        ctx.NextIntoSub = true;
                    }
                    else
                    {
                        // simple operator component
                        ctx.Component = new StatementComponent
                        {
                            Type = StatementComponentType.Operator,
                            ByteArg = (ulong)Operator.Plus,
                            SourcefilePosition = ctx.Token.SourcefilePosition
                        };
                        ctx.NextIntoSub = true;
                    }

                    break;
                case OperatorMinus:
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
                    if (ctx.NextToken!.Type is OperatorMinus)
                    {
                        if (ctx.PrevToken!.Type is Word)
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
                        if (ctx.NextToken!.Type is Word)
                        {
                            ctx.Component = new StatementComponent
                            {
                                Type = StatementComponentType.Operator,
                                ByteArg = (ulong)Operator.DecrementRead,
                                SourcefilePosition = ctx.Token.SourcefilePosition
                            };
                            ctx.NextIntoSub = true;
                        }
                        else
                        {
                            ctx.TokenIndex -= 1;
                        }
                    }
                    else if (ctx.NextToken!.Type == OperatorEquals)
                    {
                        // simple operator component
                        ctx.Component = new StatementComponent
                        {
                            Type = StatementComponentType.Operator,
                            ByteArg = (ulong)(Operator.Minus | Operator.Compound),
                            SourcefilePosition = ctx.Token.SourcefilePosition
                        };
                        ctx.TokenIndex += 1;
                        ctx.NextIntoSub = true;
                    }
                    else
                    {
                        // simple operator component; special cases handled in ExpressionCompiler
                        ctx.Component = new StatementComponent
                        {
                            Type = StatementComponentType.Operator,
                            ByteArg = (ulong)Operator.Minus,
                            SourcefilePosition = ctx.Token.SourcefilePosition
                        };
                        ctx.NextIntoSub = true;
                    }

                    break;
                case Exclamation:
                    switch (ctx.NextToken!.Type)
                    {
                        case OperatorEquals:
                            ctx.TokenIndex += 1;
                            ctx.Component = new StatementComponent
                            {
                                Type = StatementComponentType.Operator,
                                ByteArg = (ulong)Operator.NotEquals,
                                SourcefilePosition = ctx.Token.SourcefilePosition
                            };
                            ctx.NextIntoSub = true;
                            break;
                        case Word or LiteralFalse or LiteralTrue:
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
                case OperatorMultiply:
                case OperatorDivide:
                case OperatorModulus:
                case Circumflex:
                    // simple operator component; special cases handled in ExpressionCompiler
                    ctx.Component = new StatementComponent
                    {
                        Type = StatementComponentType.Operator,
                        ByteArg = (ulong)(ctx.Token.Type switch
                        {
                            OperatorPlus => Operator.Plus,
                            OperatorMinus => Operator.Minus,
                            OperatorMultiply => Operator.Multiply,
                            OperatorDivide => Operator.Divide,
                            OperatorModulus => Operator.Modulus,
                            Circumflex => Operator.Circumflex,
                            _ => throw new ArgumentOutOfRangeException()
                        } | (ctx.NextToken!.Type == OperatorEquals ? Operator.Compound : Operator.Unknown)),
                        SourcefilePosition = ctx.Token.SourcefilePosition
                    };
                    if (ctx.NextToken!.Type == OperatorEquals)
                        ctx.TokenIndex += 1;
                    ctx.NextIntoSub = true;
                    break;
                // pipe operators
                case ParDiamondOpen:
                    if (ctx.NextToken!.Type == ParDiamondOpen)
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
                            Terminator, ParDiamondOpen, ParDiamondClose), ref subctx);
                        ctx.LastComponent!.SubStatement = subctx.Statement;
                        ctx.TokenIndex = subctx.TokenIndex - 1;
                    }
                    else
                    {
                        if (ctx.NextToken!.Type == ParDiamondOpen)
                            break;
                        ctx.Component = new StatementComponent
                        {
                            Type = StatementComponentType.Operator,
                            ByteArg = (ulong)(ctx.NextToken!.Type == OperatorEquals
                                ? Operator.LesserEq
                                : Operator.Lesser),
                            SourcefilePosition = ctx.Token.SourcefilePosition
                        };
                        ctx.NextIntoSub = true;
                    }

                    break;
                case ParDiamondClose:
                    if (ctx.NextToken!.Type == ParDiamondClose)
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
                            Terminator, ParDiamondOpen, ParDiamondClose), ref subctx);
                        ctx.LastComponent!.SubStatement = subctx.Statement;
                        ctx.TokenIndex = subctx.TokenIndex - 1;
                    }
                    else
                    {
                        ctx.Component = new StatementComponent
                        {
                            Type = StatementComponentType.Operator,
                            ByteArg = (ulong)(ctx.NextToken!.Type == OperatorEquals
                                ? Operator.GreaterEq
                                : Operator.Greater),
                            SourcefilePosition = ctx.Token.SourcefilePosition
                        };
                        ctx.NextIntoSub = true;
                    }

                    break;
                case ParAccClose:
                case Terminator:
                    if (ctx.Statement.Type == StatementComponentType.Undefined
                        && ctx.Statement.CodeType == BytecodeType.Undefined
                        && ctx.Statement.Main.Count > 0)
                    {
                        ctx.Statement.Type = StatementComponentType.Code;
                        ctx.Statement.CodeType = BytecodeType.Statement;
                    }

                    if (!_terminators.Contains(ctx.NextToken!.Type)
                        && (ctx.Statement.Type != StatementComponentType.Undefined
                            || ctx.Statement.CodeType != BytecodeType.Undefined))
                        ctx.Statement = new Statement();
                    /*
                    ctx.Component = new StatementComponent
                    {
                        Type = StatementComponentType.Provider,
                        CodeType = BytecodeType.Statement,
                        VariableContext = VariableContext.This
                    };
                    */
                    break;
            }

            if (_terminators.Contains(ctx.NextToken!.Type))
            {
                if (_endBeforeTerminator)
                    ctx.TokenIndex -= 1;
                _active = false;
            }

            return this;
        }

        private static void CompileDeclaration(CompilerContext ctx, IClassInstance targetType)
        {
            if (ctx.NextToken?.Type != Word)
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