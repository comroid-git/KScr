using KScr.Eval;
using KScr.Lib;
using KScr.Lib.Model;
using KScr.Lib.Store;

namespace KScr.Runtime
{
    public sealed class KScrRuntime : RuntimeBase
    {
        public override ObjectStore ObjectStore { get; } = new ObjectStore();
        public override ClassStore ClassStore { get; } = new ClassStore();
        public override ITokenizer CodeTokenizer { get; } = new CodeTokenizer();
        public override ITokenizer ClassTokenizer { get; } = new ClassTokenizer();
        public override ICodeCompiler CodeCompiler { get; } = new MainCodeCompiler();
        public override IClassCompiler ClassCompiler { get; } = new ClassCompiler();
    }
}