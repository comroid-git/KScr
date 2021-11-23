using KScr.Lib;
using KScr.Lib.Model;

namespace KScr.Compiler.Code
{
    public sealed class ParameterExpressionCompiler : AbstractCodeCompiler
    {
        public ParameterExpressionCompiler(ICompiler parent) : base(parent)
        {
            // todo
        }

        public override ICompiler? AcceptToken(RuntimeBase vm, ref CompilerContext ctx)
        {
            
            
            return base.AcceptToken(vm, ref ctx);
        }
    }
}