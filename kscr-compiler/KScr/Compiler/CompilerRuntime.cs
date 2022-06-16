using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Antlr4.Runtime;
using KScr.Antlr;
using KScr.Bytecode;
using KScr.Compiler.Class;
using KScr.Core.Bytecode;
using KScr.Core.Model;
using KScr.Core.Store;

namespace KScr.Compiler;

public class CompilerRuntime : BytecodeRuntime
{
    public override INativeRunner? NativeRunner => null;
    public override ObjectStore ObjectStore => null!;
    public override ClassStore ClassStore { get; } = new();

    public void CompileSource(string source)
    {
        if (source.EndsWith(SourceFileExt) && File.Exists(source))
            throw new NotSupportedException(); //new MemberNode(source){Member = Package.RootPackage.GetOrCreateClass(this, )}
        else if (Directory.Exists(source))
            node = new PackageNode(source, Package.RootPackage.GetOrCreatePackage())
        else throw new FileNotFoundException("Source path not found: " + source);
    }

    private readonly ConcurrentDictionary<string, KScrParser.FileContext> _fileDecls = new();
    public KScrParser.FileContext MakeFileDecl(FileInfo file) => _fileDecls.GetOrAdd(file.FullName, path => MakeFileDecl(new AntlrFileStream(path)));

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
        return new ClassInfoVisitor(this, new CompilerContext { Package = pkg }).Visit(fileDecl.classDecl(0));
    }
}