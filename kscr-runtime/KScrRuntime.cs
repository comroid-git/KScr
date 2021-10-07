using KScr.Eval;
using KScr.Lib;
using KScr.Lib.Model;
using KScr.Lib.Store;

namespace KScr.Runtime
{
    public sealed class KScrRuntime : RuntimeBase
    {
        public override ObjectStore ObjectStore { get; } = new ObjectStore();
        public override TypeStore TypeStore { get; } = new TypeStore();
        public override ITokenizer Tokenizer => new Tokenizer();
        public override ICompiler Compiler => new MainCompiler();
    }
}