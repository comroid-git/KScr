using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Antlr4.Runtime;
using KScr.Antlr;
using KScr.Compiler.Class;
using KScr.Compiler.Code;
using KScr.Core;
using KScr.Core.Bytecode;
using KScr.Core.Model;

namespace KScr.Compiler;

public abstract class AbstractVisitor<T> : KScrParserBaseVisitor<T>
{
    protected RuntimeBase vm { get; }
    protected CompilerContext ctx { get; }

    protected AbstractVisitor(RuntimeBase vm, CompilerContext ctx)
    {
        this.vm = vm;
        this.ctx = ctx;
    }

    protected virtual Core.Bytecode.Class VisitClass(KScrParser.ClassDeclContext cls) => new ClassVisitor(vm, ctx).Visit(cls);
    protected virtual ClassInfo VisitClassInfo(KScrParser.ClassDeclContext cls) => new ClassInfoVisitor(vm, ctx).Visit(cls);
    protected virtual ITypeInfo VisitType(KScrParser.TypeContext type) => new TypeInfoVisitor(vm, ctx).Visit(type);

    protected virtual TypeParameter VisitTypeParameter(KScrParser.GenericTypeDefContext gtd)
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
            TypeParameterSpecializationType.Extends => VisitType(gtd.ext!),
            TypeParameterSpecializationType.Super => VisitType(gtd.sup!),
            _ => throw new ArgumentOutOfRangeException()
        };
        ITypeInfo? def = spec == TypeParameterSpecializationType.N ? new TypeInfo { Name = gtd.defN.Text }
            : gtd.def != null ? new TypeInfo { FullDetailedName = VisitType(gtd.def).FullDetailedName }
            : null;
        return new TypeParameter(name, spec, target.AsClassInstance(vm)) { DefaultValue = def };
    }
    protected virtual IClassMember VisitClassMember(KScrParser.MemberContext member) => member.RuleIndex switch
    {
        KScrParser.RULE_methodDecl => new MethodVisitor(vm, ctx).Visit(member.methodDecl()),
        KScrParser.RULE_constructorDecl => new ConstructorVisitor(vm, ctx).Visit(member.constructorDecl()),
        KScrParser.RULE_initDecl => new InitializerVisitor(vm, ctx).Visit(member.initDecl()),
        KScrParser.RULE_propertyDecl => new PropertyVisitor(vm, ctx).Visit(member.propertyDecl()),
        KScrParser.RULE_classDecl => new ClassVisitor(vm, ctx).Visit(member.classDecl()),
        _ => throw new ArgumentOutOfRangeException(nameof(member.RuleIndex), member.RuleIndex, "Invalid Rule for member: " + member)
    };
    protected virtual ExecutableCode VisitCode(KScrParser.CodeBlockContext code) => new CodeblockVisitor(vm, parser, ctx).Visit(code);
    protected virtual Statement VisitStatement(KScrParser.StatementContext stmt) => new StatementVisitor(vm, parser, ctx).Visit(stmt);
    protected virtual StatementComponent VisitExpression(KScrParser.ExprContext expr) => new ExpressionVisitor(vm, parser, ctx).Visit(expr);

    public IClassInstance? FindType(string name)
    {
        if (!name.Contains('.') && ctx.Imports.FirstOrDefault(cls => cls.EndsWith(name)) is { } importedName)
            return vm.FindType(importedName, owner: ctx.Class!.AsClass(vm));
        return vm.FindType(name, ctx.Package);
    }

    public ITypeInfo? FindTypeInfo(KScrParser.TypeContext context)
    {
        var type = FindType(vm, );
        if ((type?.CanonicalName.EndsWith(name) ?? false) && NextToken?.Type != TokenType.ParDiamondOpen)
            return type!;
        var baseCls = type?.BaseClass ?? ctx.Class;
        if (baseCls == null && ctx.Class.TypeParameters.Any(x => x.Name == name))
            // find base type param
            return ctx.Class.TypeParameters.Find(x => x.Name == name)!;
        if (baseCls!.TypeParameters.Count > 0 && NextToken?.Type == TokenType.ParDiamondOpen)
        {
            var args = new List<ITypeInfo>();
            do
            {
                TokenIndex += 2;
                if (Token.Type == TokenType.Word && Regex.IsMatch(Token.Arg!, "\\d+"))
                { // use tuple instead
                    return Class.TupleType.CreateInstance(vm, baseCls, 
                        new ITypeInfo[]{new TypeParameter(Token.Arg!, 
                            TypeParameterSpecializationType.N, Class.NumericIntType)});
                }
                args.Add(FindTypeInfo(vm, true) ?? throw new InvalidOperationException());
            } while (NextToken?.Type == TokenType.Comma);
            TokenIndex += 1;

            return baseCls.CreateInstance(vm, Class, args.ToArray());
        }

        return baseCls.TypeParameters.FirstOrDefault(x => x.Name == name);
    }

    public SourcefilePosition ToSrcPos(ParserRuleContext context) => Utils.ToSrcPos(context);
}