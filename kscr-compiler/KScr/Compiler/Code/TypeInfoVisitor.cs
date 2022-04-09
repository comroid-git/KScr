using System.Collections.Generic;
using KScr.Antlr;
using KScr.Core;
using KScr.Core.Exception;
using KScr.Core.Model;
using static KScr.Core.Exception.CompilerError;

namespace KScr.Compiler.Code;

public class TypeInfoVisitor : AbstractVisitor<ITypeInfo>
{
    public TypeInfoVisitor(RuntimeBase vm, CompilerContext ctx) : base(vm, ctx)
    {
    }

    public override ITypeInfo VisitId(KScrParser.IdContext context)
    {
        return FindType(context.GetText())
               ?? throw new CompilerException(ToSrcPos(context), TypeSymbolNotFound, context.GetText());
    }

    public override ITypeInfo VisitIdPart(KScrParser.IdPartContext context)
    {
        return (ITypeInfo?)FindType(context.GetText()) ?? new TypeParameter(context.GetText());
    }

    public override ITypeInfo VisitNormalTypeUse(KScrParser.NormalTypeUseContext context)
    {
        var raw = VisitRawType(context.rawType());
        if (raw == null)
            throw new CompilerException(ToSrcPos(context), TypeSymbolNotFound, context.rawType().GetText());
        if (context.genericTypeUses() == null)
            return raw;
        var args = new List<ITypeInfo>();
        foreach (var type in context.genericTypeUses().type())
            args.Add(Visit(type));
        if (raw is Core.Class cls || raw is IClassInstance inst && (cls = inst.BaseClass) != null)
            return cls.CreateInstance(vm, ctx.Class!.AsClass(vm), args.ToArray());
        throw new CompilerException(ToSrcPos(context), TypeSymbolNotFound, context.rawType().GetText());
    }

    public override ITypeInfo VisitArrayTypeUse(KScrParser.ArrayTypeUseContext context)
    {
        var raw = VisitRawType(context.rawType());
        if (raw == null)
            throw new CompilerException(ToSrcPos(context), TypeSymbolNotFound, context.rawType().GetText());
        if (context.genericTypeUses() == null)
            return Core.Class.ArrayType.CreateInstance(vm, ctx.Class!.AsClass(vm), raw);
        var args = new List<ITypeInfo>();
        foreach (var type in context.genericTypeUses().type())
            args.Add(Visit(type));
        if (raw is Core.Class cls || raw is IClassInstance inst && (cls = inst.BaseClass) != null)
            return Core.Class.ArrayType.CreateInstance(vm, ctx.Class!.AsClass(vm),
                cls.CreateInstance(vm, ctx.Class!.AsClass(vm), args.ToArray()));
        throw new CompilerException(ToSrcPos(context), TypeSymbolNotFound, context.rawType().GetText());
    }

    public override ITypeInfo VisitTypeLitObject(KScrParser.TypeLitObjectContext context)
    {
        return Core.Class.ObjectType;
    }

    public override ITypeInfo VisitTypeLitVoid(KScrParser.TypeLitVoidContext context)
    {
        return Core.Class.VoidType;
    }

    public override ITypeInfo VisitTypeLitArray(KScrParser.TypeLitArrayContext context)
    {
        return Core.Class.ArrayType;
    }

    public override ITypeInfo VisitTypeLitTuple(KScrParser.TypeLitTupleContext context)
    {
        return Core.Class.TupleType;
    }

    public override ITypeInfo VisitNumTypeLitTuple(KScrParser.NumTypeLitTupleContext context)
    {
        var args = new List<ITypeInfo>();

        var intN = context.Start.Type == KScrLexer.INT;
        if (context.Start.Type == KScrLexer.NUMIDENT && context.genericTypeUses() == null)
            return Core.Class.NumericType;
        if (context.genericTypeUses() is { n: { } n })
        {
            if (intN)
                args.Add(new TypeInfo { Name = "n", DetailedName = n.Text });
            else
                throw new CompilerException(ToSrcPos(context.genericTypeUses()), UnexpectedToken,
                    ctx.Class?.FullName, n.Text, "Int Literal expected");
        }
        else if (context.genericTypeUses() is { first: { } t })
        {
            args.Add(new TypeInfo { Name = "n", DetailedName = "1" });
            args.Add(Visit(t));
        }
        else if (intN)
        {
            args.Add(new TypeInfo { Name = "n", DetailedName = "32" });
        }

        if (context.genericTypeUses() is { } genUse)
            foreach (var t in genUse.type())
                args.Add(Visit(t));

        if (intN)
            // is int<n> type
            return Core.Class.IntType.CreateInstance(vm, ctx.Class!.AsClass(vm), args.ToArray());
        return Core.Class.TupleType.CreateInstance(vm, ctx.Class!.AsClass(vm), args.ToArray());
    }

    public override ITypeInfo VisitNumTypeLitByte(KScrParser.NumTypeLitByteContext context)
    {
        return Core.Class.NumericByteType;
    }

    public override ITypeInfo VisitNumTypeLitShort(KScrParser.NumTypeLitShortContext context)
    {
        return Core.Class.NumericShortType;
    }

    public override ITypeInfo VisitNumTypeLitInt(KScrParser.NumTypeLitIntContext context)
    {
        return Core.Class.NumericIntType;
    }

    public override ITypeInfo VisitNumTypeLitLong(KScrParser.NumTypeLitLongContext context)
    {
        return Core.Class.NumericLongType;
    }

    public override ITypeInfo VisitNumTypeLitFloat(KScrParser.NumTypeLitFloatContext context)
    {
        return Core.Class.NumericFloatType;
    }

    public override ITypeInfo VisitNumTypeLitDouble(KScrParser.NumTypeLitDoubleContext context)
    {
        return Core.Class.NumericDoubleType;
    }

    public override ITypeInfo VisitTypeLitType(KScrParser.TypeLitTypeContext context)
    {
        return Core.Class.TypeType;
    }

    public override ITypeInfo VisitTypeLitEnum(KScrParser.TypeLitEnumContext context)
    {
        return Core.Class.EnumType;
    }
}