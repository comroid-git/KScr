﻿using KScr.Compiler.Class;
using KScr.Compiler.Code;
using KScr.Lib;
using KScr.Lib.Model;
using KScr.Lib.Store;

namespace KScr.Runtime
{
    public sealed class KScrRuntime : RuntimeBase
    {
        public override ObjectStore ObjectStore { get; } = new ObjectStore();
        public override ClassStore ClassStore { get; } = new ClassStore();
        public override ITokenizer ClassTokenizer => new ClassTokenizer();
        public override IClassCompiler ClassCompiler => new ClassCompiler();
        public override ITokenizer CodeTokenizer => new CodeTokenizer();
        public override ICodeCompiler CodeCompiler => new MainCodeCompiler();
    }
}