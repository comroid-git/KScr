using KScr.Lib;
using KScr.Lib.Bytecode;
using KScr.Lib.Model;

namespace KScr.Compiler.Class
{
    public class ParameterDefinitionCompiler : AbstractCompiler
    {
        private readonly Method _method;
        private bool _active = true;

        public ParameterDefinitionCompiler(ClassCompiler parent, Method method) : base(parent)
        {
            _method = method;
        }

        public override bool Active => _active;

        public override ICompiler? AcceptToken(RuntimeBase vm, ref CompilerContext ctx)
        {
            switch (ctx.Token.Type)
            {
                case TokenType.Word:
                    break;
                case TokenType.ParRoundClose:
                    _active = false;
                    return Parent;
            }
            return this;
        }
    }
}