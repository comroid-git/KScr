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
                    CompileDeclaration(ctx, Lib.Bytecode.Class.VoidType);
                    return this;
                case TokenType.IdentNum:
                    CompileDeclaration(ctx, Lib.Bytecode.Class.NumericType);
                    return this;
                case TokenType.IdentNumByte:
                    CompileDeclaration(ctx, Lib.Bytecode.Class.NumericByteType);
                    return this;
                case TokenType.IdentNumShort:
                    CompileDeclaration(ctx, Lib.Bytecode.Class.NumericShortType);
                    return this;
                case TokenType.IdentNumInt:
                    CompileDeclaration(ctx, Lib.Bytecode.Class.NumericIntegerType);
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
                    CompileDeclaration(ctx, Lib.Bytecode.Class.StringType);
                    return this;
                case TokenType.Dot:
                    // field call
                    ctx.Component = new StatementComponent
                    {
                        Type = StatementComponentType.Provider,
                        CodeType = BytecodeType.Call,
                        Arg = ctx.NextToken!.Arg!
                    };
                    ctx.TokenIndex += 1;
                    if (ctx.NextToken?.Type == TokenType.ParRoundOpen)
                    {
                        // method call, parse parameter expressions
                        subctx = new CompilerContext(ctx, CompilerType.CodeParameterExpression);
                        CompilerLoop(vm, new ParameterExpressionCompiler(this), ref subctx);
                        ctx.LastComponent!.SubComponent = subctx.Component;
                        ctx.TokenIndex = subctx.TokenIndex;
                    }

                    break;
                case TokenType.Word:
                    ctx.Component = new StatementComponent
                    {
                        Type = ctx.Statement.Type == StatementComponentType.Declaration
                            ? StatementComponentType.Declaration
                            : StatementComponentType.Provider,
                        CodeType = BytecodeType.ExpressionVariable,
                        Arg = ctx.Token.Arg!
                    };
                    break;
                case TokenType.This:
                    ctx.Component = new StatementComponent
                    {
                        Type = StatementComponentType.Provider,
                        VariableContext = VariableContext.This
                    };
                    break;
                case TokenType.StdIo:
                    ctx.Component = new StatementComponent
                    {
                        Type = StatementComponentType.Provider,
                        CodeType = BytecodeType.StdioExpression
                    };
                    break;
                case TokenType.OperatorPlus:
                case TokenType.OperatorMinus:
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
                        })
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
                            Type = StatementComponentType.Emitter
                        };
                        ctx.TokenIndex += 2;
                        subctx = new CompilerContext(ctx, CompilerType.PipeEmitter);
                        subctx.Statement = new Statement
                        {
                            TargetType = Lib.Bytecode.Class.VoidType,
                            Type = StatementComponentType.Expression
                        };
                        CompilerLoop(vm, new ExpressionCompiler(this, false,
                            TokenType.Terminator, TokenType.ParDiamondOpen, TokenType.ParDiamondClose), ref subctx);
                        ctx.LastComponent!.SubStatement = subctx.Statement;
                        ctx.TokenIndex = subctx.TokenIndex - 1;
                    }

                    break;
                case TokenType.ParDiamondClose:
                    if (ctx.NextToken!.Type == TokenType.ParDiamondClose)
                    {
                        // compile Emitter
                        ctx.Component = new StatementComponent
                        {
                            Type = StatementComponentType.Consumer
                        };
                        ctx.TokenIndex += 2;
                        subctx = new CompilerContext(ctx, CompilerType.PipeConsumer);
                        CompilerLoop(vm, new ExpressionCompiler(this, false,
                            TokenType.Terminator, TokenType.ParDiamondOpen, TokenType.ParDiamondClose), ref subctx);
                        ctx.LastComponent!.SubStatement = subctx.Statement;
                        ctx.TokenIndex = subctx.TokenIndex - 1;
                    }

                    break;
                case TokenType.Terminator:
                    ctx.Statement = new Statement();
                    return this;
            }

            return this;
        }

        private static void CompileDeclaration(CompilerContext ctx, IClassInstance targetType)
        {
            if (ctx.NextToken?.Type != TokenType.Word)
                throw new CompilerException("Invalid declaration; missing variable name");

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