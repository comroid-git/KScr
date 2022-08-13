﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using Antlr4.Runtime;
using KScr.Antlr;
using KScr.Core;
using KScr.Core.Bytecode;
using KScr.Core.Exception;
using KScr.Core.Model;

namespace KScr.Compiler;

public abstract class SourceNode : AbstractVisitor<SourceNode>
{
    public readonly List<SourceNode> Nodes = new();

    public static PackageNode ForPackage(CompilerRuntime vm, CompilerContext ctx, DirectoryInfo dir, Package? package = null)
    {
        var pkg = (package ?? Package.RootPackage).GetOrCreatePackage(dir.Name);
        return new PackageNode(vm, new CompilerContext() { Parent = ctx, Package = pkg }, dir.FullName, pkg);
    }

    public static MemberNode ForBaseClass(CompilerRuntime vm, CompilerContext ctx, FileInfo file, PackageNode pkg)
    {
        var info = vm.FindClassInfo(file);
        var decl = vm.MakeFileDecl(file);
        if (decl.classDecl().Length != 1)
            throw new NotImplementedException("Unable to load more than one class from source file " + file.FullName);
        var cls = pkg.Package.GetOrCreateClass(vm, info.Name, info.Modifier, info.ClassType)!;
        var kls = decl.classDecl(0);
        foreach (var type in kls.objectExtends()?.type() ?? new KScrParser.TypeContext[]{})
            cls._superclasses.Add(ctx.FindType(vm, type.GetText())!.AsClassInstance(vm));
        foreach (var type in kls.objectImplements()?.type() ?? new KScrParser.TypeContext[]{})
            cls._interfaces.Add(ctx.FindType(vm, type.GetText())!.AsClassInstance(vm));
        return new MemberNode(vm,
            new CompilerContext() { Parent = ctx, Class = cls, Imports = vm.FindClassImports(decl.imports()) }, pkg)
        {
            MemberContext = kls,
            Member = cls,
        };
    }

    public static int RevisitRec(IEnumerable<SourceNode> nodes, bool rec = false)
    {
        int c = 0;
        foreach (var node in nodes)
        {
            if (node is PackageNode pkg)
                c += RevisitRec(pkg.Nodes, true);
            else if (node is MemberNode mem)
                if (mem.Member is Core.Std.Class cls)
                    c += mem.Nodes.Where(x => x is MemberNode)
                        .Cast<MemberNode>()
                        .Select(x => x.RevisitCode())
                        .Sum();
                else c += mem.RevisitCode();
            else throw new FatalException("Invalid Node to revisit: " + node);
        }

        if (!rec)
            Debug.WriteLine($"[NodeCompiler] Revisited {c} members");
        return c;
    }

    protected SourceNode(RuntimeBase vm, CompilerContext ctx) : base(vm, ctx)
    {
    }
}

public class PackageNode : SourceNode
{
    public readonly string Path;
    public readonly Package Package;

    public PackageNode(RuntimeBase vm, CompilerContext ctx, string path, Package package) : base(vm, ctx)
    {
        Path = path;
        Package = package;
    }

    public void Read()
    {
        int pc, cc = pc = 0;
        
        pc += ReadPackages();
        pc += ReadPackagesRec(Nodes, ref cc);
        Debug.WriteLine($"[NodeCompiler] Loaded {pc} packages");
        Debug.WriteLine($"[NodeCompiler] Loaded {cc} classes");
        Debug.WriteLine($"[NodeCompiler] Loaded {pc + cc} nodes");
    }

    public int ReadPackages()
    {
        int c = 0;
        foreach (var sub in Directory.EnumerateDirectories(Path))
        {
            var dir = new DirectoryInfo(sub);
            Nodes.Add(ForPackage(vm as CompilerRuntime ?? throw new FatalException("Invalid Runtime"), ctx, dir, Package));
            c++;
        }
        return c;
    }

    public static int ReadPackagesRec(IEnumerable<SourceNode> nodes, ref int cc)
    {
        int c = 0;
        foreach (var node in nodes.Where(x => x is PackageNode).Cast<PackageNode>())
        {
            c += node.ReadPackages();
            c += ReadPackagesRec(node.Nodes, ref cc);
            cc += node.ReadClasses();
        }

        return c;
    }

    public int ReadClasses()
    {
        int c = 0;
        foreach (var sub in Directory.EnumerateFiles(Path, '*'+RuntimeBase.SourceFileExt, SearchOption.TopDirectoryOnly))
        {
            var file = new FileInfo(sub);
            var classNode = ForBaseClass(vm as CompilerRuntime ?? throw new FatalException("Invalid Runtime"), ctx, file, this);
            classNode.ReadMembers();
            Nodes.Add(classNode);
            c++;
        }
        return c;
    }
}

