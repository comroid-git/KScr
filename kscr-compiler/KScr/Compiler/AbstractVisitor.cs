using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using KScr.Antlr;
using KScr.Compiler.Class;
using KScr.Compiler.Code;
using KScr.Core;
using KScr.Core.Bytecode;
using KScr.Core.Exception;
using KScr.Core.Model;
using KScr.Core.Std;

namespace KScr.Compiler;

public abstract class AbstractVisitor<T> : KScrParserBaseVisitor<T>
{
    protected AbstractVisitor(CompilerRuntime vm, CompilerContext ctx)
    {
        this.vm = vm;
        this.ctx = ctx;
    }

    protected CompilerRuntime vm { get; }
    protected CompilerContext ctx { get; }
    protected ITypeInfo? RequestedType { get; init; }

    protected override bool ShouldVisitNextChild(IRuleNode node, T currentResult)
    {
        return currentResult == null;
    }

    protected ITypeInfo VisitTypeInfo(KScrParser.TypeContext type)
    {
        return new TypeInfoVisitor(vm, ctx).Visit(type);
    }

    protected new MemberModifier VisitModifiers(KScrParser.ModifiersContext mods)
    {
        return new ModifierVisitor().Visit(mods);
    }

    protected Operator VisitOperator(ParserRuleContext op)
    {
        return new OperatorVisitor().Visit(op);
    }

    protected TypeParameter VisitTypeParameter(KScrParser.GenericTypeDefContext gtd)
    {
        var name = gtd.idPart().GetText();
        var spec = name == "n" ? TypeParameterSpecializationType.N
            : gtd.elp != null ? TypeParameterSpecializationType.List
            : gtd.ext != null ? TypeParameterSpecializationType.Extends
            : gtd.sup != null ? TypeParameterSpecializationType.Super
            : TypeParameterSpecializationType.Extends;
        var target = spec switch
        {
            TypeParameterSpecializationType.List => Core.Std.Class.Sequencable.CreateInstance(vm,
                Core.Std.Class.Sequencable,
                Core.Std.Class.ObjectType),
            TypeParameterSpecializationType.N => Core.Std.Class.NumericIntType,
            TypeParameterSpecializationType.Extends => gtd.ext == null
                ? Core.Std.Class.ObjectType.DefaultInstance
                : VisitTypeInfo(gtd.ext!),
            TypeParameterSpecializationType.Super => VisitTypeInfo(gtd.sup!),
            _ => throw new ArgumentOutOfRangeException()
        };
        ITypeInfo? def = spec == TypeParameterSpecializationType.N ? new TypeInfo { Name = gtd.defN.Text }
            : gtd.def != null ? new TypeInfo { FullDetailedName = VisitTypeInfo(gtd.def).FullDetailedName }
            : null;
        return new TypeParameter(name, spec, target.AsClassInstance(vm)) { DefaultValue = def };
    }

    protected IClassMember VisitClassMember(KScrParser.MemberContext member)
    {
        return member.RuleIndex switch
        {
            KScrParser.RULE_methodDecl or KScrParser.RULE_constructorDecl or KScrParser.RULE_initDecl
                or KScrParser.RULE_propertyDecl or KScrParser.RULE_member
                => new ClassMemberVisitor(vm, ctx).Visit(member),
            KScrParser.RULE_classDecl => new ClassVisitor(vm, ctx).Visit(member),
            _ => throw new ArgumentOutOfRangeException(nameof(member.RuleIndex), member.RuleIndex,
                "Invalid Rule for member: " + member)
        };
    }

    protected ExecutableCode VisitCode(ParserRuleContext? code)
    {
        return code == null ? new ExecutableCode() : new CodeblockVisitor(vm, ctx).Visit(code);
    }

    protected ExecutableCode VisitMemberCode(ParserRuleContext member)
    {
        return new CodeblockVisitor(vm, ctx).Visit(member);
    }

    protected Statement VisitStatement(ParserRuleContext stmt)
    {
        return new StatementVisitor(vm, ctx).Visit(stmt);
    }

    protected StatementComponent VisitExpression(ParserRuleContext expr, ITypeInfo? requestedType = null)
    {
        requestedType ??= Core.Std.Class.VoidType; // todo Remove
        return new ExpressionVisitor(vm, ctx) { RequestedType = requestedType }.Visit(expr);
    }

    protected Statement VisitPipeRead(KScrParser.ExprContext pipe, KScrParser.ExprContext[] reads)
    {
        var stmt = new Statement
        {
            Type = StatementComponentType.Pipe,
            Main = { VisitExpression(pipe) }
        };
        foreach (var read in reads)
            stmt.Main.Add(new StatementComponent
            {
                Type = StatementComponentType.Consumer,
                SubComponent = VisitExpression(read)
            });
        return stmt;
    }

    protected Statement VisitPipeWrite(KScrParser.ExprContext pipe, KScrParser.ExprContext[] writes)
    {
        var stmt = new Statement
        {
            Type = StatementComponentType.Pipe,
            Main = { VisitExpression(pipe) }
        };
        foreach (var write in writes)
            stmt.Main.Add(new StatementComponent
            {
                Type = StatementComponentType.Emitter,
                SubComponent = VisitExpression(write)
            });
        return stmt;
    }

    protected Statement VisitPipeListen(KScrParser.ExprContext pipe, KScrParser.LambdaContext[] listeners)
    {
        var stmt = new Statement
        {
            Type = StatementComponentType.Pipe,
            Main = { VisitExpression(pipe) }
        };
        foreach (var listener in listeners)
            stmt.Main.Add(new StatementComponent
            {
                Type = StatementComponentType.Pipe,
                CodeType = BytecodeType.Call,
                SubComponent = listener is KScrParser.MethodRefContext mRef
                    ? VisitMethodRef(mRef)
                    : listener is KScrParser.LambdaExprContext lExpr
                        ? VisitLambdaExpr(lExpr)
                        : throw new CompilerException(ToSrcPos(listener), CompilerErrorMessage.Invalid, "syntax",
                            ctx.Class, "expected Lambda or Method reference")
            });
        return stmt;
    }

