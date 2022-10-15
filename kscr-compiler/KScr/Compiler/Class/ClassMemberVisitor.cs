using System.Collections.Generic;
using System.Linq;
using KScr.Antlr;
using KScr.Core;
using KScr.Core.Bytecode;
using KScr.Core.Exception;
using KScr.Core.Model;

namespace KScr.Compiler.Class;

public class ClassMemberVisitor : AbstractVisitor<IClassMember>
{
    public ClassMemberVisitor(RuntimeBase vm, CompilerContext ctx) : base(vm, ctx)
    {
    }

    public override IClassMember VisitMemInit(KScrParser.MemInitContext memInit)
    {
        var context = memInit.initDecl();
        return new Method(ToSrcPos(context), ctx.Class!.AsClass(vm), Method.StaticInitializerName,
            Core.Std.Class.VoidType, MemberModifier.Private | MemberModifier.Static)
        {
            Body = VisitMemberCode(context.memberBlock()),
            CatchFinally = memInit.catchBlocks() == null ? null : VisitCatchBlocks(memInit.catchBlocks())
        };
    }

    public override IClassMember VisitMemCtor(KScrParser.MemCtorContext memCtor)
    {
        var context = memCtor.constructorDecl();
        var cls = ctx.Class!.AsClass(vm);
        var mod = VisitModifiers(context.modifiers()) | MemberModifier.Static;
        var ctor = new Method(ToSrcPos(context), cls, Method.ConstructorName, cls, mod)
        {
            Body = VisitMemberCode(context.memberBlock()),
            CatchFinally = memCtor.catchBlocks() == null ? null : VisitCatchBlocks(memCtor.catchBlocks())
        };
        List<Symbol> paramSymbols = new();
        foreach (var param in context.parameters().parameter())
        {
            var parameter = new MethodParameter
                { Name = param.idPart().GetText(), Type = VisitTypeInfo(param.type()) };
            ctor.Parameters.Add(parameter);
            paramSymbols.Add(ctx.RegisterSymbol(parameter.Name, parameter.Type, SymbolType.Parameter));
        }
        foreach (var super in context.subConstructorCalls().subConstructorCall())
        {
            var superType = ctx.FindType(vm, super.type().GetText())!;
            ctor.SuperCalls.Add(new StatementComponent()
            {
                Type = StatementComponentType.Code,
                CodeType = BytecodeType.ConstructorCall,
                Arg = superType.FullDetailedName,
                SubStatement = VisitArguments(super.arguments())
            });
        }
        if (!paramSymbols.All(ctx.UnregisterSymbol))
            throw new FatalException("Could not unregister all ctor parameter symbols");

        return ctor;
    }

    public override IClassMember VisitMemMtd(KScrParser.MemMtdContext memMtd)
    {
        var context = memMtd.methodDecl();
        var name = context.idPart().GetText();
        var type = VisitTypeInfo(context.type());
        var mod = VisitModifiers(context.modifiers());
        var mtd = new Method(ToSrcPos(context), ctx.Class!.AsClass(vm), name, type, mod)
        {
            Body = VisitMemberCode(context.memberBlock()),
            CatchFinally = memMtd.catchBlocks() == null ? null : VisitCatchBlocks(memMtd.catchBlocks())
        };
        foreach (var param in context.parameters().parameter())
            mtd.Parameters.Add(new MethodParameter
                { Name = param.idPart().GetText(), Type = VisitTypeInfo(param.type()) });
        return mtd;
    }

    public override IClassMember VisitMemIdx(KScrParser.MemIdxContext memIdx)
    {
        var context = memIdx.indexerMemberDecl();
        var type = VisitTypeInfo(context.type());
        var mod = VisitModifiers(context.modifiers());
        var idx = new Property(ToSrcPos(context), ctx.Class!.AsClass(vm), Method.IndexerName, type, mod);
        return new PropBlockVisitor(this, idx).Visit(context.propBlock());
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
                var stmt = new Statement
                {
                    Type = StatementComponentType.Expression,
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