using System;
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

    public override ITypeInfo VisitId(KScrParser.IdContext context) {
        return FindType(context.GetText())
               ?? throw new CompilerException(ToSrcPos(context), TypeSymbolNotFound, context.GetText());
    }

    public override ITypeInfo VisitIdPart(KScrParser.IdPartContext context)
    {
        return FindType(context.GetText())
               ?? throw new CompilerException(ToSrcPos(context), TypeSymbolNotFound, context.GetText());
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
        if (raw is Core.Bytecode.Class cls || raw is IClassInstance inst && (cls = inst.BaseClass) != null)
            return cls.CreateInstance(vm, ctx.Class!.AsClass(vm), args.ToArray());
        throw new CompilerException(ToSrcPos(context), TypeSymbolNotFound, context.rawType().GetText());
    }

    public override ITypeInfo VisitArrayTypeUse(KScrParser.ArrayTypeUseContext context)
    {
        var raw = VisitRawType(context.rawType());
        if (raw == null)
            throw new CompilerException(ToSrcPos(context), TypeSymbolNotFound, context.rawType().GetText());
        if (context.genericTypeUses() == null)
            return Core.Bytecode.Class.ArrayType.CreateInstance(vm, ctx.Class!.AsClass(vm), raw);
        var args = new List<ITypeInfo>();
        foreach (var type in context.genericTypeUses().type()) 
            args.Add(Visit(type));
        if (raw is Core.Bytecode.Class cls || raw is IClassInstance inst && (cls = inst.BaseClass) != null) 
            return Core.Bytecode.Class.ArrayType.CreateInstance(vm, ctx.Class!.AsClass(vm), 
                cls.CreateInstance(vm, ctx.Class!.AsClass(vm), args.ToArray()));
        throw new CompilerException(ToSrcPos(context), TypeSymbolNotFound, context.rawType().GetText());
    }

    public override ITypeInfo VisitTypeLitObject(KScrParser.TypeLitObjectContext context) => Core.Bytecode.Class.ObjectType;

    public override ITypeInfo VisitTypeLitArray(KScrParser.TypeLitArrayContext context) => Core.Bytecode.Class.ArrayType;

    public override ITypeInfo VisitTypeLitTuple(KScrParser.TypeLitTupleContext context) => Core.Bytecode.Class.TupleType;

    public override ITypeInfo VisitNumTypeLitTuple(KScrParser.NumTypeLitTupleContext context)
    {
        var args = new List<ITypeInfo>();

        bool intN = context.Start.Type == KScrLexer.INT;
        if (context.genericTypeUses().n is {} n)
        {
            if (intN)
                args.Add(new TypeInfo() { Name = n.Text });
            else throw new CompilerException(ToSrcPos(context.genericTypeUses()), UnexpectedToken,
                    ctx.Class?.FullName, n.Text, "Int Literal expected");
        } else if (context.genericTypeUses().first is { } t)
        {
            args.Add(new TypeInfo() { Name = "1" });
            args.Add(Visit(t));
        }
        foreach (var t in context.genericTypeUses().type()) 
            args.Add(Visit(t));

        if (intN)
        { // is int<n> type
            return Core.Bytecode.Class.IntType.CreateInstance(vm, ctx.Class!.AsClass(vm), args.ToArray());
        }
        else
        { // is tuple<num<T>> type
            return Core.Bytecode.Class.TupleType.CreateInstance(vm, ctx.Class!.AsClass(vm), args.ToArray());
        }
    }

    public override ITypeInfo VisitNumTypeLitByte(KScrParser.NumTypeLitByteContext context) => Core.Bytecode.Class.NumericByteType;
    public override ITypeInfo VisitNumTypeLitShort(KScrParser.NumTypeLitShortContext context) => Core.Bytecode.Class.NumericShortType;
    public override ITypeInfo VisitNumTypeLitInt(KScrParser.NumTypeLitIntContext context) => Core.Bytecode.Class.NumericIntType;
    public override ITypeInfo VisitNumTypeLitLong(KScrParser.NumTypeLitLongContext context) => Core.Bytecode.Class.NumericLongType;
    public override ITypeInfo VisitNumTypeLitFloat(KScrParser.NumTypeLitFloatContext context) => Core.Bytecode.Class.NumericFloatType;
    public override ITypeInfo VisitNumTypeLitDouble(KScrParser.NumTypeLitDoubleContext context) => Core.Bytecode.Class.NumericDoubleType;

    public override ITypeInfo VisitTypeLitType(KScrParser.TypeLitTypeContext context) => Core.Bytecode.Class.TypeType;

    public override ITypeInfo VisitTypeLitEnum(KScrParser.TypeLitEnumContext context) => Core.Bytecode.Class.EnumType;
}