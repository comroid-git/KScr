using System;
using System.Collections.Generic;
using KScr.Lib.Bytecode;
using KScr.Lib.Store;

namespace KScr.Lib.Model
{
    [Flags]
    public enum ClassTokenType : uint
    {
        None = 0x0,
        Word = 0x0000_F000,

        // logistical symbols
        Dot = 0x0000_0010,
        Colon = 0x0000_0020,
        Comma = 0x0000_0040,
        Equals = 0x0000_0080,

        // identifiers
        IdentNum = 0x0000_0100,
        IdentStr = 0x0000_0200,
        IdentVoid = 0x0000_0400,
        
        // inheritance
        Extends = 0x0000_0800,
        Implements = 0x0000_0F00,

        // accessibility keywords
        Public = 0x0000_1000,
        Internal = 0x0000_2000,
        Protected = 0x0000_4000,
        Private = 0x0000_8000,

        // class models
        Class = 0x0001_0000,
        Interface = 0x0002_0000,
        Enum = 0x0004_0000,

        // static
        Static = 0x0010_0000,
        Dynamic = 0x0020_0000,

        // other modifiers
        Abstract = 0x0040_0000,
        Final = 0x0080_0000,

        // parentheses
        ParRoundOpen = 0x0100_0000,
        ParRoundClose = 0x1000_0000,
        ParSquareOpen = 0x0200_0000,
        ParSquareClose = 0x2000_0000,
        ParAccOpen = 0x0400_0000,
        ParAccClose = 0x4000_0000,
        ParDiamondOpen = 0x0800_0000,
        ParDiamondClose = 0x0F00_0000
    }

    public sealed class ClassToken
    {
        public ClassTokenType Type { get; set; }
        public string Arg { get; set; }
        public bool Complete { get; set; }
    }

    public interface IClassTokenizer
    {
        List<ClassToken> Tokenize(string source);
    }
}