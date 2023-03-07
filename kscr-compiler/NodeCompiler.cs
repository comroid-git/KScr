using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Antlr4.Runtime;
using comroid.common;
using KScr.Antlr;
using KScr.Core;
using KScr.Core.Bytecode;
using KScr.Core.Exception;
using KScr.Core.Model;
using KScr.Core.System;

namespace KScr.Compiler;

public abstract class SourceNode : AbstractVisitor<SourceNode>, IValidatable
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
            try
            {
                if (node is PackageNode pkg)
                    c += RevisitRec(pkg.Nodes, true);
                else if (node is MemberNode mem)
                    if (mem.Member is Core.System.Class cls)
                        c += mem.Nodes.Where(x => x is MemberNode)
                            .Cast<MemberNode>()
                            .Select(x => x.RevisitCode())
                            .Sum();
                    else c += mem.RevisitCode();
                else if (node is FileNode fil)
                    c += RevisitRec(fil.Nodes, true);
                else throw new FatalException("Invalid Node to revisit: " + node);
            }
            catch (CompilerException cex)
            {
                node.vm.CompilerErrors.Add(cex);
            }

        if (!rec)
            Log<CompilerRuntime>.At(LogLevel.Debug, $"Revisited {c} members");
        return c;
    }

    public abstract void Validate(RuntimeBase _);
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
        Log<CompilerRuntime>.At(LogLevel.Debug, $"Loaded {pc} packages");
        Log<CompilerRuntime>.At(LogLevel.Debug, $"Loaded {fc} files");
        Log<CompilerRuntime>.At(LogLevel.Debug, $"Loaded {mc} members");
        Log<CompilerRuntime>.At(LogLevel.Debug, $"Loaded {pc + fc + mc} nodes");
    }

    public static int ReadPackageMembersRec(IEnumerable<SourceNode> nodes)
    {
        var c = 0;
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
            try
        {
            var dir = new DirectoryInfo(sub);
            var node = ForPackage(vm, ctx, dir, Package);
            node.Validate(vm);
            Nodes.Add(node);
            c++;
        }
            catch (CompilerException cex)
            {
                vm.CompilerErrors.Add(cex);
            }

        return c;
    }

    private static int ReadFilesRec(IEnumerable<SourceNode> nodes)
    {
        var c = 0;
        foreach (var node in nodes.Where(x => x is PackageNode).Cast<PackageNode>())
        {
            c += node.ReadFiles();
            c += ReadFilesRec(node.Nodes);
        }

        return c;
    }

    public int ReadFiles()
    {
        var c = 0;
        foreach (var sub in Directory.EnumerateFiles(Path, '*' + RuntimeBase.SourceFileExt,
                     SearchOption.TopDirectoryOnly))
            try
            {
                var file = new FileInfo(sub);
                var node = new FileNode(vm, ctx, this, file);
                node.Validate(vm);
                Nodes.Add(node);
                c++;
            }
            catch (CompilerException cex)
            { 
                vm.CompilerErrors.Add(cex);
            }

        return c;
    }

    public static int ReadClassesRec(IEnumerable<SourceNode> nodes)
    {
        var c = 0;
        foreach (var node in nodes)
            try
            {
                if (node is FileNode fn)
                    c += fn.ReadClass();
                c += ReadClassesRec(node.Nodes);
                node.Validate(node.vm);
            }
            catch (CompilerException cex)
            {
                node.vm.CompilerErrors.Add(cex);
            }

        return c;
    }

    public override void Validate(RuntimeBase _)
    {
        if (!Package.FullName.All(c => !char.IsLetter(c) || char.IsLower(c)))
            throw new CompilerException(RuntimeBase.SystemSrcPos, CompilerErrorMessage.InvalidName,
                "package", Package.FullName, "must be lowercase");
    }
}

