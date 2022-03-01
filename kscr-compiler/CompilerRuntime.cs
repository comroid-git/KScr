using System;
using System.Collections.Generic;
using System.IO;
using KScr.Compiler.Class;
using KScr.Lib;
using KScr.Lib.Bytecode;
using KScr.Lib.Exception;
using KScr.Lib.Model;
using KScr.Lib.Store;

namespace KScr.Compiler
{
    public class CompilerRuntime : RuntimeBase
    {
        public override ObjectStore ObjectStore => null!;
        public override ClassStore ClassStore { get; } = new();
        public override ITokenizer Tokenizer => new Tokenizer();
        public override ClassCompiler Compiler => new();

        public void CompileFiles(IEnumerator<FileInfo> files)
        {
            var compiler = Compiler;
            if (!files.MoveNext())
                throw new ArgumentException("Missing compiler Classpath");
            var context = CompileClass(files.Current, compiler);
            while (files.MoveNext())
                CompileClass(files.Current, ref context, compiler);
        }

        public CompilerContext CompileClass(FileInfo file, AbstractCompiler abstractCompiler)
        {
            CompilerContext context = null!;
            return CompileClass(file, ref context, abstractCompiler);
        }

        public CompilerContext CompileClass(FileInfo file, ref CompilerContext context, AbstractCompiler abstractCompiler)
        {
            string? source = File.ReadAllText(file.FullName);
            var tokenlist = Tokenizer.Tokenize(this, file.FullName,
                source ?? throw new FileNotFoundException("Source file not found: " + file.FullName));
            var tokens = new TokenContext(tokenlist);
            string clsName = file.Name.Substring(0, file.Name.Length - AbstractCompiler.FileAppendix.Length);
            // ReSharper disable once ConstantConditionalAccessQualifier -> because of parameterless override
            var pkg = context?.Package ?? Package.RootPackage;
            pkg = abstractCompiler.ResolvePackage(pkg, abstractCompiler.FindClassPackageName(tokens).Split("."));
            var classInfo = abstractCompiler.FindClassInfo(tokens, clsName);
            var cls = pkg.GetOrCreateClass(this, clsName, classInfo.Modifier);
            var prev = context;
            context = new CompilerContext(context ?? new CompilerContext(new CompilerContext(), pkg), cls, tokens,
                CompilerType.Class);
            AbstractCompiler.CompilerLoop(this, Compiler, ref context);
            return prev == null ? context : context = prev;
        }

        public override void CompilePackage(DirectoryInfo dir, ref CompilerContext context, AbstractCompiler abstractCompiler)
        {
            foreach (var subDir in dir.EnumerateDirectories())
            {
                var pkg = new Package(context.Package, subDir.Name);
                var prev = context;
                context = new CompilerContext(context, pkg);
                CompilePackage(subDir, ref context, abstractCompiler);
                context = prev;
            }

            foreach (var subFile in dir.EnumerateFiles('*' + AbstractCompiler.FileAppendix)) CompileClass(subFile, ref context, abstractCompiler);
        }
    }
}