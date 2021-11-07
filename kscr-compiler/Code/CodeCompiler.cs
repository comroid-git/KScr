using KScr.Lib;
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
                case TokenType.ParAccClose:
                    _active = false;
                    return Parent;
            }

            return this;
        }
    }
    
    public class ExpressionCompiler : AbstractCodeCompiler
    {
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