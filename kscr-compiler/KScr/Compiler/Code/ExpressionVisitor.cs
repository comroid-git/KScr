using System;
using System.Linq;
using Antlr4.Runtime;
using KScr.Antlr;
using KScr.Core;
using KScr.Core.Bytecode;
using KScr.Core.Exception;
using KScr.Core.Model;
using KScr.Core.Std;
using KScr.Core.Store;

namespace KScr.Compiler.Code;

public class ExpressionVisitor : AbstractVisitor<StatementComponent>
{
    public ExpressionVisitor(RuntimeBase vm, CompilerContext ctx) : base(vm, ctx)
    {
    }

    public override StatementComponent VisitCheckInstanceof(KScrParser.CheckInstanceofContext context) => new()
    {
        Type = StatementComponentType.Provider,
        CodeType = BytecodeType.Instanceof,
        Arg = VisitTypeInfo(context.type()).FullDetailedName,
        SubComponent = VisitExpression(context.expr())
    };

    public override StatementComponent VisitReadIndexer(KScrParser.ReadIndexerContext context)
    {
        return new StatementComponent()
        {
            Type = StatementComponentType.Provider,
            CodeType = BytecodeType.Indexer,
            SubComponent = VisitExpression(context.target),
            SubStatement = VisitIndexerUse(context.indexerUse())
        };
    }

    public override StatementComponent VisitDeclaration(KScrParser.DeclarationContext context)
    {
        var type = VisitTypeInfo(context.type());
        var name = context.idPart().GetText();
        ctx.RegisterSymbol(name, type);
        return new StatementComponent
        {
            Type = StatementComponentType.Declaration,
            CodeType = context.expr() != null ? BytecodeType.Assignment : BytecodeType.Declaration,
            Args = { type.FullDetailedName, name },
            SubComponent = context.expr() is { } expr ? VisitExpression(expr) : null,
            SourcefilePosition = ToSrcPos(context.idPart())
        };
    }

    public override StatementComponent VisitMutation(KScrParser.MutationContext context)
    {
        if (context.binaryop() is { } op)
            return new StatementComponent
            {
                Type = StatementComponentType.Operator,
                CodeType = BytecodeType.Assignment,
                ByteArg = (ulong)(VisitOperator(op) | Operator.Compound),
                SubComponent = VisitExpression(context.expr())
            };
        return VisitExpression(context.expr());
    }

    public override StatementComponent VisitVarAssign(KScrParser.VarAssignContext context)
    {
        var left = VisitExpression(context.left);
        if (context.mutation().binaryop() is { } op)
            return new StatementComponent
            {
                Type = StatementComponentType.Operator,
                CodeType = BytecodeType.Assignment,
                ByteArg = (ulong)(VisitOperator(op) | Operator.Compound | Operator.Binary),
                SubComponent = left,
                AltComponent = VisitExpression(context.mutation().expr()),
                SourcefilePosition = ToSrcPos(context)
            };
        else
        {
            var right = VisitExpression(context.mutation());
            var actualType = left.OutputType(vm, ctx);
            var varType = right.OutputType(vm, ctx);
            if (!actualType.AsClass(vm).CanHold(varType.AsClass(vm)))
                throw new CompilerException(ToSrcPos(context.mutation().expr()), CompilerErrorMessage.CannotAssign, actualType, varType);
            return new StatementComponent
            {
                Type = StatementComponentType.Code,
                CodeType = BytecodeType.Assignment,
                SubComponent = left,
                AltComponent = right,
                SourcefilePosition = ToSrcPos(context)
            };
        }
    }

    public override StatementComponent VisitOpPrefix(KScrParser.OpPrefixContext context)
    {
        return new StatementComponent
        {
            Type = StatementComponentType.Operator,
            CodeType = BytecodeType.Operator,
            ByteArg = (ulong)(new OperatorVisitor().Visit(context.prefixop()) | Operator.UnaryPrefix),
            SubComponent = VisitExpression(context.expr()),
            SourcefilePosition = ToSrcPos(context)
        };
    }

    public override StatementComponent VisitOpBinary(KScrParser.OpBinaryContext context)
    {
        return new StatementComponent
        {
            Type = StatementComponentType.Operator,
            CodeType = BytecodeType.Operator,
            ByteArg = (ulong)(new OperatorVisitor().Visit(context.binaryop()) | Operator.Binary),
            SubComponent = VisitExpression(context.left),
            AltComponent = VisitExpression(context.right),
            SourcefilePosition = ToSrcPos(context)
        };
    }

    public override StatementComponent VisitOpPostfix(KScrParser.OpPostfixContext context)
    {
        return new StatementComponent
        {
            Type = StatementComponentType.Operator,
            CodeType = BytecodeType.Operator,
            ByteArg = (ulong)(new OperatorVisitor().Visit(context.postfixop()) | Operator.UnaryPostfix),
            SubComponent = VisitExpression(context.expr()),
            SourcefilePosition = ToSrcPos(context)
        };
    }

