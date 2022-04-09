using System;
using KScr.Antlr;
using KScr.Core;
using KScr.Core.Bytecode;
using KScr.Core.Model;
using KScr.Core.Store;

namespace KScr.Compiler.Code;

public class ExpressionVisitor : AbstractVisitor<StatementComponent>
{
    public ExpressionVisitor(RuntimeBase vm, CompilerContext ctx) : base(vm, ctx)
    {
    }

    public override StatementComponent VisitCheckInstanceof(KScrParser.CheckInstanceofContext context)
    {
        throw new NotImplementedException("Compiling of expression " + context + " is not supported");
    }

    public override StatementComponent VisitReadArray(KScrParser.ReadArrayContext context)
    {
        throw new NotImplementedException("Compiling of expression " + context + " is not supported");
    }

    public override StatementComponent VisitDeclaration(KScrParser.DeclarationContext context)
    {
        return new StatementComponent
        {
            Type = StatementComponentType.Declaration,
            CodeType = context.expr() != null ? BytecodeType.Assignment : BytecodeType.Declaration,
            Arg = VisitTypeInfo(context.type()).FullDetailedName + ';' + context.idPart().GetText(),
            SubComponent = context.expr() is { } expr ? VisitExpression(expr) : null,
            SourcefilePosition = ToSrcPos(context)
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
        return context.mutation().binaryop() is { } op
            ? new StatementComponent
            {
                Type = StatementComponentType.Operator,
                CodeType = BytecodeType.Assignment,
                ByteArg = (ulong)(VisitOperator(op) | Operator.Compound | Operator.Binary),
                SubComponent = VisitExpression(context.left),
                AltComponent = VisitExpression(context.mutation().expr()),
                SourcefilePosition = ToSrcPos(context)
            }
            : new StatementComponent
            {
                Type = StatementComponentType.Code,
                CodeType = BytecodeType.Assignment,
                SubComponent = VisitExpression(context.left),
                AltComponent = VisitExpression(context.mutation()),
                SourcefilePosition = ToSrcPos(context)
            };
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
        return new StatementComponent
        {
            Type = StatementComponentType.Expression,
            CodeType = BytecodeType.ConstructorCall,
            Arg = FindTypeInfo(context.type())!.FullDetailedName,
            SubStatement = VisitArguments(context.arguments()),
            SourcefilePosition = ToSrcPos(context)
        };
    }

    public override StatementComponent VisitExprCallMember(KScrParser.ExprCallMemberContext context)
    {
        var expr = VisitExpression(context.expr());
        expr.PostComponent = new StatementComponent
        {
            Type = StatementComponentType.Expression,
            CodeType = BytecodeType.Call,
            Arg = context.idPart().GetText(),
            SubStatement = VisitArguments(context.arguments()),
            SourcefilePosition = ToSrcPos(context)
        };
        return expr;
    }

    public override StatementComponent VisitExprNullFallback(KScrParser.ExprNullFallbackContext context)
    {
        return new StatementComponent
        {
            Type = StatementComponentType.Expression,
            CodeType = BytecodeType.NullFallback,
            SubComponent = VisitExpression(context.nullable),
            AltComponent = VisitExpression(context.fallback),
            SourcefilePosition = ToSrcPos(context)
        };
    }

    public override StatementComponent VisitThrowStatement(KScrParser.ThrowStatementContext context)
    {
        return new StatementComponent
        {
            Type = StatementComponentType.Code,
            CodeType = BytecodeType.Throw,
            SubComponent = VisitExpression(context.expr()),
            SourcefilePosition = ToSrcPos(context)
        };
    }

    public override StatementComponent VisitSwitchStatement(KScrParser.SwitchStatementContext context)
    {
        throw new NotImplementedException("Compiling of expression " + context + " is not supported");
    }

    public override StatementComponent VisitExprCast(KScrParser.ExprCastContext context)
    {
        throw new NotImplementedException("Compiling of expression " + context + " is not supported");
    }

    public override StatementComponent VisitNewArrayValue(KScrParser.NewArrayValueContext context)
    {
        throw new NotImplementedException("Compiling of expression " + context + " is not supported");
    }

    public override StatementComponent VisitNewListedArrayValue(KScrParser.NewListedArrayValueContext context)
    {
        throw new NotImplementedException("Compiling of expression " + context + " is not supported");
    }

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
}