    protected new StatementComponent VisitMethodRef(KScrParser.MethodRefContext context)
    {
        return new StatementComponent
        {
            Type = StatementComponentType.Lambda,
            CodeType = BytecodeType.Call,
            Arg = context.label()?.GetText() ?? "unlabelled",
            Args =
            {
                ctx.Class!.CanonicalName,
                VisitTypeInfo(context.type()).CanonicalName,
                context.idPart().GetText()
            }
        };
    }

    protected new StatementComponent VisitLambdaExpr(KScrParser.LambdaExprContext context)
    {
        var args = new Statement
        {
            Type = StatementComponentType.Lambda,
            CodeType = BytecodeType.ParameterExpression
        };
        var sym = new List<Symbol>();
        try
        {
            foreach (var paramDecl in context.tupleExpr().typedExpr())
            {
                var pType = paramDecl.type() != null ? VisitTypeInfo(paramDecl.type()) : Core.Std.Class.VoidType;
                sym.Add(ctx.RegisterSymbol(paramDecl.expr().GetText(), pType));
                args.Main.Add(new StatementComponent
                {
                    Type = StatementComponentType.Declaration,
                    CodeType = BytecodeType.ParameterExpression,
                    Args =
                    {
                        pType
                            .CanonicalName,
                        paramDecl.expr().GetText()
                    }
                });
            }

            return new StatementComponent
            {
                Type = StatementComponentType.Lambda,
                CodeType = BytecodeType.Statement,
                SubStatement = args,
                Arg = context.label()?.GetText() ?? "unlabelled",
                Args = { ctx.Class!.CanonicalName },
                InnerCode = VisitCode(context.lambdaBlock())
            };
        }
        finally
        {
            foreach (var symbol in sym)
                ctx.UnregisterSymbol(symbol);
        }
    }

    protected new StatementComponent VisitCatchBlocks(KScrParser.CatchBlocksContext context)
    {
        var comp = new StatementComponent
        {
            Type = StatementComponentType.Code,
            CodeType = BytecodeType.StmtCatch,
            SubStatement = new Statement { Type = StatementComponentType.Code, CodeType = BytecodeType.StmtCatch },
            SourcefilePosition = ToSrcPos(context)
        };
        // catches
        foreach (var katchow in context.catchBlock())
        {
            var types = katchow.type()
                .Select(VisitTypeInfo)
                .ToList();
            var name = katchow.idPart().GetText();
            foreach (var type in types)
                ctx.RegisterSymbol(name, type);
            var innerCode = VisitCode(katchow.codeBlock());
            if (ctx.UnregisterSymbols(name) != types.Count)
                throw new FatalException("Could not unregister all catch variables");
            comp.SubStatement.Main.Add(new StatementComponent
            {
                Type = StatementComponentType.Code,
                CodeType = BytecodeType.StmtCatch,
                Arg = name,
                Args = katchow.type() == null
                    ? new List<string>()
                    : types.Select(x => x.CanonicalName).ToList(),
                InnerCode = innerCode,
                SourcefilePosition = ToSrcPos(katchow)
            });
        }

        // finally
        if (context.finallyBlock() is { } finalli)
            comp.AltComponent = new StatementComponent
            {
                Type = StatementComponentType.Code,
                CodeType = BytecodeType.StmtFinally,
                InnerCode = VisitCode(finalli.codeBlock()),
                SourcefilePosition = ToSrcPos(finalli)
            };
        return comp;
    }

    protected new Statement VisitArguments(KScrParser.ArgumentsContext context)
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalse
        if (context == null)
            return new Statement { Type = StatementComponentType.Code, CodeType = BytecodeType.ParameterExpression };
        var param = new Statement { Type = StatementComponentType.Code, CodeType = BytecodeType.ParameterExpression };
        foreach (var expr in context.expr())
            param.Main.Add(VisitExpression(expr));
        return param;
    }

    protected new Statement VisitIndexerExpr(KScrParser.IndexerExprContext? context)
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalse
        if (context == null)
            return new Statement { Type = StatementComponentType.Code, CodeType = BytecodeType.Indexer };
        var param = new Statement { Type = StatementComponentType.Code, CodeType = BytecodeType.Indexer };
        foreach (var expr in context.expr())
            param.Main.Add(VisitExpression(expr));
        return param;
    }

    protected Statement VisitArrayInitializer(KScrParser.ExprContext[] context)
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalse
        if (context == null)
            return new Statement { Type = StatementComponentType.Code, CodeType = BytecodeType.ArrayConstructor };
        var param = new Statement { Type = StatementComponentType.Code, CodeType = BytecodeType.ArrayConstructor };
        foreach (var expr in context)
            param.Main.Add(VisitExpression(expr));
        return param;
    }

    public IClassInstance? FindType(string name)
    {
        if (!name.Contains('.') && ctx.Imports.FirstOrDefault(cls => cls.EndsWith(name)) is { } importedName)
            return vm.FindType(importedName, owner: ctx.Class!.AsClass(vm));
        return vm.FindType(name, ctx.Package);
    }

    public ITypeInfo? FindTypeInfo(KScrParser.TypeContext context)
    {
        return VisitTypeInfo(context);
    }

    public SourcefilePosition ToSrcPos(ParserRuleContext context)
    {
        return Utils.ToSrcPos(context);
    }
}