    public override StatementComponent VisitParens(KScrParser.ParensContext context)
    {
        return new StatementComponent
        {
            Type = StatementComponentType.Expression,
            CodeType = BytecodeType.Parentheses,
            SubComponent = VisitExpression(context.expr()),
            SourcefilePosition = ToSrcPos(context)
        };
    }

    public override StatementComponent VisitCtorCall(KScrParser.CtorCallContext context)
    {
        var comp = new StatementComponent
        {
            Type = StatementComponentType.Expression,
            CodeType = BytecodeType.ConstructorCall,
            Arg = FindTypeInfo(context.type())!.FullDetailedName,
            SubStatement = VisitArguments(context.arguments()),
            SourcefilePosition = ToSrcPos(context)
        };
        var rtrn = comp.OutputType(vm, ctx);
        if (!RequestedType!.AsClass(vm).CanHold(rtrn.AsClass(vm)))
            throw new CompilerException(ToSrcPos(context.type()), CompilerErrorMessage.InvalidType, ctx.Class,
                rtrn.FullDetailedName, "Expected derivate of " + RequestedType);
        return comp;
    }

    public override StatementComponent VisitExprCallMember(KScrParser.ExprCallMemberContext context)
    {
        var expr = VisitExpression(context.expr());
        var memberName = context.idPart().GetText();
        expr.AppendPostComponent(new StatementComponent
        {
            Type = StatementComponentType.Expression,
            CodeType = BytecodeType.Call,
            Arg = memberName,
            SubStatement = VisitArguments(context.arguments()),
            SourcefilePosition = ToSrcPos(context.idPart())
        });
        bool isExtCall = expr.CodeType is BytecodeType.ExpressionVariable or BytecodeType.LiteralString
            or BytecodeType.LiteralRange or BytecodeType.LiteralNumeric or BytecodeType.LiteralTrue
            or BytecodeType.LiteralFalse or BytecodeType.TypeExpression;
        ITypeInfo rtrn;
        try
        {
            if (isExtCall)
                ctx.PushContext(expr.OutputType(vm, ctx, ignorePostComp: true).AsClass(vm));
            rtrn = expr.PostComponent.OutputType(vm, ctx, expr);
        }
        finally
        {
            if (isExtCall)
                ctx.DropContext();
        }
        if (!RequestedType!.AsClass(vm).CanHold(rtrn.AsClass(vm)))
            throw new CompilerException(ToSrcPos(context.expr()), CompilerErrorMessage.InvalidType, ctx.Class,
                rtrn.FullDetailedName, "Expected derivate of " + RequestedType);
        return expr;
    }

    public override StatementComponent VisitThrowStatement(KScrParser.ThrowStatementContext context)
    {
        var expr = VisitExpression(context.expr());
        var comp = new StatementComponent
        {
            Type = StatementComponentType.Code,
            CodeType = BytecodeType.Throw,
            SubComponent = expr,
            SourcefilePosition = ToSrcPos(context)
        };
        var rtrn = comp.OutputType(vm, ctx);
        if (!Core.Std.Class.ThrowableType.CanHold(rtrn.AsClass(vm)))
            throw new CompilerException(ToSrcPos(context.expr()), CompilerErrorMessage.InvalidType, ctx.Class,
                rtrn.FullDetailedName, "Expected derivate of Throwable");
        return comp;
    }

    public override StatementComponent VisitSwitchStatement(KScrParser.SwitchStatementContext context)
    {
        var comp = new StatementComponent
        {
            Type = StatementComponentType.Code,
            CodeType = BytecodeType.StmtSwitch,
            // condition
            SubComponent = VisitExpression(context.tupleExpr()),
            SubStatement = new Statement
            {
                Type = StatementComponentType.Code,
                CodeType = BytecodeType.StmtSwitch | BytecodeType.StmtCase
            }
        };
        // cases
        foreach (var cas in context.caseClause())
            comp.SubStatement.Main.Add(new StatementComponent
            {
                Type = StatementComponentType.Code,
                CodeType = BytecodeType.StmtCase,
                // case condition
                SubComponent = VisitExpression(cas.tupleExpr()),
                // case body
                InnerCode = VisitCode(cas.caseBlock())
            });
        // default case
        if (context.defaultClause() is { } def)
            comp.SubStatement.Main.Add(new StatementComponent
            {
                Type = StatementComponentType.Code,
                CodeType = BytecodeType.StmtCase | BytecodeType.StmtElse,
                // case body
                InnerCode = VisitCode(def.caseBlock())
            });

        return comp;
    }

