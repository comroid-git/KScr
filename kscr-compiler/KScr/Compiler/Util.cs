using System;
using KScr.Antlr;
using KScr.Core.Bytecode;
using KScr.Core.Model;

namespace KScr.Compiler;

public static class AntlrExtensions
{
    public static MemberModifier Modifiers(this KScrParser.ModifiersContext ctx)
    {
        MemberModifier mod = MemberModifier.None;
        foreach (var context in ctx.modifier()) 
            mod |= context.Modifier();
        return mod;
    }

    public static MemberModifier Modifier(this KScrParser.ModifierContext ctx) => ctx.RuleIndex switch
    {
        KScrParser.PUBLIC => MemberModifier.Public,
        KScrParser.INTERNAL => MemberModifier.Internal,
        KScrParser.PROTECTED => MemberModifier.Protected,
        KScrParser.PRIVATE => MemberModifier.Private,
        KScrParser.ABSTRACT => MemberModifier.Abstract,
        KScrParser.FINAL => MemberModifier.Final,
        KScrParser.STATIC => MemberModifier.Static,
        KScrParser.NATIVE => MemberModifier.Native,
        _ => throw new ArgumentOutOfRangeException(nameof(ctx.RuleIndex), ctx.RuleIndex, "Invalid modifier")
    };

    public static ClassType ClassType(this KScrParser.ClassTypeContext ctx) => ctx.RuleIndex switch
    {
        KScrParser.CLASS => Lib.Model.ClassType.Class,
        KScrParser.INTERFACE => Lib.Model.ClassType.Interface,
        KScrParser.ANNOTATION => Lib.Model.ClassType.Annotation,
        KScrParser.ENUM => Lib.Model.ClassType.Enum,
        _ => throw new ArgumentOutOfRangeException(nameof(ctx.RuleIndex), ctx.RuleIndex, "Invalid class type")
    };
}

public class PackageDeclVisitor : KScrParserBaseVisitor<string>
{
    public override string VisitPackageDecl(KScrParser.PackageDeclContext context) => context.id().ToString();
}
public class ClassInfoVisitor : KScrParserBaseVisitor<ClassInfo>
{
    public override ClassInfo VisitClassDecl(KScrParser.ClassDeclContext context)
    {
        var pkgName = new PackageDeclVisitor().Visit(context);
        var mod = context.modifiers().Modifiers();
        var type = context.classType().ClassType();
        var name = context.idPart().ToString();
            
        return new ClassInfo(mod, type, name)
        {
            FullName = pkgName + '.' + name,
            CanonicalName = pkgName + '.' + name
        };
    }
}