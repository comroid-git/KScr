using System;
using KScr.Antlr;
using KScr.Lib.Bytecode;

namespace KScr.Compiler.Class;

public class ClassDeclVisitor : KScrBaseVisitor<Lib.Bytecode.Class>
{
    public Lib.Bytecode.Class Class { get; }

    public ClassDeclVisitor(Lib.Bytecode.Class @class)
    {
        Class = @class;
    }

    public override Lib.Bytecode.Class VisitGenericTypeDef(KScrParser.GenericTypeDefContext context)
    {
        throw new NotImplementedException();
    }

    public override Lib.Bytecode.Class VisitObjectExtends(KScrParser.ObjectExtendsContext context)
    {
        throw new NotImplementedException();
    }

    public override Lib.Bytecode.Class VisitObjectImplements(KScrParser.ObjectImplementsContext context)
    {
        throw new NotImplementedException();
    }

    public override Lib.Bytecode.Class VisitMember(KScrParser.MemberContext context)
    {
        IClassMember member;
        switch (context.RuleIndex)
        {
            case KScrParser.RULE_methodDecl:
                member = new Method()
                break;
            case KScrParser.RULE_constructorDecl: 
                break;
            case KScrParser.RULE_propertyDecl: 
                break;
            case KScrParser.RULE_classDecl: 
                break;
            case KScrParser.RULE_initDecl: 
                break;
        }
    }
}