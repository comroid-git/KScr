using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Net.Mime;
using KScr.Antlr;
using KScr.Core;
using KScr.Core.Bytecode;
using KScr.Core.Core;
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

    public override StatementComponent VisitDeclaration(KScrParser.DeclarationContext context) => new()
    {
        Type = StatementComponentType.Declaration,
        CodeType = context.expr() != null ? BytecodeType.Assignment : BytecodeType.Declaration,
        Arg = VisitTypeInfo(context.type()).FullDetailedName + ';' + context.idPart().GetText(),
        SubComponent = context.expr() is {} expr ? VisitExpression(expr) : null
    };

    public override StatementComponent VisitMutation(KScrParser.MutationContext context) => context.binaryop() is { } op
        ? new StatementComponent()
        {
            Type = StatementComponentType.Operator,
            CodeType = BytecodeType.Assignment,
            ByteArg = (ulong)(VisitOperator(op) | Operator.Compound),
            SubComponent = VisitExpression(context.expr())
        }
        : new StatementComponent()
        {
            Type = StatementComponentType.Code,
            CodeType = BytecodeType.Assignment,
            SubComponent = VisitExpression(context.expr())
        };

    public override StatementComponent VisitVarAssign(KScrParser.VarAssignContext context) => new()
    {
        Type = StatementComponentType.Code,
        CodeType = BytecodeType.Assignment,
        SubComponent = VisitExpression(context.left),
        AltComponent = VisitExpression(context.mutation())
    };

    public override StatementComponent VisitOpPrefix(KScrParser.OpPrefixContext context) => new()
    {
        Type = StatementComponentType.Operator,
        CodeType = BytecodeType.Operator,
        ByteArg = (ulong)(new OperatorVisitor().Visit(context.prefixop()) | Operator.UnaryPrefix),
        SubComponent = VisitExpression(context.expr())
    };

    public override StatementComponent VisitOpBinary(KScrParser.OpBinaryContext context) => new()
    {
        Type = StatementComponentType.Operator,
        CodeType = BytecodeType.Operator,
        ByteArg = (ulong)(new OperatorVisitor().Visit(context.binaryop()) | Operator.Binary),
        SubComponent = VisitExpression(context.left),
        AltComponent = VisitExpression(context.right),
    };

    public override StatementComponent VisitOpPostfix(KScrParser.OpPostfixContext context) => new()
    {
        Type = StatementComponentType.Operator,
        CodeType = BytecodeType.Operator,
        ByteArg = (ulong)(new OperatorVisitor().Visit(context.postfixop()) | Operator.UnaryPostfix),
        SubComponent = VisitExpression(context.expr())
    };

    public override StatementComponent VisitParens(KScrParser.ParensContext context) => new()
    {
        Type = StatementComponentType.Expression,
        CodeType = BytecodeType.Parentheses,
        SubComponent = VisitExpression(context.expr())
    };

    public override StatementComponent VisitCtorCall(KScrParser.CtorCallContext context) => new()
    {
        Type = StatementComponentType.Expression,
        CodeType = BytecodeType.ConstructorCall,
        Arg = FindTypeInfo(context.type())!.FullDetailedName,
        SubStatement = VisitArguments(context.arguments())
    };

    public override StatementComponent VisitExprCallMember(KScrParser.ExprCallMemberContext context)
    {
        var expr = VisitExpression(context.expr());
        expr.PostComponent = new()
        {
            Type = StatementComponentType.Expression,
            CodeType = BytecodeType.Call,
            Arg = context.idPart().GetText(),
            SubStatement = VisitArguments(context.arguments())
        };
        return expr;
    }

    public override StatementComponent VisitExprNullFallback(KScrParser.ExprNullFallbackContext context) => new()
    {
        Type = StatementComponentType.Expression,
        CodeType = BytecodeType.NullFallback,
        SubComponent = VisitExpression(context.nullable),
        AltComponent = VisitExpression(context.fallback)
    };

    public override StatementComponent VisitThrowStatement(KScrParser.ThrowStatementContext context) => new()
    {
        Type = StatementComponentType.Code,
        CodeType = BytecodeType.Throw,
        SubComponent = VisitExpression(context.expr())
    };

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

    public override StatementComponent VisitTypeValue(KScrParser.TypeValueContext context) => new()
    {
        Type = StatementComponentType.Expression,
        CodeType = BytecodeType.TypeExpression,
        Arg = VisitTypeInfo(context.type()).FullDetailedName
    };

    public override StatementComponent VisitVarThis(KScrParser.VarThisContext context) => new()
    {
        Type = StatementComponentType.Expression,
        VariableContext = VariableContext.This
    };

    public override StatementComponent VisitVarSuper(KScrParser.VarSuperContext context) => new()
    {
        Type = StatementComponentType.Expression,
        VariableContext = VariableContext.Super
    };

    public override StatementComponent VisitVarLitNum(KScrParser.VarLitNumContext context) => new()
    {
        Type = StatementComponentType.Expression,
        CodeType = BytecodeType.LiteralNumeric,
        Arg = Numeric.Compile(vm, context.GetText()).Value.ToString(IObject.ToString_LongName)
    };

    public override StatementComponent VisitVarLitTrue(KScrParser.VarLitTrueContext context) => new()
    {
        Type = StatementComponentType.Expression,
        CodeType = BytecodeType.LiteralTrue
    };

    public override StatementComponent VisitVarLitFalse(KScrParser.VarLitFalseContext context) => new()
    {
        Type = StatementComponentType.Expression,
        CodeType = BytecodeType.LiteralFalse
    };

    public override StatementComponent VisitVarLitStr(KScrParser.VarLitStrContext context)
    {
        var txt = context.GetText();
        return new()
        {
            Type = StatementComponentType.Expression,
            CodeType = BytecodeType.LiteralString,
            Arg = txt.Substring(txt.IndexOf('"') + 1, txt.LastIndexOf('"') - 1)
        };
    }

    public override StatementComponent VisitVarLitStdio(KScrParser.VarLitStdioContext context) => new()
    {
        Type = StatementComponentType.Expression,
        CodeType = BytecodeType.StdioExpression
    };

    public override StatementComponent VisitVarLitNull(KScrParser.VarLitNullContext context) => new()
    {
        Type = StatementComponentType.Expression,
        CodeType = BytecodeType.Null
    };

    public override StatementComponent VisitIdPart(KScrParser.IdPartContext context) => new()
    {
        Type = StatementComponentType.Provider,
        CodeType = BytecodeType.ExpressionVariable,
        Arg = context.GetText()
    };

    public override StatementComponent VisitRangeInvoc(KScrParser.RangeInvocContext context) => new()
    {
        Type = StatementComponentType.Provider,
        CodeType = BytecodeType.LiteralRange,
        SubComponent = VisitExpression(context.left),
        AltComponent = VisitExpression(context.right),
    };
}