public class MemberNode : SourceNode
{
    public ParserRuleContext MemberContext { get; init; } = null!;
    public IClassMember? Member { get; init; }
    public ParserRuleContext? UncompiledCode { get; init; }
    public PackageNode Pkg { get; }
    public MemberNode? Parent { get; }

    public MemberNode(RuntimeBase vm, CompilerContext ctx, PackageNode pkg, MemberNode? parent = null) : base(vm, ctx)
    {
        Pkg = pkg;
        Parent = parent;
    }

    // only if is ClassNode
    public int ReadMembers()
    {
        int c = 0;
        if (MemberContext is not KScrParser.ClassDeclContext cls)
            throw new NotSupportedException("Can read members only for Class node");
        foreach (var mem in cls.member())
        {
            Nodes.Add(Visit(mem));
            c++;
        }
        return c;
    }

    public int RevisitCode()
    {
        if (Member is Method mtd)
        {
            mtd.Body = VisitCode(UncompiledCode);
            return 1;
        }
        else if (UncompiledCode != null)
        {
            Visit(UncompiledCode);
            return 1;
        }

        return 0;
    }

    public override SourceNode VisitInitDecl(KScrParser.InitDeclContext context)
    {
        return new MemberNode(vm, ctx, Pkg, this)
        {
            MemberContext = context,
            Member = new Method(Utils.ToSrcPos(context.memberBlock()), ContainingClass(), Method.StaticInitializerName,
                Core.Std.Class.VoidType, MemberModifier.PSF),
            UncompiledCode = context.memberBlock()
        };
    }

    public override SourceNode VisitConstructorDecl(KScrParser.ConstructorDeclContext context)
    {
        var ctor = new Method(Utils.ToSrcPos(context.type()), ContainingClass(), Method.ConstructorName,
            ContainingClass(), MemberModifier.PS);
        foreach (var param in context.parameters().parameter())
            ctor.Parameters.Add(new MethodParameter()
            {
                Type = VisitTypeInfo(param.type()),
                Name = param.idPart().GetText()
            });
        return new MemberNode(vm, ctx, Pkg, this)
        {
            MemberContext = context,
            Member = ctor,
            UncompiledCode = context.memberBlock()
        };
    }

    public override SourceNode VisitMethodDecl(KScrParser.MethodDeclContext context)
    {
        var mtd = new Method(Utils.ToSrcPos(context.idPart()), ContainingClass(), context.idPart().GetText(),
            FindTypeInfo(context.type())!, VisitModifiers(context.modifiers()));
        foreach (var param in context.parameters().parameter())
            mtd.Parameters.Add(new MethodParameter()
            {
                Type = VisitTypeInfo(param.type()),
                Name = param.idPart().GetText()
            });
        return new MemberNode(vm, ctx, Pkg, this)
        {
            MemberContext = context,
            Member = mtd,
            UncompiledCode = context.memberBlock()
        };
    }

    public override SourceNode VisitPropertyDecl(KScrParser.PropertyDeclContext context)
    {
        return new MemberNode(vm, ctx, Pkg, this)
        {
            MemberContext = context,
            Member = new Property(Utils.ToSrcPos(context.idPart()), ContainingClass(), context.idPart().GetText(),
                FindTypeInfo(context.type())!, VisitModifiers(context.modifiers())),
            UncompiledCode = context.propBlock()
        };
    }

    public override SourceNode VisitPropComputed(KScrParser.PropComputedContext context)
    {
        if (Member is not Property prop)
            throw new FatalException("Invalid Member for Property body: " + Member);
        prop.Getter = VisitCode(context);
        return this;
    }

    public override SourceNode VisitPropAccessors(KScrParser.PropAccessorsContext context)
    {
        if (Member is not Property prop)
            throw new FatalException("Invalid Member for Property body: " + Member);
        if (context.propGetter() is { } getter)
        {
            prop.Gettable = true;
            prop.Getter = VisitCode(getter);
        }
        if (context.propSetter() is { } setter)
        {
            prop.Settable = true;
            prop.Setter = VisitCode(setter);
        }
        if (context.propInit() is { } init)
        {
            prop.Inittable = true;
            prop.Initter = VisitCode(init);
        }

        return this;
    }

    public override SourceNode VisitPropFieldStyle(KScrParser.PropFieldStyleContext context)
    {
        if (Member is not Property prop)
            throw new FatalException("Invalid Member for Property body: " + Member);
        prop.Getter = new ExecutableCode()
        {
            Main =
            {
                new Statement()
                {
                    Type = StatementComponentType.Expression,
                    CodeType = BytecodeType.Expression,
                    Main = { VisitExpression(context.expr()) }
                }
            }
        };
        return this;
    }

    private Core.Std.Class ContainingClass()
    {
        if (Member is Core.Std.Class cls)
            return cls;
        return Parent!.ContainingClass();
    }
}