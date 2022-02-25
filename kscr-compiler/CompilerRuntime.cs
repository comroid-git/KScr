
using System;
using System.Collections.Generic;
using System.IO;
using KScr.Compiler.Class;
using KScr.Compiler.Code;
using KScr.Lib;
using KScr.Lib.Model;
using KScr.Lib.Store;

namespace KScr.Compiler
{
    public class CompilerRuntime : RuntimeBase
    {
        public override ObjectStore ObjectStore => null!;
        public override ClassStore ClassStore { get; } = new();
        public override ITokenizer Tokenizer => new Tokenizer();
        public override ClassCompiler Compiler => new ClassCompiler();

        public void CompileFiles(IEnumerator<FileInfo> files)
        {
            var compiler = Compiler;
            if (!files.MoveNext())
                throw new ArgumentException("Missing compiler Classpath");
            CompilerContext context = compiler.CompileClass(this, files.Current);
            while (files.MoveNext())
                compiler.CompileClass(this, files.Current, ref context);
        }
    }
}