    public override StatementComponent VisitTupleExpr(KScrParser.TupleExprContext context)
    {
        var expr = new StatementComponent()
        {
            Type = StatementComponentType.Code,
            CodeType = BytecodeType.TupularExpression,
            SubStatement = new Statement()
        };
        foreach (var ctx in context.expr())
            expr.SubStatement.Main.Add(VisitExpression(ctx));
        return expr;
    }

    public override StatementComponent VisitCast(KScrParser.CastContext context) => new()
    {
        Type = StatementComponentType.Expression,
        CodeType = BytecodeType.Cast,
        Arg = VisitTypeInfo(context.type()).FullDetailedName,
        SubComponent = VisitExpression(context.expr())
    };

    public override StatementComponent VisitNewArray(KScrParser.NewArrayContext context) => new()
    {
        Type = StatementComponentType.Provider,
        CodeType = BytecodeType.ArrayConstructor,
        ByteArg = 0,
        Arg = VisitTypeInfo(context.type()).FullDetailedName,
        SubStatement = VisitIndexerUse(context.indexerUse())
    };

    public override StatementComponent VisitNewListedArray(KScrParser.NewListedArrayContext context) => new()
    {
        Type = StatementComponentType.Provider,
        CodeType = BytecodeType.ArrayConstructor,
        ByteArg = 1,
        Arg = VisitTypeInfo(context.type()).FullDetailedName,
        SubStatement = VisitArrayInitializer(context.expr())
    };

    public override StatementComponent VisitTypeValue(KScrParser.TypeValueContext context)
    {
        return new StatementComponent
        {
            Type = StatementComponentType.Expression,
            CodeType = BytecodeType.TypeExpression,
            Arg = VisitTypeInfo(context.type()).FullDetailedName,
            SourcefilePosition = ToSrcPos(context)
        };
    }

    public override StatementComponent VisitVarThis(KScrParser.VarThisContext context)
    {
        return new StatementComponent
        {
            Type = StatementComponentType.Expression,
            VariableContext = VariableContext.This,
            SourcefilePosition = ToSrcPos(context)
        };
    }

    public override StatementComponent VisitVarSuper(KScrParser.VarSuperContext context)
    {
        return new StatementComponent
        {
            Type = StatementComponentType.Expression,
            VariableContext = VariableContext.Super,
            SourcefilePosition = ToSrcPos(context)
        };
    }

    public override StatementComponent VisitVarLitNum(KScrParser.VarLitNumContext context)
    {
        return new StatementComponent
        {
            Type = StatementComponentType.Expression,
            CodeType = BytecodeType.LiteralNumeric,
            Arg = Numeric.Compile(vm, context.GetText()).Value.ToString(IObject.ToString_LongName),
            SourcefilePosition = ToSrcPos(context)
        };
    }

    public override StatementComponent VisitVarLitTrue(KScrParser.VarLitTrueContext context)
    {
        return new StatementComponent
        {
            Type = StatementComponentType.Expression,
            CodeType = BytecodeType.LiteralTrue,
            SourcefilePosition = ToSrcPos(context)
        };
    }

    public override StatementComponent VisitVarLitFalse(KScrParser.VarLitFalseContext context)
    {
        return new StatementComponent
        {
            Type = StatementComponentType.Expression,
            CodeType = BytecodeType.LiteralFalse,
            SourcefilePosition = ToSrcPos(context)
        };
    }

    public override StatementComponent VisitVarLitStr(KScrParser.VarLitStrContext context)
    {
        var txt = context.GetText();
        return new StatementComponent
        {
            Type = StatementComponentType.Expression,
            CodeType = BytecodeType.LiteralString,
            Arg = txt.Substring(txt.IndexOf('"') + 1, txt.LastIndexOf('"') - 1),
            SourcefilePosition = ToSrcPos(context)
        };
    }

    public override StatementComponent VisitVarLitStdio(KScrParser.VarLitStdioContext context)
    {
        return new StatementComponent
        {
            Type = StatementComponentType.Expression,
            CodeType = BytecodeType.StdioExpression,
            SourcefilePosition = ToSrcPos(context)
        };
    }

    public override StatementComponent VisitVarLitEndl(KScrParser.VarLitEndlContext context)
    {
        return new StatementComponent
        {
            Type = StatementComponentType.Expression,
            CodeType = BytecodeType.EndlExpression,
            SourcefilePosition = ToSrcPos(context)
        };
    }

    public override StatementComponent VisitVarLitNull(KScrParser.VarLitNullContext context)
    {
        return new StatementComponent
        {
            Type = StatementComponentType.Expression,
            CodeType = BytecodeType.Null,
            SourcefilePosition = ToSrcPos(context)
        };
    }

