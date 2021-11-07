using KScr.Compiler.Class;
using KScr.Compiler.Code;
using KScr.Lib;
using KScr.Lib.Model;
using KScr.Lib.Store;

namespace KScr.Compiler
{
    public sealed class CompilerRuntime : RuntimeBase
    {
        public override ObjectStore ObjectStore => null!;
        public override ClassStore ClassStore { get; } = new ClassStore();
        public override ITokenizer Tokenizer => new Tokenizer();
        public override IClassCompiler ClassCompiler => new ClassCompiler();
        public override ICodeCompiler CodeCompiler => new MainCodeCompiler();
    }
}