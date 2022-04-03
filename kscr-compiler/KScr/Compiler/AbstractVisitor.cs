﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using KScr.Antlr;
using KScr.Compiler.Class;
using KScr.Compiler.Code;
using KScr.Core;
using KScr.Core.Bytecode;
using KScr.Core.Model;

namespace KScr.Compiler;

public abstract class AbstractVisitor<T> : KScrParserBaseVisitor<T>
{
    protected override bool ShouldVisitNextChild(IRuleNode node, T currentResult) => currentResult == null;

    protected RuntimeBase vm { get; }
    protected CompilerContext ctx { get; }

    protected AbstractVisitor(RuntimeBase vm, CompilerContext ctx)
    {
        this.vm = vm;
        this.ctx = ctx;
    }

    protected Core.Bytecode.Class VisitClass(KScrParser.ClassDeclContext cls) => new ClassVisitor(vm, ctx).Visit(cls);
    protected ClassInfo VisitClassInfo(KScrParser.ClassDeclContext cls) => new ClassInfoVisitor(vm, ctx).Visit(cls);
    protected ITypeInfo VisitTypeInfo(KScrParser.TypeContext type) => new TypeInfoVisitor(vm, ctx).Visit(type);
    protected new MemberModifier VisitModifiers(KScrParser.ModifiersContext mods) => new ModifierVisitor().Visit(mods);
    protected Operator VisitOperator(ParserRuleContext op) => new OperatorVisitor().Visit(op);
    protected TypeParameter VisitTypeParameter(KScrParser.GenericTypeDefContext gtd)
    {
        var name = gtd.idPart().GetText();
        var spec = name == "n" ? TypeParameterSpecializationType.N
            : gtd.elp != null ? TypeParameterSpecializationType.List
            : gtd.ext != null ? TypeParameterSpecializationType.Extends
            : gtd.sup != null ? TypeParameterSpecializationType.Super
            : throw new ArgumentException("Invalid GenericTypeDef: " + gtd);
        var target = spec switch
        {
            TypeParameterSpecializationType.List => Core.Bytecode.Class.IterableType.CreateInstance(vm, Core.Bytecode.Class.IterableType, Core.Bytecode.Class.ObjectType),
            TypeParameterSpecializationType.N => Core.Bytecode.Class.NumericIntType,
            TypeParameterSpecializationType.Extends => VisitTypeInfo(gtd.ext!),
            TypeParameterSpecializationType.Super => VisitTypeInfo(gtd.sup!),
            _ => throw new ArgumentOutOfRangeException()
        };
        ITypeInfo? def = spec == TypeParameterSpecializationType.N ? new TypeInfo { Name = gtd.defN.Text }
            : gtd.def != null ? new TypeInfo { FullDetailedName = VisitTypeInfo(gtd.def).FullDetailedName }
            : null;
        return new TypeParameter(name, spec, target.AsClassInstance(vm)) { DefaultValue = def };
    }
    protected IClassMember VisitClassMember(KScrParser.MemberContext member) => member.RuleIndex switch
    {
        KScrParser.RULE_methodDecl or KScrParser.RULE_constructorDecl or KScrParser.RULE_initDecl
            or KScrParser.RULE_propertyDecl or KScrParser.RULE_member
            => new ClassMemberVisitor(vm, ctx).Visit(member),
        KScrParser.RULE_classDecl => new ClassVisitor(vm, ctx).Visit(member.classDecl()),
        _ => throw new ArgumentOutOfRangeException(nameof(member.RuleIndex), member.RuleIndex, "Invalid Rule for member: " + member)
    };
    protected ExecutableCode VisitCode(ParserRuleContext? code) => code == null ? new ExecutableCode() : new CodeblockVisitor(vm, ctx).Visit(code);
    protected ExecutableCode VisitMemberCode(ParserRuleContext member) => new CodeblockVisitor(vm, ctx).Visit(member);
    protected Statement VisitStatement(KScrParser.StatementContext stmt) => new StatementVisitor(vm, ctx).Visit(stmt);
    protected StatementComponent VisitExpression(ParserRuleContext expr) => new ExpressionVisitor(vm, ctx).Visit(expr);

    protected new Statement VisitArguments(KScrParser.ArgumentsContext context)
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalse
        if (context == null)
            return new Statement()
                { Type = StatementComponentType.Code, CodeType = BytecodeType.ParameterExpression};
        var param = new Statement()
            { Type = StatementComponentType.Code, CodeType = BytecodeType.ParameterExpression};
        foreach (var expr in context.expr()) 
            param.Main.Add(VisitExpression(expr));
        return param;
    }

    public IClassInstance? FindType(string name)
    {
        if (!name.Contains('.') && ctx.Imports.FirstOrDefault(cls => cls.EndsWith(name)) is { } importedName)
            return vm.FindType(importedName, owner: ctx.Class!.AsClass(vm));
        return vm.FindType(name, ctx.Package);
    }

    public ITypeInfo? FindTypeInfo(KScrParser.TypeContext context) => VisitTypeInfo(context);

    public SourcefilePosition ToSrcPos(ParserRuleContext context) => Utils.ToSrcPos(context);
}