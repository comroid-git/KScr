using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Antlr4.Runtime;
using comroid.common;
using KScr.Antlr;
using KScr.Bytecode;
using KScr.Compiler.Class;
using KScr.Core.Bytecode;
using KScr.Core.Exception;
using KScr.Core.Model;
using KScr.Core.Store;

namespace KScr.Compiler;

public class CompilerRuntime : BytecodeRuntime, IAntlrErrorListener<IToken>
{
    private readonly ConcurrentDictionary<string, KScrParser.FileContext> _fileDecls = new();

    public override INativeRunner? NativeRunner => null;
    public override ObjectStore ObjectStore => null!;
    private string location = string.Empty;

    public void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol, int line,
        int pos,
        string msg, RecognitionException e)
    {
        CompilerErrors.Add(new CompilerException(offendingSymbol.ToSrcPos(location), CompilerErrorMessage.UnexpectedToken,
            "", offendingSymbol.Text, msg));
    }

    public void CompileSource(string source, string? basePackage = null)
    {
        var pkg = Package.RootPackage;
        if (basePackage != null)
            pkg = Package.RootPackage.GetOrCreatePackage(basePackage);
        var ctx = new CompilerContext { Package = pkg };
        SourceNode node;
        var src = new FileInfo(source);
        if (source.EndsWith(SourceFileExt))
        {
            var decl = MakeFileDecl(src);
            ctx = new CompilerContext
                { Parent = ctx, Class = FindClassInfo(decl), Imports = FindClassImports(decl.imports()) };
            node = new FileNode(this, ctx, new PackageNode(this, ctx, src.DirectoryName!, pkg), src).CreateClassNode();
            var mc = (node as MemberNode)!.ReadMembers();
            Log<CompilerRuntime>.At(LogLevel.Debug, $"Loaded {mc} members");
        }
        else if (Directory.Exists(source))
        {
            node = new PackageNode(this, ctx, source, pkg);
            (node as PackageNode)!.Read();
        }
        else
        {
            throw new FileNotFoundException("Source path not found: " + src.FullName);
        }

        SourceNode.RevisitRec(new[] { node });
    }

    public KScrParser.FileContext MakeFileDecl(FileInfo file)
    {
        return _fileDecls.GetOrAdd(file.FullName, path => MakeFileDecl(new AntlrFileStream(path), path));
    }

    public KScrParser.FileContext MakeFileDecl(BaseInputCharStream input, string detail)
    {
        var lexer = new KScrLexer(input);
        var tokens = new CommonTokenStream(lexer);
        var parser = new KScrParser(tokens);
        
        parser.RemoveErrorListeners();
        parser.AddErrorListener(this);
        location = detail;
        var file = parser.file();
        
        if (CompilerErrors.Count > 0)
            throw new CompilerException(new SourcefilePosition { SourcefilePath = detail }, CompilerErrorMessage.Underlying);
        return file;
    }

    public List<string> FindClassImports(KScrParser.ImportsContext ctx)
    {
        var yields = new List<string>();
        foreach (var importDecl in ctx.importDecl())
            yields.Add(importDecl.id().GetText());
        return yields;
    }

    public ClassInfo FindClassInfo(KScrParser.FileContext fileDecl)
    {
        var pkg = Package.RootPackage.GetOrCreatePackage(fileDecl.packageDecl().id().GetText());
        return new ClassInfoVisitor(this, new CompilerContext { Package = pkg }).Visit(fileDecl.classDecl(0));
    }

    public void PrintCompilerErrors<L>() where L : class => PrintCompilerErrors(Log<L>.Get());
    public void PrintCompilerErrors(Log log)
    {
        foreach (var error in CompilerErrors) 
            log.At(LogLevel.Error, error.Message);
        CompilerErrors.Clear();
    }
}