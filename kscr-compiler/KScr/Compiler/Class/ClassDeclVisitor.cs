using System;
using System.ComponentModel.Design;
using KScr.Antlr;
using KScr.Core.Bytecode;

namespace KScr.Compiler.Class;

public class ClassDeclVisitor : KScrParserBaseVisitor<Core.Bytecode.Class>
{
    public Core.Bytecode.Class Class { get; }

    public ClassDeclVisitor(Core.Bytecode.Class @class)
    {
        Class = @class;
    }

    public override Core.Bytecode.Class VisitGenericTypeDef(KScrParser.GenericTypeDefContext context)
    {
        throw new NotImplementedException();
    }

    public override Core.Bytecode.Class VisitObjectExtends(KScrParser.ObjectExtendsContext context)
    {
        throw new NotImplementedException();
    }

    public override Core.Bytecode.Class VisitObjectImplements(KScrParser.ObjectImplementsContext context)
    {
        throw new NotImplementedException();
    }

    public override Core.Bytecode.Class VisitMember(KScrParser.MemberContext context)
    {
        IClassMember member = context.RuleIndex switch
        {
            KScrParser.RULE_methodDecl => new MethodVisitor().Visit(context.methodDecl()),
            KScrParser.RULE_constructorDecl => new ConstructorVisitor().Visit(context.methodDecl()),
            KScrParser.RULE_propertyDecl => new PropertyVisitor().Visit(context.methodDecl()),
            KScrParser.RULE_initDecl => new InitializerVisitor().Visit(context.methodDecl()),
            //KScrParser.RULE_classDecl => new MethodVisitor().Visit(context.methodDecl()),
            _ => throw new ArgumentOutOfRangeException(nameof(context.RuleIndex), context.RuleIndex, "Invalid rule " + context)
        };
    }
}