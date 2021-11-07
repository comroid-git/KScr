
using KScr.Lib;
using KScr.Lib.Model;
using KScr.Lib.Store;

namespace KScr.Compiler
{
    public class CompilerRuntime : RuntimeBase
    {
        public override ObjectStore ObjectStore => null!;
        public override ClassStore ClassStore { get; } = new ClassStore();
        public override ITokenizer Tokenizer => new Tokenizer();
        public override ICompiler ClassCompiler => new ClassCompiler();
        public override ICompiler CodeCompiler => new StatementCompiler();
    }
}