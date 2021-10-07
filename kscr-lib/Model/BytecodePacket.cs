using System;
using System.Collections.Generic;
using KScr.Lib.Core;
using KScr.Lib.Exception;
using KScr.Lib.Store;
using String = KScr.Lib.Core.String;

namespace KScr.Lib.Model
{
    [Flags]
    public enum BytecodeType : uint
    {
        Terminator = 0xFFFF_FFFF,

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
        ExpressionVariable = 0x0000_0F00 | Expression,

        OperatorPlus = 0x0000_1000 | Operator,
        OperatorMinus = 0x0000_2000 | Operator,
        OperatorMultiply = 0x0000_4000 | Operator,
        OperatorDivide = 0x0000_8000 | Operator,
        OperatorModulus = 0x0000_F000 | Operator,


        Throw = 0x4000_0000 | Statement,
        Return = 0x8000_0000 | Statement,
        Null = 0xF000_0000 | Expression
    }
    
        public override string ToString()
        {
            return $"BytecodePacket<{Type}{(Arg != null ? "," + Arg : string.Empty)}>";
        }
    }

}