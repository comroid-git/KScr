using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Antlr4.Runtime;
using KScr.Antlr;
using KScr.Bytecode;
using KScr.Bytecode.Model;
using KScr.Compiler.Class;
using KScr.Compiler.Code;
using KScr.Core.Bytecode;
using KScr.Core.Model;
using KScr.Core.Store;

namespace KScr.Compiler;

public class CompilerRuntime : BytecodeRuntime
{
    public override INativeRunner? NativeRunner => null;
    public override ObjectStore ObjectStore => null!;
    public override ClassStore ClassStore { get; } = new();

    public IDictionary<IPackageMember, IBytecode> CompilePackages(DirectoryInfo srcDir)
    {
        var yield = new Dictionary<IPackageMember, IBytecode>();
        var pkg = Package.RootPackage;
        foreach (var dir in srcDir.EnumerateDirectories())
        {
            pkg = pkg.GetOrCreatePackage(BasePackage + '.' + dir.Name);
            foreach (var pair in CompilePackageMembers(dir, pkg))
                yield.Add(pair.Key, pair.Value);
            pkg = Package.RootPackage;
        }
        return yield;
    }

    private IDictionary<IPackageMember, IBytecode> CompilePackageMembers(DirectoryInfo pkgDir, Package pkg, DirectoryInfo srcDir = null!)
    {
        srcDir ??= pkgDir;
        var yield = new Dictionary<IPackageMember, IBytecode>();
        
        // compile classes in this package
        // dict<MainClassName, FileContext>
        var files = GetSourceFiles(pkgDir);
        var fileDecls = BuildFileDecls(pkg, files);

        // the key at [last split part of string class name] of Dict<string,ClassContext> is always present; it is the main class
        // the static initializer is run here, so extra symbols and subclasses are generated
        // todo: add builtin-code for code generation
        var classes = BuildClassSymbols(pkg, fileDecls);
        
        // compile code
        // imports are recursively run through this method from here as well
        foreach (var cls in CompileSymbols(srcDir, classes))
            yield[cls.MainClass] = cls; 
        
        // compile subpackages
        foreach (var dir in GetPackageDirs(pkgDir))
        foreach (var pair in CompilePackageMembers(dir, pkg.GetOrCreatePackage(dir.Name), srcDir))
            yield[pair.Key] = pair.Value;

        return yield;
    }

    private IEnumerable<FileInfo> GetSourceFiles(DirectoryInfo pkgDir)
    {
        foreach (var file in pkgDir.EnumerateFiles("*.kscr"))
            yield return file;
    }

    private IEnumerable<DirectoryInfo> GetPackageDirs(DirectoryInfo pkgDir)
    {
        foreach (var dir in pkgDir.EnumerateDirectories())
            yield return dir;
    }

    private IEnumerable<(string canonicalMainClassName, KScrParser.FileContext file, IEnumerable<string> imports)> 
        BuildFileDecls(Package pkg, IEnumerable<FileInfo> files)
    {
        foreach (var file in files)
        {
            var fileDecl = MakeFileDecl(new AntlrFileStream(file.FullName, Encoding));
            var name = MakeCanonicalName(pkg, fileDecl.classDecl(0));
            yield return (name, fileDecl, FindClassImports(fileDecl.imports()));
        }
    }

    private IEnumerable<(string mainClassName, IEnumerable<(string canonicalName, Core.Std.Class cls, IEnumerable<(IClassMember member, KScrParser.MemberContext ctx)>)> classes, CompilerContext cCtx)>
        BuildClassSymbols(Package pkg, IEnumerable<(string canonicalMainClassName, KScrParser.FileContext file, IEnumerable<string> imports)>  files)
    {
        foreach (var entry in files)
        {
            var mainClassName = entry.canonicalMainClassName;
            var cCtx = new CompilerContext { Package = pkg, Imports = entry.imports.ToImmutableList() };
            var classes = BuildClassSymbols(cCtx, pkg, entry.file);
            yield return (mainClassName, classes, cCtx);
        }
    }

