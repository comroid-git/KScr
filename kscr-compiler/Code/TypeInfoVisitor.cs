using System;
using System.Collections.Generic;
using System.Linq;
using KScr.Antlr;
using KScr.Core.Exception;
using KScr.Core.Model;
using KScr.Core.System;
using static KScr.Core.Exception.CompilerErrorMessage;

namespace KScr.Compiler.Code;

public class TypeInfoVisitor : AbstractVisitor<ITypeInfo>
{
    public TypeInfoVisitor(CompilerRuntime vm, CompilerContext ctx) : base(vm, ctx)
    {
    }

    public override ITypeInfo VisitId(KScrParser.IdContext context)
    {
        return FindType(context.GetText())
               ?? throw new CompilerException(ToSrcPos(context), TypeSymbolNotFound, context.GetText());
    }

    public override ITypeInfo VisitIdPart(KScrParser.IdPartContext context)
    {
        var name = context.GetText();
        return ctx.FindType(vm, name) ?? vm.FindType(name) ?? (ITypeInfo)new TypeParameter(name);
    }

    public override ITypeInfo VisitRawType(KScrParser.RawTypeContext context)
    {
        var clsName = context.GetText();
        return ctx.Class?.TypeParameters.FirstOrDefault(x => x.Name == clsName)
               ?? ctx.FindType(vm, clsName)
               ?? throw new CompilerException(ToSrcPos(context), TypeSymbolNotFound, clsName);
    }

    public override ITypeInfo VisitType(KScrParser.TypeContext context)
    {
        var raw = VisitRawType(context.rawType());
        var arr = context.indexerEmpty() != null || context.ELIPSES() != null;

        ITypeInfo It(Core.System.Class cls, IEnumerable<ITypeInfo>? args = null)
        {
            return cls.CreateInstance(vm, ctx.Class!.AsClass(vm), args?.ToArray() ?? Array.Empty<ITypeInfo>());
        }

        ITypeInfo Arr(ITypeInfo cls, IEnumerable<ITypeInfo>? args = null)
        {
            return It(Core.System.Class.ArrayType,
                new[] { cls }.Concat(args ?? ArraySegment<ITypeInfo>.Empty).ToArray());
        }

        ITypeInfo Tup(int n, ITypeInfo cls, IEnumerable<ITypeInfo>? args = null)
        {
            return It(Core.System.Class.TupleType,
                new[] { new TypeParameter(n, TypeParameterSpecializationType.N), cls }
                    .Concat(args ?? ArraySegment<ITypeInfo>.Empty).ToArray());
        }

        ITypeInfo IntN(int n, IEnumerable<ITypeInfo>? args = null)
        {
            return Core.System.Class.IntType.CreateInstance(vm, Core.System.Class.IntType,
                new[] { new TypeParameter(n, TypeParameterSpecializationType.N) }
                    .Concat(args ?? ArraySegment<ITypeInfo>.Empty).ToArray());
        }

        // null check
        if (raw == null)
            throw new CompilerException(ToSrcPos(context), TypeSymbolNotFound, context.rawType().GetText());
        // handle array type
        if (context.indexerEmpty() != null || context.ELIPSES() != null)
            return arr ? Arr(raw) : raw;
        // handle int<n>
        if (raw == Core.System.Class.IntType)
            return IntN(int.Parse(context.genericTypeUses()?.n?.Text ?? "32"));

        // collect other type args
        var args = new List<ITypeInfo>();
        foreach (var type in context.genericTypeUses().type())
            args.Add(Visit(type));
        // handle n -> tuple
        if (context.genericTypeUses().n is { } n)
            return Tup(int.Parse(n.Text), It(raw.AsClass(vm)));
        if (raw is Core.System.Class cls || (raw is IClassInstance inst && (cls = inst.BaseClass) != null))
            return arr ? Arr(cls, args) : It(cls, args);
        throw new CompilerException(ToSrcPos(context), TypeSymbolNotFound, context.rawType().GetText());
    }

    public override ITypeInfo VisitTypeLitObject(KScrParser.TypeLitObjectContext context)
    {
        return Core.System.Class.ObjectType;
    }

    public override ITypeInfo VisitTypeLitVoid(KScrParser.TypeLitVoidContext context)
    {
        return Core.System.Class.VoidType;
    }

    public override ITypeInfo VisitTypeLitArray(KScrParser.TypeLitArrayContext context)
    {
        return Core.System.Class.ArrayType;
    }

    public override ITypeInfo VisitTypeLitTuple(KScrParser.TypeLitTupleContext context)
    {
        return Core.System.Class.TupleType;
    }

    public override ITypeInfo VisitNumTypeLit(KScrParser.NumTypeLitContext context)
    {
        return Core.System.Class.NumericType;
    }

    public override ITypeInfo VisitNumTypeLitBool(KScrParser.NumTypeLitBoolContext context)
    {
        return Core.System.Class.BoolType;
    }

    public override ITypeInfo VisitNumTypeLitByte(KScrParser.NumTypeLitByteContext context)
    {
        return Core.System.Class.NumericByteType;
    }

    public override ITypeInfo VisitNumTypeLitShort(KScrParser.NumTypeLitShortContext context)
    {
        return Core.System.Class.NumericShortType;
    }

    public override ITypeInfo VisitNumTypeLitInt(KScrParser.NumTypeLitIntContext context)
    {
        return Core.System.Class.NumericIntType;
    }

    public override ITypeInfo VisitNumTypeLitLong(KScrParser.NumTypeLitLongContext context)
    {
        return Core.System.Class.NumericLongType;
    }

    public override ITypeInfo VisitNumTypeLitFloat(KScrParser.NumTypeLitFloatContext context)
    {
        return Core.System.Class.NumericFloatType;
    }

    public override ITypeInfo VisitNumTypeLitDouble(KScrParser.NumTypeLitDoubleContext context)
    {
        return Core.System.Class.NumericDoubleType;
    }

    public override ITypeInfo VisitTypeLitType(KScrParser.TypeLitTypeContext context)
    {
        return Core.System.Class.TypeType;
    }

    public override ITypeInfo VisitTypeLitEnum(KScrParser.TypeLitEnumContext context)
    {
        return Core.System.Class.EnumType;
    }
}