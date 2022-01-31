using KScr.Lib;
using KScr.Lib.Bytecode;
using KScr.Lib.Exception;
using KScr.Lib.Model;

namespace KScr.Compiler.Code
{
    public sealed class ParameterExpressionCompiler : AbstractCompiler
    {
        public ParameterExpressionCompiler(ICompiler parent) : base(parent)
        {
        }

        public override ICompiler? AcceptToken(RuntimeBase vm, ref CompilerContext ctx)
        {
            MethodParameterComponent mpc;
            ctx.Component = mpc = new MethodParameterComponent
            {
                Type = StatementComponentType.Code,
                CodeType = BytecodeType.ParameterExpression
            };
            //ctx.TokenIndex -= 1;

            while (ctx.Token.Type != TokenType.ParRoundClose)
            {
                CompilerContext subctx = new CompilerContext(ctx, CompilerType.CodeParameterExpression);
                CompilerLoop(vm, new ExpressionCompiler(this, TokenType.Comma), ref subctx);
                mpc.Expressions.Add(subctx.Component);
                ctx.TokenIndex = subctx.TokenIndex;

                if (ctx.Token.Type != TokenType.Comma)
                    throw new CompilerException("Invalid expression delimiter in Method parameters; comma expected");
                ctx.TokenIndex += 1;
            }

            return Parent;
        }
    }
}