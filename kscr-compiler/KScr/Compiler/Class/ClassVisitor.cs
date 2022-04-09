using System;
using System.ComponentModel.Design;
using System.Linq;
using KScr.Antlr;
using KScr.Core;
using KScr.Core.Bytecode;
using KScr.Core.Exception;
using KScr.Core.Model;

namespace KScr.Compiler.Class;

public class ClassVisitor : AbstractVisitor<Core.Class>
{
    public ClassVisitor(RuntimeBase vm, CompilerContext ctx) : base(vm, ctx)
    {
    }

    private Core.Class cls => ctx.Class!.AsClass(vm);

    public override Core.Class VisitClassDecl(KScrParser.ClassDeclContext context)
    {
        if (context.genericTypeDefs() is { } defs)
            foreach (var genTypeDef in defs.genericTypeDef())
                if (cls.TypeParameters.All(x => x.Name != genTypeDef.idPart().GetText())) 
                    cls.TypeParameters.Add(VisitTypeParameter(genTypeDef));
        if (context.objectExtends() is { } ext)
            foreach (var extendsType in ext.type())
                cls._superclasses.Add(VisitTypeInfo(extendsType).AsClassInstance(vm));
        if (context.objectImplements() is { } impl)
            foreach (var implementsType in impl.type())
                cls._interfaces.Add(VisitTypeInfo(implementsType).AsClassInstance(vm));
        
        foreach (var each in context.member())
        {
            var member = VisitClassMember(each);
            cls.DeclaredMembers[member.Name] = member;
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
