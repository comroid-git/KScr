using System.Collections.Generic;
using System.IO;
using Antlr4.Runtime;
using KScr.Antlr;
using KScr.Antlr;
using KScr.Compiler.Class;
using KScr.Lib;
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
            var files = sources.GetEnumerator();
            while (files.MoveNext())
                CompileClass(files.Current);
        }

        public void CompileClass(FileInfo file)
        {
            string clsName = file.Name.Substring(0, file.Name.Length - AbstractCompiler.FileAppendix.Length);
            CompileClass(clsName, file.FullName);
        }

        public Lib.Bytecode.Class CompileClass(string clsName, string filePath = "org/comroid/kscr/core/System.kscr", string? source = null)
        {
            var fileDecl = MakeFileDecl(source != null ? new AntlrInputStream(source) : new AntlrFileStream(filePath, Encoding));

            // ReSharper disable once ConstantConditionalAccessQualifier -> because of parameterless override
            var pkg = AbstractCompiler.ResolvePackage(Package.RootPackage, 
                new PackageDeclVisitor().VisitPackageDecl(fileDecl.packageDecl()).Split("."));
            var imports = FindClassImports(fileDecl.imports());
            var news = new Dictionary<string, Lib.Bytecode.Class>();
            
            foreach (var classDecl in fileDecl.classDecl())
            {
                var classInfo = new ClassInfoVisitor().Visit(classDecl);
                var cls = pkg.GetOrCreateClass(this, clsName, classInfo.Modifier, classInfo.ClassType);
                if (cls == null) throw new FatalException("invalid state");
                new ClassDeclVisitor(cls).Visit(classDecl);
                cls.LateInitialization(this, MainStack);
                news[cls.Name] = cls;
            }
            return news[clsName];
        }

        public override void CompilePackage(DirectoryInfo dir, ref CompilerContext context,
            AbstractCompiler abstractCompiler)
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

        public KScrParser.FileContext MakeFileDecl(BaseInputCharStream input)
        {
            var lexer = new KScrLexer(input);
            var tokens = new CommonTokenStream(lexer);
            var parser = new KScrParser(tokens);
            return parser.file();
        }

        private IList<string> FindClassImports(KScrParser.ImportsContext ctx)
        {
            var yields = new List<string>();
            foreach (var importDecl in ctx.importDecl())
                yields.Add(importDecl.id().ToString());
            return yields;
        }

        public ClassInfo FindClassInfo(FileInfo file)
        {
            var fileDecl = MakeFileDecl(new AntlrFileStream(file.FullName));
            return new ClassInfoVisitor().Visit(fileDecl.classDecl(0));
        }
    }
}