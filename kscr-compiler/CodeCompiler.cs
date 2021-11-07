using KScr.Lib;
using KScr.Lib.Model;

namespace KScr.Compiler
{
    public abstract class AbstractCodeCompiler : AbstractCompiler {}
    
    public class StatementCompiler : AbstractCodeCompiler
    {
        public override ICompiler? AcceptToken(RuntimeBase vm, ref CompilerContext ctx)
        {
            throw new System.NotImplementedException();
        }
    }
    
    public class ExpressionCompiler : AbstractCodeCompiler
    {
        public override ICompiler? AcceptToken(RuntimeBase vm, ref CompilerContext ctx)
        {
            throw new System.NotImplementedException();
        }
    }
}