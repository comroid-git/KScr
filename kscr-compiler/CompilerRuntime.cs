using System;
using System.Collections.Generic;
using System.IO;
using KScr.Compiler.Class;
using KScr.Compiler.Code;
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

        public void CompileFiles(IEnumerable<FileInfo> sources)
        {
            try
            {
                var files = sources.GetEnumerator();
                while (files.MoveNext())
                    CompileClass(files.Current);
            }/*
            catch (CompilerException ex)
            {
                if (DebugMode)
#pragma warning disable CA2200
                    // ReSharper disable once PossibleIntendedRethrow
                    throw ex;
#pragma warning restore CA2200
                Console.WriteLine("An CompilerException occurred: " + ex);
            }*/
            finally {}
        }

        public void CompileClass(FileInfo file)
        {
            string clsName = file.Name.Substring(0, file.Name.Length - AbstractCompiler.FileAppendix.Length);
            CompileClass(clsName, File.ReadAllText(file.FullName), file.FullName);
        }
        
        public void CompileClass(string clsName, string source, string filePath = "org/comroid/kscr/core/System.kscr")
        {
            var tokenlist = new Tokenizer().Tokenize(filePath,
                source ?? throw new FileNotFoundException("Source file not found: " + filePath));
            var tokens = new TokenContext(tokenlist);
            // ReSharper disable once ConstantConditionalAccessQualifier -> because of parameterless override
            var pkg = Package.RootPackage;
            pkg = AbstractCompiler.ResolvePackage(pkg, AbstractCompiler.FindClassPackageName(tokens).Split("."));
            var classInfo = AbstractCompiler.FindClassInfo(AbstractCompiler.FindClassPackageName(tokens), tokens, clsName);
            var cls = pkg.GetOrCreateClass(this, clsName, classInfo.Modifier, classInfo.ClassType);
            var context = new CompilerContext(new CompilerContext(new CompilerContext(), pkg), cls, tokens, CompilerType.Class);
            AbstractCompiler.CompilerLoop(this, new ClassCompiler(), ref context);
            cls.LateInitialization(this);
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

            foreach (var subFile in dir.EnumerateFiles('*' + AbstractCompiler.FileAppendix)) CompileClass(subFile);
        }
    }
}