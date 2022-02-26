using System;
using System.Linq;
using KScr.Lib;
using KScr.Lib.Bytecode;
using KScr.Lib.Core;
using KScr.Lib.Exception;
using KScr.Lib.Model;

namespace KScr.Compiler.Code
{
    public class ExpressionCompiler : AbstractCodeCompiler
    {
        private readonly bool _endBeforeTerminator;
        private readonly TokenType[] _terminators;

        public ExpressionCompiler(ICompiler parent, bool endBeforeTerminator = false, params TokenType[] terminators) :
            base(parent)
        {
            _endBeforeTerminator = endBeforeTerminator;
            _terminators = terminators;
        }

        public ExpressionCompiler(ICompiler parent, bool endBeforeTerminator = false,
            TokenType terminator = TokenType.Terminator)
            : this(parent, endBeforeTerminator, new[] { terminator })
        {
        }

        public override ICompiler? AcceptToken(RuntimeBase vm, ref CompilerContext ctx)
        {
            switch (ctx.Token.Type)
            {
                case TokenType.ParRoundOpen:
                    ctx.Component = new StatementComponent
                    {
                        Type = StatementComponentType.Expression,
                        CodeType = BytecodeType.Parentheses
                    };

                    // compile inner expression
                    var subctx = new CompilerContext(ctx, CompilerType.CodeExpression);
                    subctx.TokenIndex += 1;
                    subctx.Statement = new Statement
                    {
                        Type = StatementComponentType.Expression,
                        TargetType = ctx.Statement.TargetType
                    };
                    CompilerLoop(vm, new ExpressionCompiler(this), ref subctx);
                    ctx.LastComponent!.SubStatement = subctx.Statement;
                    ctx.TokenIndex = subctx.TokenIndex - 1;
                    // finished
                    _active = false;
                    return this;
                case TokenType.LiteralNull:
                    ctx.Component = new StatementComponent
                    {
                        Type = StatementComponentType.Expression,
                        CodeType = BytecodeType.Null
                    };
                    break;
                case TokenType.LiteralNum:
                    if (ctx.NextToken?.Type == TokenType.Tilde)
                        return this; // parse ranges completely
                    if (!ctx.Statement.TargetType.CanHold(Lib.Bytecode.Class.NumericType))
                        throw new CompilerException("Invalid Numeric literal; expected " + ctx.Statement.TargetType);
                    var numstr = Numeric.Compile(vm, ctx.Token.Arg!).Value!.ToString(IObject.ToString_LongName);
                    ctx.Component = new StatementComponent
                    {
                        Type = StatementComponentType.Expression,
                        CodeType = BytecodeType.LiteralNumeric,
                        Arg = numstr
                    };
                    break;
                case TokenType.LiteralStr:
                    if (!ctx.Statement.TargetType.CanHold(Lib.Bytecode.Class.StringType))
                        throw new CompilerException("Invalid Numeric literal; expected " + ctx.Statement.TargetType);
                    ctx.Component = new StatementComponent
                    {
                        Type = StatementComponentType.Expression,
                        CodeType = BytecodeType.LiteralString,
                        Arg = ctx.Token.Arg!
                    };
                    break;
                case TokenType.LiteralTrue:
                    if (!ctx.Statement.TargetType.CanHold(Lib.Bytecode.Class.NumericType))
                        throw new CompilerException("Invalid Numeric literal; expected " + ctx.Statement.TargetType);
                    ctx.Component = new StatementComponent
                    {
                        Type = StatementComponentType.Expression,
                        CodeType = BytecodeType.LiteralTrue
                    };
                    break;
                case TokenType.LiteralFalse:
                    if (!ctx.Statement.TargetType.CanHold(Lib.Bytecode.Class.NumericType))
                        throw new CompilerException("Invalid Numeric literal; expected " + ctx.Statement.TargetType);
                    ctx.Component = new StatementComponent
                    {
                        Type = StatementComponentType.Expression,
                        CodeType = BytecodeType.LiteralFalse
                    };
                    break;
                // range literal
                case TokenType.Tilde:
                    int start = -1, end = -1;
                    if (ctx.PrevToken?.Type == TokenType.LiteralNum)
                        start = int.Parse(ctx.PrevToken.Arg!);
                    if (ctx.NextToken?.Type == TokenType.LiteralNum)
                        end = int.Parse(ctx.NextToken.Arg!);
                    var sb = BitConverter.GetBytes(start);
                    var eb = BitConverter.GetBytes(end);
                    ctx.Component = new StatementComponent
                    {
                        Type = StatementComponentType.Expression,
                        CodeType = BytecodeType.LiteralRange,
                        ByteArg = BitConverter.ToUInt64(new[]{
                            sb[0],sb[1],sb[2],sb[3],
                            eb[0],eb[1],eb[2],eb[3]
                        })
                    };
                    if (end != -1)
                        ctx.TokenIndex += 1;
                    break;
                // equality operators
                case TokenType.ParDiamondOpen:
                    if (ctx.NextToken!.Type == TokenType.ParDiamondOpen)
                        break;
                    ctx.Component = new StatementComponent
                    {
                        Type = StatementComponentType.Operator,
                        ByteArg = (ulong)(ctx.NextToken!.Type == TokenType.OperatorEquals ? Operator.LesserEq : Operator.Lesser)
                    };
                    ctx.NextIntoSub = true;
                    break;
                case TokenType.ParDiamondClose:
                    if (ctx.NextToken!.Type == TokenType.ParDiamondClose)
                        break;
                    ctx.Component = new StatementComponent
                    {
                        Type = StatementComponentType.Operator,
                        ByteArg = (ulong)(ctx.NextToken!.Type == TokenType.OperatorEquals ? Operator.GreaterEq : Operator.Greater)
                    };
                    ctx.NextIntoSub = true;
                    break;
                case TokenType.OperatorEquals:
                    if (ctx.NextToken!.Type != TokenType.OperatorEquals)
                        break;
                    ctx.TokenIndex += 1;
                    ctx.Component = new StatementComponent
                    {
                        Type = StatementComponentType.Operator,
                        ByteArg = (ulong)Operator.Equals
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
                                ByteArg = (ulong)Operator.ReadIncrement
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
                                ByteArg = (ulong)Operator.IncrementRead
                            };
                            ctx.NextIntoSub = true;
                        } else ctx.TokenIndex -= 1;
                    }
                    break;
                case TokenType.OperatorMinus:
                    if (ctx.NextToken!.Type is TokenType.Word or TokenType.LiteralNum)
                    {
                        ctx.Component = new StatementComponent
                        {
                            Type = StatementComponentType.Operator,
                            ByteArg = (ulong)Operator.ArithmeticNot
                        };
                        ctx.NextIntoSub = true;
                    }
                    else if (ctx.NextToken!.Type is TokenType.OperatorMinus)
                    {
                        if (ctx.PrevToken!.Type is TokenType.Word)
                        {
                            ctx.Component = new StatementComponent
                            {
                                Type = StatementComponentType.Operator,
                                ByteArg = (ulong)Operator.ReadDecrement
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
                                ByteArg = (ulong)Operator.DecrementRead
                            };
                            ctx.NextIntoSub = true;
                        } else ctx.TokenIndex -= 1;
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
                                ByteArg = (ulong)Operator.NotEquals
                            };
                            ctx.NextIntoSub = true;
                            break;
                        case TokenType.Word or TokenType.LiteralFalse or TokenType.LiteralTrue:
                            ctx.Component = new StatementComponent
                            {
                                Type = StatementComponentType.Operator,
                                ByteArg = (ulong)Operator.LogicalNot
                            };
                            ctx.NextIntoSub = true;
                            break;
                    }
                    break;
            }

            var use = base.AcceptToken(vm, ref ctx);
            if (_terminators.Contains(ctx.NextToken?.Type ?? TokenType.Terminator))
            {
                if (_endBeforeTerminator)
                    ctx.TokenIndex -= 1;
                _active = false;
                return _endBeforeTerminator ? Parent : this;
            }

            return use;
        }
    }
}