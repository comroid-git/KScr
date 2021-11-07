using KScr.Lib;
using KScr.Lib.Bytecode;
using KScr.Lib.Model;

namespace KScr.Compiler.Code
{
    public abstract class AbstractCodeCompiler : AbstractCompiler
    {
        protected bool _active = true;

        public override bool Active => _active;

        protected AbstractCodeCompiler(ICompiler parent) : base(parent)
        {
        }
    }
    
    public class StatementCompiler : AbstractCodeCompiler
    {
        public StatementCompiler(ICompiler parent) : base(parent)
        {
        }

        public override ICompiler? AcceptToken(RuntimeBase vm, ref CompilerContext ctx)
        {
            switch (ctx.Token.Type)
            {
                case TokenType.IdentVoid:
                case TokenType.IdentNum:
                case TokenType.IdentStr:
                    ctx.Statement = new Statement();
                    ctx.Component = new StatementComponent
                    {
                        
                    };
                    ctx.TokenIndex += 1;
                    break;
                case TokenType.ParAccClose:
                    _active = false;
                    return Parent;
            }

            return this;
        }
    }
    
    public class ExpressionCompiler : AbstractCodeCompiler
    {
        private enum Mode
        {
            Parentheses
        }
        
        public ExpressionCompiler(ICompiler parent) : base(parent)
        {
        }

        public override ICompiler? AcceptToken(RuntimeBase vm, ref CompilerContext ctx)
        {
            switch (ctx.Token.Type)
            {
                case TokenType.Terminator:
                    // todo finalize & push expression
                    _active = false;
                    return Parent;
            }

            return this;
        }
    }
}