public class FileNode : SourceNode
{
    public FileNode(CompilerRuntime vm, CompilerContext ctx, PackageNode pkg, FileInfo file) : base(vm, ctx)
    {
        Pkg = pkg;
        File = file;
        Decl = vm.MakeFileDecl(File);
        ClassInfo = vm.FindClassInfo(Decl);
        Imports = vm.FindClassImports(Decl.imports());
        Cls = Pkg.Package.GetOrCreateClass(ClassInfo.Name, vm, ClassInfo.Modifier, ClassInfo.ClassType)!;
    }

    public PackageNode Pkg { get; }
    public FileInfo File { get; }
    public ClassInfo ClassInfo { get; }
    public KScrParser.FileContext Decl { get; }
    public List<string> Imports { get; }
    public Core.System.Class Cls { get; }

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
        var ctx = new CompilerContext { Parent = this.ctx, Class = Cls, Imports = Imports };
        var kls = Decl.classDecl(0);
        try
        {
            ctx.PushContext(Cls);
            foreach (var type in kls.superclassesDef()?.type() ?? Array.Empty<KScrParser.TypeContext>())
            {
                var target = ctx.FindType(vm, type.GetText())!.AsClass(vm);
                var instance = target.CreateInstance(vm, ctx.Class.AsClass(vm), type.genericTypeUses().GetGenericsUses(vm, ctx, target).ToArray());
                if (target.ClassType == ClassType.Interface)
                    Cls.DeclaredInterfaces.Add(instance);
                else Cls.DeclaredSuperclasses.Add(instance);
            }
            foreach (var generic in kls.genericDefs()?.genericTypeDef() ?? Array.Empty<KScrParser.GenericTypeDefContext>())
            {
                Cls.TypeParameters.Add(VisitTypeParameter(generic));
            }
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

    public override void Validate(RuntimeBase _)
    {
        // 1 validate constructors using all superconstructors
        if (Cls.DeclaredMembers.ContainsKey(Method.ConstructorName))
        {
            var ctor = (Cls.DeclaredMembers[Method.ConstructorName] as Method)!;
            foreach (var error in Cls.Superclasses
                         .Where(cls => cls.Name is not "object" and not "void")
                         .Where(cls => !ctor.SuperCalls.Any(spr => spr.Arg.StartsWith(cls.CanonicalName)))
                         .Select(missing => new CompilerException(ctor.SourceLocation,
                             CompilerErrorMessage.ClassSuperTypeNotCalled, Cls, missing)))
                vm.CompilerErrors.Add(error);
        }
        
        // 2 validate class abstract or all abstract members implemented
        if (!Cls.IsAbstract())
            foreach (var error in ((IClass)Cls).InheritedMembers
                     .Where(ModifierMethods.IsAbstract)
                     .Where(mem => ((IClass)Cls).ClassMembers.All(x => x.Name != mem.Name))
                     .Select(missing => new CompilerException(Cls.SourceLocation,
                         CompilerErrorMessage.ClassAbstractMemberNotImplemented, Cls, missing)))
                vm.CompilerErrors.Add(error);

        // 3 validate used type parameters
    }
}

public class MemberNode : SourceNode
{
    public MemberNode(CompilerRuntime vm, CompilerContext ctx, PackageNode pkg, MemberNode? parent = null) : base(vm,
        ctx)
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
            try
            {
                var node = Visit(mem);
                Nodes.Add(node);
                c++;
            }
            catch (CompilerException cex)
            {
                vm.CompilerErrors.Add(cex);
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
                Core.System.Class.VoidType, MemberModifier.PSF),
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
        foreach (var super in context.subConstructorCalls()?.subConstructorCall() ??
                              Array.Empty<KScrParser.SubConstructorCallContext>())
        {
            var superType = ctx.FindType(vm, super.type().GetText())!;
            ctor.SuperCalls.Add(new StatementComponent
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

    private Core.System.Class ContainingClass()
    {
        if (Member is Core.System.Class cls)
            return cls;
        return Parent!.ContainingClass();
    }

    public override void Validate(RuntimeBase _)
    {
        // 1 validate overriding member is constructor or matches supermember footprint

        // 2 validate used type parameters
    }
}