using KScr.Antlr;
using KScr.Core.Bytecode;
using KScr.Core.Model;

namespace KScr.Compiler.Class;

public class ClassInfoVisitor : AbstractVisitor<ClassInfo>
{
    public ClassInfoVisitor(CompilerRuntime vm, CompilerContext ctx) : base(vm, ctx)
    {
    }

    public override ClassInfo VisitClassDecl(KScrParser.ClassDeclContext context)
    {
        var pkgName = ctx.Package.FullName;
        var modifier = new ModifierVisitor().Visit(context.modifiers());
        var type = new ClassTypeVisitor().Visit(context.classType());
        var name = context.idPart().GetText();
        return new ClassInfo(modifier, type, name)
        {
            CanonicalName = $"{pkgName}.{name}",
            FullName = $"{pkgName}.{name}"
        };
    }
}

public class ModifierVisitor : KScrParserBaseVisitor<MemberModifier>
{
    public override MemberModifier VisitModPublic(KScrParser.ModPublicContext context)
    {
        return MemberModifier.Public;
    }

    public override MemberModifier VisitModInternal(KScrParser.ModInternalContext context)
    {
        return MemberModifier.Internal;
    }

    public override MemberModifier VisitModProtected(KScrParser.ModProtectedContext context)
    {
        return MemberModifier.Protected;
    }

    public override MemberModifier VisitModPrivate(KScrParser.ModPrivateContext context)
    {
        return MemberModifier.Private;
    }

    public override MemberModifier VisitModStatic(KScrParser.ModStaticContext context)
    {
        return MemberModifier.Static;
    }

    public override MemberModifier VisitModFinal(KScrParser.ModFinalContext context)
    {
        return MemberModifier.Final;
    }

    public override MemberModifier VisitModAbstract(KScrParser.ModAbstractContext context)
    {
        return MemberModifier.Abstract;
    }

    public override MemberModifier VisitModSyncronized(KScrParser.ModSyncronizedContext context)
    {
        return MemberModifier.Syncronized;
    }

    public override MemberModifier VisitModNative(KScrParser.ModNativeContext context)
    {
        return MemberModifier.Native;
    }

    public override MemberModifier VisitModifiers(KScrParser.ModifiersContext context)
    {
        var mod = MemberModifier.None;
        foreach (var sub in context.modifier())
            mod |= Visit(sub);
        return mod;
    }
}

public class ClassTypeVisitor : KScrParserBaseVisitor<ClassType>
{
    public override ClassType VisitCtClass(KScrParser.CtClassContext context)
    {
        return ClassType.Class;
    }

    public override ClassType VisitCtInterface(KScrParser.CtInterfaceContext context)
    {
        return ClassType.Interface;
    }

    public override ClassType VisitCtEnum(KScrParser.CtEnumContext context)
    {
        return ClassType.Enum;
    }

    public override ClassType VisitCtAnnotation(KScrParser.CtAnnotationContext context)
    {
        return ClassType.Annotation;
    }
}