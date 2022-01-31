using KScr.Compiler.Class;
using KScr.Lib;
using KScr.Lib.Bytecode;
using KScr.Lib.Core;
using KScr.Lib.Exception;
using KScr.Lib.Model;
using KScr.Lib.Store;

namespace KScr.Compiler.Code
{
    public abstract class AbstractCodeCompiler : AbstractCompiler
    {
        protected bool _active = true;

        public override bool Active => _active;

        protected AbstractCodeCompiler(ICompiler parent) : base(parent)
        {
        }

        public override ICompiler? AcceptToken(RuntimeBase vm, ref CompilerContext ctx)
        {
            switch (ctx.Token.Type)
            {
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
                        var subctx = new CompilerContext(ctx, CompilerType.CodeParameterExpression);
                        CompilerLoop(vm, new ParameterExpressionCompiler(this), ref subctx);

                        ctx.Component.SubComponent = subctx.Component;
                        ctx.TokenIndex = subctx.TokenIndex;
                    }

                    break;
                case TokenType.Word:
                    ctx.Component = new StatementComponent
                    {
                        Type = StatementComponentType.Provider,
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
                case TokenType.OperatorPlus:
                case TokenType.OperatorMinus:
                case TokenType.OperatorMultiply:
                case TokenType.OperatorDivide:
                case TokenType.OperatorModulus:
                    // simple operator component
                    ctx.Component = new StatementComponent
                    {
                        Type = StatementComponentType.Operator,
                        Arg = ctx.Token.String()
                    };
                    break;
            }

            return this;
        }
    }
}