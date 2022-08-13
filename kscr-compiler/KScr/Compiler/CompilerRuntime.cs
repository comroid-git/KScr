using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Antlr4.Runtime;
using KScr.Antlr;
using KScr.Bytecode;
using KScr.Compiler.Class;
using KScr.Core;
using KScr.Core.Bytecode;
using KScr.Core.Exception;
using KScr.Core.Model;
using KScr.Core.Store;

namespace KScr.Compiler;

public class CompilerRuntime : BytecodeRuntime
{
    public override INativeRunner? NativeRunner => null;
    public override ObjectStore ObjectStore => null!;
    public override ClassStore ClassStore { get; } = new();

    public void CompileSource(string source, string? basePackage = null)
    {
        Package pkg = Package.RootPackage;
        if (basePackage != null)
            pkg = Package.RootPackage.GetOrCreatePackage(basePackage);
        var ctx = new CompilerContext() { Package = pkg };
        SourceNode node;
        var src = new FileInfo(source);
        if (source.EndsWith(SourceFileExt))
        {
            var decl = MakeFileDecl(src);
            ctx = new CompilerContext() { Parent = ctx, Class = FindClassInfo(src), Imports = FindClassImports(decl.imports()) };
            node = SourceNode.ForBaseClass(this, ctx, src, new PackageNode(this, ctx, src.DirectoryName!, pkg));
            int mc = (node as MemberNode)!.ReadMembers();
            Debug.WriteLine($"[NodeCompiler] Loaded {mc} members");
        }
        else if (Directory.Exists(source))
        {
            node = new PackageNode(this, ctx, source, pkg);
            (node as PackageNode)!.Read();
        }
        else throw new FileNotFoundException("Source path not found: " + src.FullName);

        SourceNode.RevisitRec(new[] { node });
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

    public List<string> FindClassImports(KScrParser.ImportsContext ctx)
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