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
    protected KScrParser parser { get; }

    protected AbstractVisitor(RuntimeBase vm, KScrParser parser, CompilerContext ctx)
    {
        this.vm = vm;
        this.parser = parser;
        this.ctx = ctx;
    }

    protected virtual Core.Bytecode.Class VisitClass(KScrParser.ClassDeclContext cls) => new ClassVisitor(vm, parser, ctx).Visit(cls);
    protected virtual ClassInfo VisitClassInfo(KScrParser.ClassDeclContext cls) => new ClassInfoVisitor(vm, parser, ctx).Visit(cls);
    protected virtual IClassMember VisitClassMember(KScrParser.MemberContext member) => member.RuleIndex switch
    {
        KScrParser.RULE_methodDecl => new MethodVisitor(vm, parser, ctx).Visit(member.methodDecl()),
        KScrParser.RULE_constructorDecl => new ConstructorVisitor(vm, parser, ctx).Visit(member.constructorDecl()),
        KScrParser.RULE_initDecl => new InitializerVisitor(vm, parser, ctx).Visit(member.initDecl()),
        KScrParser.RULE_propertyDecl => new PropertyVisitor(vm, parser, ctx).Visit(member.propertyDecl()),
        KScrParser.RULE_classDecl => new ClassVisitor(vm, parser, ctx).Visit(member.classDecl()),
        _ => throw new ArgumentOutOfRangeException(nameof(member.RuleIndex), member.RuleIndex, "Invalid Rule for member: " + member)
    };
    protected virtual ExecutableCode VisitCode(KScrParser.CodeBlockContext code) => new CodeblockVisitor(vm, parser, ctx).Visit(code);
    protected virtual Statement VisitStatement(KScrParser.StatementContext stmt) => new StatementVisitor(vm, parser, ctx).Visit(stmt);
    protected virtual StatementComponent VisitExpression(KScrParser.ExprContext expr) => new ExpressionVisitor(vm, parser, ctx).Visit(expr);

    public IClassInstance? FindType(string name)
    {
        if (ctx.Imports.FirstOrDefault(cls => cls.EndsWith(name)) is { } importedName)
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

    public SourcefilePosition ToSrcPos(ParserRuleContext context) => Utils.ToSrcPos(parser, context);
}