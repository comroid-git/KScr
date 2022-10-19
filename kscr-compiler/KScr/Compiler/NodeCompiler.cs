using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

    protected SourceNode(CompilerRuntime vm, CompilerContext ctx) : base(vm, ctx)
    {
    }

    public static PackageNode ForPackage(CompilerRuntime vm, CompilerContext ctx, DirectoryInfo dir,
        Package? package = null)
    {
        var pkg = (package ?? Package.RootPackage).GetOrCreatePackage(dir.Name);
        return new PackageNode(vm, new CompilerContext { Parent = ctx, Package = pkg }, dir.FullName, pkg);
    }

    public static int RevisitRec(IEnumerable<SourceNode> nodes, bool rec = false)
    {
        var c = 0;
        foreach (var node in nodes)
            if (node is PackageNode pkg)
                c += RevisitRec(pkg.Nodes, true);
            else if (node is MemberNode mem)
                if (mem.Member is Core.Std.Class cls)
                    c += mem.Nodes.Where(x => x is MemberNode)
                        .Cast<MemberNode>()
                        .Select(x => x.RevisitCode())
                        .Sum();
                else c += mem.RevisitCode();
            else if (node is FileNode fil)
                c += RevisitRec(fil.Nodes, true);
            else throw new FatalException("Invalid Node to revisit: " + node);

        if (!rec)
            Debug.WriteLine($"[NodeCompiler] Revisited {c} members");
        return c;
    }
}

public class PackageNode : SourceNode
{
    public readonly Package Package;
    public readonly string Path;

    public PackageNode(CompilerRuntime vm, CompilerContext ctx, string path, Package package) : base(vm, ctx)
    {
        Path = path;
        Package = package;
    }

    public void Read()
    {
        int pc, fc, mc = fc = pc = 0;

        pc += ReadPackages();
        pc += ReadPackageMembersRec(Nodes);
        fc += ReadFilesRec(Nodes);
        mc += ReadClassesRec(Nodes);
        Debug.WriteLine($"[NodeCompiler] Loaded {pc} packages");
        Debug.WriteLine($"[NodeCompiler] Loaded {fc} files");
        Debug.WriteLine($"[NodeCompiler] Loaded {mc} members");
        Debug.WriteLine($"[NodeCompiler] Loaded {pc + fc + mc} nodes");
    }

    public static int ReadPackageMembersRec(IEnumerable<SourceNode> nodes)
    {
        int c = 0;
        foreach (var node in nodes.Where(x => x is PackageNode).Cast<PackageNode>())
        {
            c += node.ReadPackages();
            c += ReadPackageMembersRec(node.Nodes);
        }
        return c;
    }

    public int ReadPackages()
    {
        var c = 0;
        foreach (var sub in Directory.EnumerateDirectories(Path))
        {
            var dir = new DirectoryInfo(sub);
            Nodes.Add(ForPackage(vm, ctx, dir, Package));
            c++;
        }
        return c;
    }

    private static int ReadFilesRec(IEnumerable<SourceNode> nodes)
    {
        int c = 0;
        foreach (var node in nodes.Where(x => x is PackageNode).Cast<PackageNode>())
        {
            c += node.ReadFiles();
            c += ReadFilesRec(node.Nodes);
        }
        return c;
    }

    public int ReadFiles()
    {
        int c = 0;
        foreach (var sub in Directory.EnumerateFiles(Path, '*' + RuntimeBase.SourceFileExt, SearchOption.TopDirectoryOnly))
        {
            var file = new FileInfo(sub);
            Nodes.Add(new FileNode(vm, ctx, this, file));
            c++;
        }
        return c;
    }

    public static int ReadClassesRec(IEnumerable<SourceNode> nodes)
    {
        int c = 0; 
        foreach (var node in nodes)
        {
            if (node is FileNode fn)
                c += fn.ReadClass();
            c += ReadClassesRec(node.Nodes);
        }
        return c;
    }
}

