﻿using System;
using KScr.Antlr;
using KScr.Compiler.Code;
using KScr.Core;
using KScr.Core.Bytecode;
using KScr.Core.Model;

namespace KScr.Compiler.Class;

public class ClassMemberVisitor : AbstractVisitor<IClassMember>
{
    public ClassMemberVisitor(RuntimeBase vm, CompilerContext ctx) : base(vm, ctx)
    {
    }

    public override IClassMember VisitMethodDecl(KScrParser.MethodDeclContext context)
    {
        var name = context.idPart().GetText();
        var type = VisitTypeInfo(context.type());
        var mod = VisitModifiers(context.modifiers());
        return new Method(ToSrcPos(context), ctx.Class!.AsClass(vm), name, type, mod) 
            { Body = VisitMemberCode(context.memberBlock()) };
    }

    public override IClassMember VisitConstructorDecl(KScrParser.ConstructorDeclContext context)
    {
        var cls = ctx.Class!.AsClass(vm);
        var mod = VisitModifiers(context.modifiers());
        return new Method(ToSrcPos(context), cls, Method.ConstructorName, cls, mod)
            { Body = VisitMemberCode(context.memberBlock()) };
    }

    public override IClassMember VisitInitDecl(KScrParser.InitDeclContext context)
    {
        return new Method(ToSrcPos(context), ctx.Class!.AsClass(vm), Method.StaticInitializerName,
                Core.Bytecode.Class.VoidType, MemberModifier.Private | MemberModifier.Static) 
            { Body = VisitMemberCode(context.memberBlock()) };
    }

    public override IClassMember VisitPropertyDecl(KScrParser.PropertyDeclContext context)
    {
        var name = context.idPart().GetText();
        var type = VisitTypeInfo(context.type());
        var mod = VisitModifiers(context.modifiers());
        var prop = new Property(ToSrcPos(context), ctx.Class!.AsClass(vm), name, type, mod);
        return new PropBlockVisitor(this, prop).Visit(context.propBlock());
    }

    private sealed class PropBlockVisitor : KScrParserBaseVisitor<Property>
    {
        private readonly ClassMemberVisitor _parent;
        private readonly Property _prop;

        public PropBlockVisitor(ClassMemberVisitor parent, Property prop)
        {
            _parent = parent;
            _prop = prop;
        }

        public override Property VisitPropComputed(KScrParser.PropComputedContext context)
        {
            _prop.Getter = _parent.VisitMemberCode(context.memberBlock());
            _prop.Gettable = true;
            return _prop;
        }

        public override Property VisitPropAccessors(KScrParser.PropAccessorsContext context)
        {
            _prop.Gettable = (_prop.Getter = _parent.VisitMemberCode(context.propGetter())) != null;
            if (context.propSetter() is { } setter)
                _prop.Settable = (_prop.Setter = _parent.VisitMemberCode(setter)) != null;
            if (context.propInit() is { } init)
                _prop.Inittable = (_prop.Initter = _parent.VisitMemberCode(init)) != null;
            return _prop;
        }

        public override Property VisitPropFieldStyle(KScrParser.PropFieldStyleContext context)
        {
            if (context.Start.Type == KScrLexer.ASSIGN && context.expr() is { } expr)
            {
                var initter = new ExecutableCode();
                var stmt = new Statement()
                {
                    Type =StatementComponentType.Expression,
                    CodeType = BytecodeType.Expression
                };
                stmt.Main.Add(_parent.VisitExpression(expr));
                initter.Main.Add(stmt);
                _prop.Inittable = (_prop.Initter = initter) != null;
            }
            _prop.Gettable = _prop.Settable = true;
            return _prop;
        }
    }
}