    private IEnumerable<(string canonicalName, Core.Std.Class cls, IEnumerable<(IClassMember member, KScrParser.MemberContext ctx)>)>
        BuildClassSymbols(CompilerContext cCtx, Package pkg, KScrParser.FileContext file)
    {
        foreach (var decl in file.classDecl())
        {
            var canonicalName = MakeCanonicalName(pkg, decl);
            var classInfo = new ClassInfoVisitor(this, cCtx).Visit(decl);
            var cls = new ClassVisitor(this, new CompilerContext { Parent = cCtx, Class = classInfo }).Visit(decl);
            yield return (canonicalName, cls, BuildClassSymbols(cls, decl));
        }
    }

    private IEnumerable<(IClassMember member, KScrParser.MemberContext ctx)>
        BuildClassSymbols(Core.Std.Class cls, KScrParser.ClassDeclContext decl)
    {
        foreach (var memberDecl in decl.member().Select(x => x))
        {
            var member = cls.DeclaredMembers[memberDecl.GetName()];
            yield return (member, memberDecl);
        }
    }

    private IEnumerable<WritableClass> CompileSymbols(DirectoryInfo srcDir,
        IEnumerable<(string mainClassName, IEnumerable<(string canonicalName, Core.Std.Class cls, 
            IEnumerable<(IClassMember member, KScrParser.MemberContext ctx)>)> classes, CompilerContext cCtx)> classes)
    {
        foreach (var pair in classes)
        {
            foreach (var import in pair.cCtx.Imports)
            {
                var names = import.Split('.');
                var pkg = Package.RootPackage;
                var dir = srcDir.FullName;
                foreach (var name in names)
                {
                    if (!Directory.Exists(Path.Combine(dir, name)))
                        break;
                    dir = Path.Combine(dir, name);
                    pkg = pkg.GetOrCreatePackage(name);
                }

                // todo Inspect
                CompilePackageMembers(new DirectoryInfo(dir), pkg, srcDir);
            }
            
            var compiled = CompileSymbols(pair.cCtx, pair.classes).ToArray();
            var mainClass = compiled.First(x => x.Name == pair.mainClassName);
            yield return new WritableClass(mainClass, compiled);
        }
    }

    private IEnumerable<Core.Std.Class> CompileSymbols(CompilerContext cCtx, 
        IEnumerable<(string canonicalName, Core.Std.Class cls, IEnumerable<(IClassMember member, KScrParser.MemberContext ctx)> members)> classes)
    {
        foreach (var node in classes)
        {
            foreach (var member in node.members)
                CompileSymbols(new CompilerContext { Parent = cCtx, Class = node.cls }, member.member, member.ctx);
            yield return node.cls;
        }
    }

    private void CompileSymbols(CompilerContext cCtx, IClassMember member, KScrParser.MemberContext context)
    {
        switch (member.MemberType)
        {
            case ClassMemberType.Method:
                if (member is not Method mtd)
                    throw new Exception("invalid state");
                mtd.Body = new CodeblockVisitor(this, cCtx).Visit(context);
                break;
            case ClassMemberType.Property:
                if (member is not Property prop)
                    throw new Exception("invalid state");
                new ClassMemberVisitor.PropBlockVisitor(new ClassMemberVisitor(this, cCtx), prop).Visit(context);
                break;
            case ClassMemberType.Class:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private string MakeCanonicalName(Package pkg, KScrParser.ClassDeclContext name) => $"{pkg.FullName}.{name.idPart().GetText()}";

    private IEnumerable<string> FindClassImports(KScrParser.ImportsContext ctx)
    {
        foreach (var importDecl in ctx.importDecl())
            yield return importDecl.id().GetText();
    }

    private KScrParser.FileContext MakeFileDecl(BaseInputCharStream input)
    {
        var lexer = new KScrLexer(input);
        var tokens = new CommonTokenStream(lexer);
        var parser = new KScrParser(tokens);
        return parser.file();
    }
}