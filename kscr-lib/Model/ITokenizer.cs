using System;
using System.Collections.Generic;

namespace KScr.Lib.Model
{
    [Flags]
    public enum TokenType : uint
    {
        Whitespace = 0x0000_00,

        Terminator = 0x0101_01,
        Word = 0x0101_10,
        Return = 0x0101_20,
        Throw = 0x0101_21,
        This = 0x0101_22,

        Dot = 0x0102_10,
        Colon = 0x0102_20,
        Comma = 0x0102_40,

        ParRoundOpen = 0x0104_10,
        ParRoundClose = 0x0104_11,
        ParSquareOpen = 0x0104_20,
        ParSquareClose = 0x0104_21,
        ParAccOpen = 0x0104_40,
        ParAccClose = 0x0104_41,
        ParDiamondOpen = 0x0104_80,
        ParDiamondClose = 0x0104_81,

        IdentVoid = 0x0108_01,
        IdentNum = 0x0108_10,
        IdentStr = 0x0108_20,

        LiteralNull = 0x010F_01,
        LiteralNum = 0x010F_10,
        LiteralTrue = 0x010F_11,
        LiteralFalse = 0x010F_12,
        LiteralStr = 0x010F_20,

        OperatorPlus = 0x0110_11,
        OperatorMinus = 0x0110_12,
        OperatorMultiply = 0x0110_14,
        OperatorDivide = 0x0110_18,
        OperatorModulus = 0x0110_1F,
        OperatorEquals = 0x0110_20,


        // inheritance
        Extends = 0x0201_10,
        Implements = 0x0201_20,

        // accessibility keywords
        Public = 0x0202_11,
        Internal = 0x0202_12,
        Protected = 0x0202_14,
        Private = 0x0202_18,

        // class models
        Class = 0x0204_10,
        Interface = 0x0204_20,
        Enum = 0x0204_40,

        // static
        Static = 0x0208_10,
        Dynamic = 0x0208_20,

        // other modifiers
        Abstract = 0x020F_10,
        Final = 0x020F_20
    }

    public static class TokenExtensios
    {
        public static string String(this IToken token) => token.Type switch
        {
            TokenType.Whitespace => " ",
            TokenType.Terminator => ";",
            TokenType.Word => token.Arg!,
            TokenType.Return => "return",
            TokenType.Throw => "throw",
            TokenType.Dot => ".",
            TokenType.Colon => "'",
            TokenType.Comma => ",",
            TokenType.ParRoundOpen => "(",
            TokenType.ParRoundClose => ")",
            TokenType.ParSquareOpen => "[",
            TokenType.ParSquareClose => "]",
            TokenType.ParAccOpen => "{",
            TokenType.ParAccClose => "}",
            TokenType.ParDiamondOpen => "<",
            TokenType.ParDiamondClose => ">",
            TokenType.IdentVoid => "void",
            TokenType.IdentNum => "num",
            TokenType.IdentStr => "str",
            TokenType.LiteralNull => "null",
            TokenType.LiteralNum => "num",
            TokenType.LiteralTrue => "true",
            TokenType.LiteralFalse => "false",
            TokenType.LiteralStr => "str",
            TokenType.OperatorPlus => "+",
            TokenType.OperatorMinus => "-",
            TokenType.OperatorMultiply => "*",
            TokenType.OperatorDivide => "/",
            TokenType.OperatorModulus => "%",
            TokenType.OperatorEquals => "=",
            TokenType.Extends => "extends",
            TokenType.Implements => "implements",
            TokenType.Public => "public",
            TokenType.Internal => "internal",
            TokenType.Protected => "protected",
            TokenType.Private => "private",
            TokenType.Class => "class",
            TokenType.Interface => "interface",
            TokenType.Enum => "enum",
            TokenType.Static => "static",
            TokenType.Dynamic => "dynamic",
            TokenType.Abstract => "abstract",
            TokenType.Final => "final",
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public interface ITokenizer
    {
        bool PushToken();
        bool PushToken(IToken? token);
        bool PushToken(ref IToken? token);
        IList<IToken> Tokenize(RuntimeBase vm, string source);
        IToken? Accept(char c, char n, char p, ref int i, ref string str);
    }

    public interface IToken
    {
        TokenType Type { get; }
        string? Arg { get; }
    }

    public abstract class AbstractToken : IToken
    {
        public bool Complete;

        public AbstractToken(TokenType type = TokenType.Whitespace, string arg = null!)
        {
            Type = type;
            Arg = arg;
        }

        public TokenType Type { get; set; }
        public string? Arg { get; set; }

        public override string ToString()
        {
            return $"Token<{Type}{(Arg != null ? ',' + Arg : string.Empty)}>";
        }
    }

    public sealed class ClassToken : AbstractToken
    {
        public ClassToken(TokenType type = TokenType.Whitespace, string arg = null!) : base(type, arg)
        {
        }
    }

    public sealed class Token : AbstractToken
    {
        public Token(TokenType type = TokenType.Whitespace, string arg = null!) : base(type, arg)
        {
        }
    }
}