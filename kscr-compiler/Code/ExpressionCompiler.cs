﻿using KScr.Lib;
using KScr.Lib.Bytecode;
using KScr.Lib.Core;
using KScr.Lib.Exception;
using KScr.Lib.Model;

namespace KScr.Compiler.Code
{
    public class ExpressionCompiler : AbstractCodeCompiler
    {
        public ExpressionCompiler(ICompiler parent) : base(parent)
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
                    ICompiler sub = new ExpressionCompiler(this);
                    var subctx = new CompilerContext(ctx, CompilerType.CodeExpression);
                    CompilerLoop(vm, ref sub, ref subctx);
                    ctx.Component.SubComponent = subctx.Component;
                    ctx.TokenIndex = subctx.TokenIndex;

                    return Parent;
                case TokenType.LiteralNull:
                    ctx.Component = new StatementComponent
                    {
                        Type = StatementComponentType.Expression,
                        CodeType = BytecodeType.Null
                    };
                    break;
                case TokenType.LiteralNum:
                    if (!ctx.Statement.TargetType.CanHold(Lib.Bytecode.Class.NumericType))
                        throw new CompilerException("Invalid Numeric literal; expected " + ctx.Statement.TargetType);
                    ctx.Component = new StatementComponent
                    {
                        Type = StatementComponentType.Expression,
                        CodeType = BytecodeType.LiteralNumeric,
                        Arg = Numeric.Compile(vm, ctx.Token.Arg!).ToString()
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
                case TokenType.Terminator:
                    // todo finalize & push expression
                    _active = false;
                    return Parent;
            }

            return base.AcceptToken(vm, ref ctx);
        }
    }
}