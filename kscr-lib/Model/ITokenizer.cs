using System;
using System.Collections.Generic;
using KScr.Lib.Bytecode;

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
        StdIo = 0x0101_30,

        Dot = 0x0102_10,
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
        IdentNumByte = 0x0108_20,
        IdentNumShort = 0x0108_21,
        IdentNumInt = 0x0108_22,
        IdentNumLong = 0x0108_24,
        IdentNumFloat = 0x0108_28,
        IdentNumDouble = 0x0108_2F,
        IdentStr = 0x0108_80,
        IdentVar = 0x0108_FF,

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
        Circumflex = 0x0111_11,
        Ampersand = 0x0111_21,
        VertBar = 0x0111_22,
        Exclamation = 0x0111_41,
        Question = 0x0111_42,
        Colon = 0x0111_80,
        Tilde = 0x0111_F0,

        // statements
        If = 0x0120_10,
        Else = 0x0120_11,
        Try = 0x0120_11,
        Catch = 0x0120_11,
        Finally = 0x0120_11,
        Do = 0x0120_80,
        While = 0x0120_81,
        For = 0x0120_82,
        ForN = 0x0120_84,
        ForEach = 0x0120_88,
        Switch = 0x0120_F4,
        Case = 0x0120_F0,
        Break = 0x0120_F1,
        Default = 0x0120_F2,
        Continue = 0x0120_F4,

        // inheritance
        Super = 0x0201_0F,
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
        Annotation = 0x0204_80,

        // static
        Static = 0x0208_10,
        Dynamic = 0x0208_20,

        // other modifiers
        Abstract = 0x020F_10,
        Final = 0x020F_20,

        // class initializers
        Package = 0x0401_10,
        Import = 0x0402_10
    }

    public static class TokenExtensios
    {
        public static string String(this IToken token)
        {
            return token.Type switch
            {
                TokenType.Whitespace => " ",
                TokenType.Terminator => ";",
                TokenType.Word => token.Arg!,
                TokenType.Return => "return",
                TokenType.Throw => "throw",
                TokenType.This => "this",
                TokenType.StdIo => "stdio",
                TokenType.Dot => ".",
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
                TokenType.IdentNumByte => "byte",
                TokenType.IdentNumShort => "short",
                TokenType.IdentNumInt => "int",
                TokenType.IdentNumLong => "long",
                TokenType.IdentNumFloat => "float",
                TokenType.IdentNumDouble => "double",
                TokenType.IdentStr => "str",
                TokenType.IdentVar => "var",
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
                TokenType.Circumflex => "^",
                TokenType.Ampersand => "&",
                TokenType.VertBar => "|",
                TokenType.Exclamation => "!",
                TokenType.Question => "?",
                TokenType.Colon => ":",
                TokenType.Tilde => "~",
                TokenType.If => "if",
                TokenType.Else => "else",
                TokenType.Do => "do",
                TokenType.While => "while",
                TokenType.For => "for",
                TokenType.ForN => "forn",
                TokenType.ForEach => "foreach",
                TokenType.Super => "super",
                TokenType.Extends => "extends",
                TokenType.Implements => "implements",
                TokenType.Public => "public",
                TokenType.Internal => "internal",
                TokenType.Protected => "protected",
                TokenType.Private => "private",
                TokenType.Class => "class",
                TokenType.Interface => "interface",
                TokenType.Enum => "enum",
                TokenType.Annotation => "annotation",
                TokenType.Static => "static",
                TokenType.Dynamic => "dynamic",
                TokenType.Abstract => "abstract",
                TokenType.Final => "final",
                TokenType.Package => "package",
                TokenType.Import => "import",
                _ => throw new ArgumentOutOfRangeException(token.ToString())
            };
        }

        public static MemberModifier? Modifier(this TokenType type)
        {
            return type switch
            {
                TokenType.Public => MemberModifier.Public,
                TokenType.Protected => MemberModifier.Protected,
                TokenType.Internal => MemberModifier.Internal,
                TokenType.Private => MemberModifier.Private,
                TokenType.Static => MemberModifier.Static,
                TokenType.Abstract => MemberModifier.Abstract,
                TokenType.Final => MemberModifier.Final,
                _ => null
            };
        }

        public static ClassType? ClassType(this TokenType type)
        {
            return type switch
            {
                TokenType.Class => Model.ClassType.Class,
                TokenType.Interface => Model.ClassType.Interface,
                TokenType.Enum => Model.ClassType.Enum,
                TokenType.Annotation => Model.ClassType.Annotation,
                _ => null
            };
        }
    }

    public interface ITokenizer
    {
        bool PushToken();
        bool PushToken(IToken? token);
        bool PushToken(ref IToken? token);
        IList<IToken> Tokenize(RuntimeBase vm, string source);
        void Accept(char c, char n, char p, ref int i, ref string str);
    }

    public interface IToken
    {
        TokenType Type { get; set; }
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