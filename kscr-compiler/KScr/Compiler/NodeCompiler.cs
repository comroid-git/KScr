using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Antlr4.Runtime;
using KScr.Antlr;
using KScr.Core;
using KScr.Core.Bytecode;

namespace KScr.Compiler;

public abstract class SourceNode
{
    public readonly List<SourceNode> Nodes = new();

    public static PackageNode ForPackage(DirectoryInfo dir, Package? package = null)
    {
        package ??= Package.RootPackage;
        return new PackageNode(dir.FullName, package.GetOrCreatePackage(dir.Name));
    }

    public static MemberNode ForClass(CompilerRuntime vm, FileInfo file, Package package)
    {
        var info = vm.FindClassInfo(file);
        var decl = vm.MakeFileDecl(file);
        return new MemberNode(file.FullName)
            { Member = package.GetOrCreateClass(vm, info.Name, info.Modifier, info.ClassType)! };
    }
}

public class PackageNode : SourceNode
{
    public readonly string Path;
    public readonly Package Package;

    public PackageNode(string path, Package package)
    {
        Path = path;
        Package = package;
    }

    public int ReadPackages()
    {
        int c = 0;
        foreach (var sub in Directory.EnumerateDirectories(Path))
        {
            var dir = new DirectoryInfo(sub);
            Nodes.Add(ForPackage(dir, Package));
            c++;
        }
        return c;
    }

    public int ReadClasses(CompilerRuntime vm)
    {
        int c = 0;
        foreach (var sub in Directory.EnumerateFiles(Path, '*'+RuntimeBase.SourceFileExt, SearchOption.TopDirectoryOnly))
        {
            var file = new FileInfo(sub);
            Nodes.Add(ForClass(vm, file, Package));
            c++;
        }
        return c;
    }
}

public class MemberNode : SourceNode
{
    public string Path { get; }
    public CodeNode? Code => Nodes.Select(x => x as CodeNode).FirstOrDefault();
    public KScrParser.MemberContext MemberContext { get; init; }
    public IClassMember Member { get; init; }

    public MemberNode(string path)
    {
        Path = path;
    }

    // only if is ClassNode
    public int ReadClasses(CompilerRuntime vm)
    {
        int c = 0;
        foreach (var sub in Directory.EnumerateFiles(Path, '*'+RuntimeBase.SourceFileExt, SearchOption.TopDirectoryOnly))
        {
            var file = new FileInfo(sub);
            Nodes.Add(ForClass(vm, file, Member.Parent.Package!));
            c++;
        }
        return c;
    }
    public int ReadMembers() => throw new NotImplementedException();
}

// member code
public class CodeNode : SourceNode
{
    public ParserRuleContext? UncompiledCode { get; init; }
}