public class FileNode : SourceNode
{
    public PackageNode Pkg { get; }
    public FileInfo File { get; }
    public ClassInfo ClassInfo { get; }
    public KScrParser.FileContext Decl { get; }
    public List<string> Imports { get; }
    public Core.Std.Class Cls { get; }

    public FileNode(CompilerRuntime vm, CompilerContext ctx, PackageNode pkg, FileInfo file) : base(vm, ctx)
    {
        this.Pkg = pkg;
        this.File = file;
        this.ClassInfo = vm.FindClassInfo(file);
        this.Decl = vm.MakeFileDecl(File);
        this.Imports = vm.FindClassImports(Decl.imports());
        this.Cls = Pkg.Package.GetOrCreateClass(vm, ClassInfo.Name, ClassInfo.Modifier, ClassInfo.ClassType)!;
    }

    public int ReadClass()
    {
        var classNode = CreateClassNode();
        var c = classNode.ReadMembers();
        Nodes.Add(classNode);
        return c;
    }

    public MemberNode CreateClassNode()
    {
        if (Decl.classDecl().Length != 1)
            throw new NotImplementedException("Unable to load more than one class from source file " + File.FullName);
        var ctx = new CompilerContext { Parent = this.ctx, Class = Cls, Imports = this.Imports };
        var kls = Decl.classDecl(0);
        try
        {
            ctx.PushContext(Cls);
            foreach (var type in kls.objectExtends()?.type() ?? new KScrParser.TypeContext[] { })
                Cls.DeclaredSuperclasses.Add(ctx.FindType(vm, type.GetText())!.AsClassInstance(vm));
            foreach (var type in kls.objectImplements()?.type() ?? new KScrParser.TypeContext[] { })
                Cls.DeclaredInterfaces.Add(ctx.FindType(vm, type.GetText())!.AsClassInstance(vm));
        }
        finally
        {
            ctx.DropContext();
        }

        return new MemberNode(vm, ctx, Pkg)
        {
            MemberContext = kls,
            Member = Cls
        };
    }
}

public class MemberNode : SourceNode
{
    public MemberNode(CompilerRuntime vm, CompilerContext ctx, PackageNode pkg, MemberNode? parent = null) : base(vm, ctx)
    {
        Pkg = pkg;
        Parent = parent;
    }

    public ParserRuleContext MemberContext { get; init; } = null!;
    public IClassMember? Member { get; init; }
    public ParserRuleContext? UncompiledCode { get; init; }
    public PackageNode Pkg { get; }
    public MemberNode? Parent { get; }

    // only if is ClassNode
    public int ReadMembers()
    {
        var c = 0;
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
            HashSet<Symbol> symbols = new();
            foreach (var param in mtd.Parameters)
                symbols.Add(ctx.RegisterSymbol(param.Name, param.Type, SymbolType.Parameter));
            mtd.Body = VisitCode(UncompiledCode);
            foreach (var symbol in symbols)
                ctx.UnregisterSymbol(symbol);
            return 1;
        }

        if (UncompiledCode != null)
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
            ctor.Parameters.Add(new MethodParameter
            {
                Type = VisitTypeInfo(param.type()),
                Name = param.idPart().GetText()
            });
        foreach (var super in context.subConstructorCalls()?.subConstructorCall() ?? Array.Empty<KScrParser.SubConstructorCallContext>())
        {
            var superType = ctx.FindType(vm, super.type().GetText())!;
            ctor.SuperCalls.Add(new StatementComponent()
            {
                Type = StatementComponentType.Code,
                CodeType = BytecodeType.ConstructorCall,
                Arg = superType.FullDetailedName,
                SubStatement = VisitArguments(super.arguments())
            });
        }
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
            mtd.Parameters.Add(new MethodParameter
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
        prop.Getter = new ExecutableCode
        {
            Main =
            {
                new Statement
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