public class OperatorVisitor : KScrParserBaseVisitor<Operator>
{
    public override Operator VisitOpPlus(KScrParser.OpPlusContext context) => Operator.Plus;
    public override Operator VisitOpMinus(KScrParser.OpMinusContext context) => Operator.Minus;
    public override Operator VisitOpMultiply(KScrParser.OpMultiplyContext context) => Operator.Multiply;
    public override Operator VisitOpDivide(KScrParser.OpDivideContext context) => Operator.Divide;
    public override Operator VisitOpModulus(KScrParser.OpModulusContext context) => Operator.Modulus;
    public override Operator VisitOpBitAnd(KScrParser.OpBitAndContext context) => Operator.BitAnd;
    public override Operator VisitOpBitOr(KScrParser.OpBitOrContext context) => Operator.BitOr;
    public override Operator VisitOpLogicAnd(KScrParser.OpLogicAndContext context) => Operator.LogicAnd;
    public override Operator VisitOpLogicOr(KScrParser.OpLogicOrContext context) => Operator.LogicOr;
    public override Operator VisitOpLogicNot(KScrParser.OpLogicNotContext context) => Operator.LogicNot;
    public override Operator VisitOpPow(KScrParser.OpPowContext context) => Operator.Pow;
    public override Operator VisitOpEqual(KScrParser.OpEqualContext context) => Operator.Equals;
    public override Operator VisitOpInequal(KScrParser.OpInequalContext context) => Operator.NotEquals;
    public override Operator VisitOpGreaterEq(KScrParser.OpGreaterEqContext context) => Operator.GreaterEq;
    public override Operator VisitOpLesserEq(KScrParser.OpLesserEqContext context) => Operator.LesserEq;
    public override Operator VisitOpGreater(KScrParser.OpGreaterContext context) => Operator.Greater;
    public override Operator VisitOpLesser(KScrParser.OpLesserContext context) => Operator.Lesser;
    public override Operator VisitOpLShift(KScrParser.OpLShiftContext context) => Operator.LShift;
    public override Operator VisitOpRShift(KScrParser.OpRShiftContext context) => Operator.RShift;
    public override Operator VisitOpULShift(KScrParser.OpULShiftContext context) => Operator.ULShift;
    public override Operator VisitOpURShift(KScrParser.OpURShiftContext context) => Operator.URShift;
    public override Operator VisitOpArithNot(KScrParser.OpArithNotContext context) => Operator.ArithmeticNot;
    public override Operator VisitOpIncrRead(KScrParser.OpIncrReadContext context) => Operator.IncrementRead;
    public override Operator VisitOpDecrRead(KScrParser.OpDecrReadContext context) => Operator.DecrementRead;
    public override Operator VisitOpReadIncr(KScrParser.OpReadIncrContext context) => Operator.ReadIncrement;
    public override Operator VisitOpReadDecr(KScrParser.OpReadDecrContext context) => Operator.ReadDecrement;
}
