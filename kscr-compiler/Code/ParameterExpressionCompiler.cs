using KScr.Lib;
using KScr.Lib.Bytecode;
using KScr.Lib.Exception;
using KScr.Lib.Model;

namespace KScr.Compiler.Code
{
    public sealed class ParameterExpressionCompiler : AbstractCompiler
    {
        private bool _active = true;

        public ParameterExpressionCompiler(ICompiler parent) : base(parent)
        {
        }

        public override ICompiler? AcceptToken(RuntimeBase vm, ref CompilerContext ctx)
        {
            ctx.Component = new StatementComponent
            {
                Type = StatementComponentType.Code,
                CodeType = BytecodeType.ParameterExpression,
                InnerCode = new ExecutableCode()
            };
            //ctx.TokenIndex -= 1;

            while (ctx.Token.Type != TokenType.ParRoundClose)
            {
                var subctx = new CompilerContext(ctx, CompilerType.CodeParameterExpression);
                subctx.Statement = new Statement
                {
                    Type = StatementComponentType.Code,
                    CodeType = BytecodeType.ParameterExpression
                };
                CompilerLoop(vm, new ExpressionCompiler(this, false, TokenType.Comma, TokenType.ParRoundClose), ref subctx);
                ctx.LastComponent!.InnerCode!.Main.Add(subctx.Statement);
                ctx.TokenIndex = subctx.TokenIndex;
            }

            _active = false;
            return this;
        }

        public override bool Active => _active;
    }
}