    public override StatementComponent VisitIdPart(KScrParser.IdPartContext context)
    {
        return FindType(context.GetText()) is { } imported
            ? new StatementComponent
            {
                Type = StatementComponentType.Expression,
                CodeType = BytecodeType.TypeExpression,
                Arg = imported.CanonicalName,
                SourcefilePosition = ToSrcPos(context)
            }
            : new StatementComponent
            {
                Type = StatementComponentType.Provider,
                CodeType = BytecodeType.ExpressionVariable,
                Arg = context.GetText(),
                SourcefilePosition = ToSrcPos(context)
            };
    }

    public override StatementComponent VisitRangeInvoc(KScrParser.RangeInvocContext context)
    {
        return new StatementComponent
        {
            Type = StatementComponentType.Provider,
            CodeType = BytecodeType.LiteralRange,
            SubComponent = VisitExpression(context.left),
            AltComponent = VisitExpression(context.right),
            SourcefilePosition = ToSrcPos(context)
        };
    }
    
    public override StatementComponent VisitExprPipeListen(KScrParser.ExprPipeListenContext context) => new()
    {
        Type = StatementComponentType.Code,
        CodeType = BytecodeType.Parentheses,
        SubStatement = VisitPipeListen(context.pipe, context.expr()[1..]) 
    };
}

public class OperatorVisitor : KScrParserBaseVisitor<Operator>
{
    public override Operator VisitOpPlus(KScrParser.OpPlusContext context)
    {
        return Operator.Plus;
    }

    public override Operator VisitOpMinus(KScrParser.OpMinusContext context)
    {
        return Operator.Minus;
    }

    public override Operator VisitOpMultiply(KScrParser.OpMultiplyContext context)
    {
        return Operator.Multiply;
    }

    public override Operator VisitOpDivide(KScrParser.OpDivideContext context)
    {
        return Operator.Divide;
    }

    public override Operator VisitOpModulus(KScrParser.OpModulusContext context)
    {
        return Operator.Modulus;
    }

    public override Operator VisitOpBitAnd(KScrParser.OpBitAndContext context)
    {
        return Operator.BitAnd;
    }

    public override Operator VisitOpBitOr(KScrParser.OpBitOrContext context)
    {
        return Operator.BitOr;
    }

    public override Operator VisitOpLogicAnd(KScrParser.OpLogicAndContext context)
    {
        return Operator.LogicAnd;
    }

    public override Operator VisitOpLogicOr(KScrParser.OpLogicOrContext context)
    {
        return Operator.LogicOr;
    }

    public override Operator VisitOpLogicNot(KScrParser.OpLogicNotContext context)
    {
        return Operator.LogicNot;
    }

    public override Operator VisitOpPow(KScrParser.OpPowContext context)
    {
        return Operator.Pow;
    }

    public override Operator VisitOpEqual(KScrParser.OpEqualContext context)
    {
        return Operator.Equals;
    }

    public override Operator VisitOpInequal(KScrParser.OpInequalContext context)
    {
        return Operator.NotEquals;
    }

    public override Operator VisitOpGreaterEq(KScrParser.OpGreaterEqContext context)
    {
        return Operator.GreaterEq;
    }

    public override Operator VisitOpLesserEq(KScrParser.OpLesserEqContext context)
    {
        return Operator.LesserEq;
    }

    public override Operator VisitOpGreater(KScrParser.OpGreaterContext context)
    {
        return Operator.Greater;
    }

    public override Operator VisitOpLesser(KScrParser.OpLesserContext context)
    {
        return Operator.Lesser;
    }

    public override Operator VisitOpLShift(KScrParser.OpLShiftContext context)
    {
        return Operator.LShift;
    }

    public override Operator VisitOpRShift(KScrParser.OpRShiftContext context)
    {
        return Operator.RShift;
    }

    public override Operator VisitOpULShift(KScrParser.OpULShiftContext context)
    {
        return Operator.ULShift;
    }

    public override Operator VisitOpURShift(KScrParser.OpURShiftContext context)
    {
        return Operator.URShift;
    }

    public override Operator VisitOpArithNot(KScrParser.OpArithNotContext context)
    {
        return Operator.ArithmeticNot;
    }

    public override Operator VisitOpIncrRead(KScrParser.OpIncrReadContext context)
    {
        return Operator.IncrementRead;
    }

    public override Operator VisitOpDecrRead(KScrParser.OpDecrReadContext context)
    {
        return Operator.DecrementRead;
    }

    public override Operator VisitOpReadIncr(KScrParser.OpReadIncrContext context)
    {
        return Operator.ReadIncrement;
    }

    public override Operator VisitOpReadDecr(KScrParser.OpReadDecrContext context)
    {
        return Operator.ReadDecrement;
    }

    public override Operator VisitOpNullFallback(KScrParser.OpNullFallbackContext context)
    {
        return Operator.NullFallback;
    }
}