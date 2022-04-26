using System;

namespace KScr.Core.Model;

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

    NullFallback = 0x0000_1000 | Expression,
    StmtMark = 0x0000_2000 | Statement,
    StmtJump = 0x0000_4000 | Statement,

    StmtIf = 0x0011_0000 | Statement,
    StmtCond = 0x0012_0000 | Statement,
    StmtElse = 0x0014_0000 | Statement,
    StmtDo = 0x0021_0000 | Statement,
    StmtWhile = 0x0022_0000 | Statement,
    StmtTry = 0x0024_0000 | Statement,
    StmtCatch = 0x0028_0000 | Statement,
    StmtFinally = 0x0018_0000 | Statement,
    StmtFor = 0x0041_0000 | Statement,
    StmtForEach = 0x0042_0000 | Statement,
    StmtSwitch = 0x0044_0000 | Statement,
    StmtCase = 0x0048_0000 | Statement,
    
    Cast = 0x0080_0000 | Expression,
    Instanceof = 0x0081_0000 | Expression,
    Indexer = 0x0082_0000,
    TupularExpression = 0x0084_0000 | Expression,
    ArrayConstructor = 0x0088_0000 | Expression,

    ParameterExpression = 0x0100_0000,
    StdioExpression = 0x0200_0000,
    EndlExpression = 0x1200_0000,
    TypeExpression = 0x0400_0000,
    ConstructorCall = 0x0800_0000,

    Call = 0x2000_0000 | Expression,
    Throw = 0x4000_0000 | Statement,
    Return = 0x8000_0000 | Statement,
    Null = 0xF000_0000 | Expression
}

[Flags]
public enum Operator
{
    Unknown = 0,

    // unary operators
    IncrementRead = 1 << 1, // ++x
    DecrementRead = 1 << 2, // --x
    ReadIncrement = 1 << 3, // x++
    ReadDecrement = 1 << 4, // x--
    LogicNot = 1 << 5, // !x
    ArithmeticNot = 1 << 6, // -x

    // binary operators
    Plus = 1 << 7, // +
    Minus = 1 << 8, // -
    Multiply = 1 << 9, // *
    Divide = 1 << 10, // /
    Modulus = 1 << 11, // %
    Pow = 1 << 12, // ^
    Equals = 1 << 13, // ==
    NotEquals = 1 << 14, // !=
    Greater = 1 << 15, // >
    GreaterEq = 1 << 16, // >=
    Lesser = 1 << 17, // <
    LesserEq = 1 << 18, // <=

    BitAnd = 1 << 19, // &
    BitOr = 1 << 20, // |
    LogicAnd = 1 << 21, // &&
    LogicOr = 1 << 22, // ||

    LShift = 1 << 23, // <<
    RShift = 1 << 24, // >>
    ULShift = 1 << 25, // <<<
    URShift = 1 << 26, // >>>

    NullFallback = 1 << 27, // ??

    // flags
    UnaryPrefix = 1 << 28,
    UnaryPostfix = 1 << 29,
    Binary = 1 << 30,
    Compound = 1 << 31
}