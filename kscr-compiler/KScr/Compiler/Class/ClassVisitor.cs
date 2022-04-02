using System;
using System.ComponentModel.Design;
using KScr.Antlr;
using KScr.Core;
using KScr.Core.Bytecode;
using KScr.Core.Exception;
using KScr.Core.Model;

namespace KScr.Compiler.Class;

public class ClassVisitor : AbstractVisitor<Core.Bytecode.Class>
{
    public ClassVisitor(RuntimeBase vm, KScrParser parser, CompilerContext ctx) : base(vm, parser, ctx)
    {
    }

    public override Core.Bytecode.Class VisitClassDecl(KScrParser.ClassDeclContext context)
    {
        var name = context.idPart().GetText();
        var modifier = new ModifierVisitor().Visit(context.modifiers());
        var type = new ClassTypeVisitor().Visit(context.classType());
        var cls = new Core.Bytecode.Class(ctx.Package, name, false, modifier, type);

        foreach (var extendsType in context.objectExtends().type())
        {
            cls._superclasses.Add(FindType(extendsType.GetText()) ?? throw new CompilerException(ToSrcPos(extendsType), ));
        }

        return cls;
    }
}

public class ModifierVisitor : KScrParserBaseVisitor<MemberModifier>
{
    public override MemberModifier VisitModPublic(KScrParser.ModPublicContext context) => MemberModifier.Public;
    public override MemberModifier VisitModInternal(KScrParser.ModInternalContext context) => MemberModifier.Internal;
    public override MemberModifier VisitModProtected(KScrParser.ModProtectedContext context) => MemberModifier.Protected;
    public override MemberModifier VisitModPrivate(KScrParser.ModPrivateContext context) => MemberModifier.Private;
    public override MemberModifier VisitModStatic(KScrParser.ModStaticContext context) => MemberModifier.Static;
    public override MemberModifier VisitModFinal(KScrParser.ModFinalContext context) => MemberModifier.Final;
    public override MemberModifier VisitModAbstract(KScrParser.ModAbstractContext context) => MemberModifier.Abstract;
    public override MemberModifier VisitModSyncronized(KScrParser.ModSyncronizedContext context) => MemberModifier.Syncronized;
    public override MemberModifier VisitModNative(KScrParser.ModNativeContext context) => MemberModifier.Native;
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
    public override ClassType VisitCtClass(KScrParser.CtClassContext context) => ClassType.Class;
    public override ClassType VisitCtInterface(KScrParser.CtInterfaceContext context) => ClassType.Interface;
    public override ClassType VisitCtEnum(KScrParser.CtEnumContext context) => ClassType.Enum;
    public override ClassType VisitCtAnnotation(KScrParser.CtAnnotationContext context) => ClassType.Annotation;
}
