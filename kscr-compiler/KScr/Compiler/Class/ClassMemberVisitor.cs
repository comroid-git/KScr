using System;
using KScr.Antlr;
using KScr.Core;
using KScr.Core.Bytecode;

namespace KScr.Compiler.Class;

public class ClassMemberVisitor : AbstractVisitor<IClassMember>
{
    public ClassMemberVisitor(RuntimeBase vm, CompilerContext ctx) : base(vm, ctx)
    {
    }

    public override IClassMember VisitMethodDecl(KScrParser.MethodDeclContext context)
    {
        throw new NotImplementedException();
    }

    public override IClassMember VisitConstructorDecl(KScrParser.ConstructorDeclContext context)
    {
        throw new NotImplementedException();
    }

    public override IClassMember VisitInitDecl(KScrParser.InitDeclContext context)
    {
        throw new NotImplementedException();
    }

    public override IClassMember VisitPropertyDecl(KScrParser.PropertyDeclContext context)
    {
        throw new NotImplementedException();
    }
}
