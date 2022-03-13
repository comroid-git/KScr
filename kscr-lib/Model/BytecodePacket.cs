using System;

namespace KScr.Lib.Model
{
    [Flags]
    public enum BytecodeType : uint
    {
        Undefined = 0xFFFF_FFFF,

        Declaration = 0x0000_0001,
        Assignment = 0x0000_0002,
        Expression = 0x0000_0004,
        Statement = 0x0000_0008,
        Operator = 0x0000_000F | Expression,
        Parentheses = 0x0000_0010 | Expression,

        DeclarationNumeric = 0x0000_0020 | Declaration,
        DeclarationString = 0x0000_0040 | Declaration,
        DeclarationByte = 0x0000_0080 | Declaration,
        DeclarationVariable = 0x0000_00F0 | Declaration,

        LiteralNumeric = 0x0000_0100 | Expression,
        LiteralString = 0x0000_0200 | Expression,
        LiteralTrue = 0x0000_0400 | Expression,
        LiteralFalse = 0x0000_0800 | Expression,
        LiteralRange = 0x0F00_0000 | Expression,
        ExpressionVariable = 0x0000_0F00 | Expression,

        OperatorPlus = 0x0000_1000 | Operator,
        OperatorMinus = 0x0000_2000 | Operator,
        OperatorMultiply = 0x0000_4000 | Operator,
        OperatorDivide = 0x0000_8000 | Operator,
        OperatorModulus = 0x0000_F000 | Operator,

        StmtIf = 0x0011_0000 | Statement,
        StmtCond = 0x0012_0000 | Statement,
        StmtElse = 0x0014_0000 | Statement,
        StmtDo = 0x0021_0000 | Statement,
        StmtWhile = 0x0022_0000 | Statement,
        StmtFor = 0x0041_0000 | Statement,
        StmtForEach = 0x00422_0000 | Statement,

        StdioExpression = 0x0200_0000,
        ParameterExpression = 0x0100_0000,
        TypeExpression = 0x0400_0000,
        ConstructorCall = 0x0800_0000,

        Call = 0x2000_0000 | Expression,
        Throw = 0x4000_0000 | Statement,
        Return = 0x8000_0000 | Statement,
        Null = 0xF000_0000 | Expression
    }

    public enum Operator : ushort
    {
        Unknown = 0,

        // unary operators
        IncrementRead, // ++x
        ReadIncrement, // x++
        DecrementRead, // --x
        ReadDecrement, // x--
        LogicalNot, // !x
        ArithmeticNot, // -x

        // binary operators
        Plus, // +
        Minus, // -
        Multiply, // *
        Divide, // /
        Modulus, // %
        Circumflex, // ^
        Equals, // ==
        NotEquals, // !=
        Greater, // >
        GreaterEq, // >=
        Lesser, // <
        LesserEq // <=
    }
}