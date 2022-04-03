using System.Collections.Generic;
using System.IO;
using Antlr4.Runtime;
using KScr.Antlr;
using KScr.Antlr;
using KScr.Compiler.Class;
using KScr.Core;
using KScr.Core.Bytecode;
using KScr.Core.Exception;
using KScr.Core.Model;
using KScr.Core.Store;

namespace KScr.Compiler
{
    public class CompilerRuntime : RuntimeBase
    {
        public override INativeRunner? NativeRunner => null;
        public override ObjectStore ObjectStore => null!;
        public override ClassStore ClassStore { get; } = new();

        public void CompileFiles(IEnumerable<FileInfo> sources)
        {
            IEnumerator<FileInfo> files = null!;
            try
            {
                files = sources.GetEnumerator();
                while (files.MoveNext())
                    CompileClass(files.Current);
            } finally
            {
                files?.Dispose();
            }
        }

        public void CompileClass(FileInfo file)
        {
            string clsName = file.Name.Substring(0, file.Name.Length - SourceFileType.Length);
            CompileClass(clsName, file.FullName);
        }

        public Core.Bytecode.Class CompileClass(string clsName, string filePath = "org/comroid/kscr/core/System.kscr", string? source = null)
        {
            var fileDecl = MakeFileDecl(source != null ? new AntlrInputStream(source) : new AntlrFileStream(filePath, Encoding));

            // ReSharper disable once ConstantConditionalAccessQualifier -> because of parameterless override
            var pkg = Package.RootPackage.GetOrCreatePackage(fileDecl.packageDecl().id().GetText());
            var imports = FindClassImports(fileDecl.imports());
            var news = new Dictionary<string, Core.Bytecode.Class>();
            var ctx = new CompilerContext
            {
                Package = pkg,
                Imports = imports
            };
            
            foreach (var classDecl in fileDecl.classDecl())
            {
                var classInfo = new ClassInfoVisitor(this, ctx).Visit(classDecl);
                var subctx = new CompilerContext
                {
                    Parent = ctx,
                    Class = pkg.GetOrCreateClass(this, classInfo.Name, classInfo.Modifier, classInfo.ClassType)
                };
                var cls = new ClassVisitor(this, subctx).Visit(classDecl);
                cls.Initialize(this);
                cls.LateInitialize(this, MainStack);
                news[cls.Name] = cls;
            }
            return news[clsName];
        }

        public KScrParser.FileContext MakeFileDecl(BaseInputCharStream input)
        {
            var lexer = new KScrLexer(input);
            var tokens = new CommonTokenStream(lexer);
            var parser = new KScrParser(tokens);
            return parser.file();
        }

        private List<string> FindClassImports(KScrParser.ImportsContext ctx)
        {
            var yields = new List<string>();
            foreach (var importDecl in ctx.importDecl())
                yields.Add(importDecl.id().GetText());
            return yields;
        }

        public ClassInfo FindClassInfo(FileInfo file)
        {
            var fileDecl = MakeFileDecl(new AntlrFileStream(file.FullName));
            var pkg = Package.RootPackage.GetOrCreatePackage(fileDecl.packageDecl().id().GetText());
            return new ClassInfoVisitor(this, new CompilerContext(){Package = pkg}).Visit(fileDecl.classDecl(0));
        }
    }
}