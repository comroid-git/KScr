using System;
using System.Collections.Generic;
using System.IO;
using KScr.Lib.Bytecode;
using KScr.Lib.Store;
using static KScr.Lib.Model.TokenType;

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
        IdentObject = 0x0108_02,
        IdentType = 0x0108_04,
        IdentArray = 0x0108_08,
        IdentTuple = 0x0108_0F,
        IdentNum = 0x0108_10,
        IdentNumByte = 0x0108_20,
        IdentNumShort = 0x0108_21,
        IdentNumInt = 0x0108_22,
        IdentNumLong = 0x0108_24,
        IdentNumFloat = 0x0108_28,
        IdentNumDouble = 0x0108_2F,
        IdentRange = 0x0108_41,
        IdentPipe = 0x0108_42,
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
        Try = 0x0120_20,
        Catch = 0x0120_21,
        Finally = 0x0120_22,
        Do = 0x0120_80,
        While = 0x0120_81,
        For = 0x0120_82,
        ForEach = 0x0120_88,
        Switch = 0x0120_F4,
        Case = 0x0120_F0,
        Break = 0x0120_F1,
        Default = 0x0120_F2,
        Continue = 0x0120_F8,
        New = 0x0120_4F,

        // inheritance
        Super = 0x0201_0F,
        Extends = 0x0201_10,
        Implements = 0x0201_20,

        // accessibility keywords
        Public = 0b1000000010_00000001,
        Internal = 0b1000000010_00000010,
        Protected = 0b1000000010_00000100,
        Private = 0b1000000010_00001000,

        // class models
        Class = 0b1000000100_00010000,
        Interface = 0b1000000100_00100000,
        Enum = 0b1000000100_01000000,
        Annotation = 0b1000000100_10000000,

        // static
        Static = 0x0208_10,
        Dynamic = 0x0208_20,

        // other modifiers
        Abstract = 0x020F_10,
        Final = 0x020F_20,
        Native = 0x020F_40,

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
                Whitespace => " ",
                Terminator => ";",
                Word => token.Arg!,
                Return => "return",
                Throw => "throw",
                This => "this",
                StdIo => "stdio",
                Dot => ".",
                Comma => ",",
                ParRoundOpen => "(",
                ParRoundClose => ")",
                ParSquareOpen => "[",
                ParSquareClose => "]",
                ParAccOpen => "{",
                ParAccClose => "}",
                ParDiamondOpen => "<",
                ParDiamondClose => ">",
                IdentVoid => "void",
                IdentNum => "num",
                IdentNumByte => "byte",
                IdentNumShort => "short",
                IdentNumInt => "int",
                IdentNumLong => "long",
                IdentNumFloat => "float",
                IdentNumDouble => "double",
                IdentStr => "str",
                IdentVar => "var",
                LiteralNull => "null",
                LiteralNum => "num",
                LiteralTrue => "true",
                LiteralFalse => "false",
                LiteralStr => "str",
                OperatorPlus => "+",
                OperatorMinus => "-",
                OperatorMultiply => "*",
                OperatorDivide => "/",
                OperatorModulus => "%",
                OperatorEquals => "=",
                Circumflex => "^",
                Ampersand => "&",
                VertBar => "|",
                Exclamation => "!",
                Question => "?",
                Colon => ":",
                Tilde => "~",
                If => "if",
                Else => "else",
                Try => "try",
                Catch => "catch",
                Finally => "finally",
                Do => "do",
                While => "while",
                For => "for",
                ForEach => "foreach",
                Switch => "foreach",
                Case => "foreach",
                Default => "foreach",
                Break => "foreach",
                Continue => "foreach",
                New => "foreach",
                Super => "super",
                Extends => "extends",
                Implements => "implements",
                /*
                    Public => "public",
                    Internal => "internal",
                    Protected => "protected",
                    Private => "private",
                    TokenType.Class => "class",
                    Interface => "interface",
                    TokenType.Enum => "enum",
                    Annotation => "annotation",
                    Static => "static",
                    Dynamic => "dynamic",
                    Abstract => "abstract",
                    Final => "final",
                    Native => "native",
                */
                TokenType.Package => "package",
                Import => "import",
                IdentObject => "object",
                IdentType => "type",
                IdentArray => "array",
                IdentTuple => "tuple",
                IdentRange => "range",
                IdentPipe => "pipe",
                _ => String(token.Type)
            };
        }

        private static string String(TokenType type)
        {
            string txt = "";
            if ((type & Public) == Public)
                txt += " public";
            if ((type & Internal) == Internal)
                txt += " internal";
            if ((type & Protected) == Protected)
                txt += " protected";
            if ((type & Private) == Private)
                txt += " private";
            if ((type & Static) == Static)
                txt += " static";
            if ((type & Dynamic) == Dynamic)
                txt += " dynamic";
            if ((type & Abstract) == Abstract)
                txt += " abstract";
            if ((type & Final) == Final)
                txt += " final";
            if ((type & Native) == Native)
                txt += " native";
            if ((type & TokenType.Class) == TokenType.Class)
                txt += " class";
            if ((type & Interface) == Interface)
                txt += " interface";
            if ((type & TokenType.Enum) == TokenType.Enum)
                txt += " enum";
            if ((type & Annotation) == Annotation)
                txt += " annotation";
            return txt.Substring(1);
        }

        public static MemberModifier? Modifier(this TokenType type)
        {
            var mod = MemberModifier.None;
            if ((type & Public) == Public)
                mod |= MemberModifier.Public;
            if ((type & Protected) == Protected)
                mod |= MemberModifier.Protected;
            if ((type & Internal) == Internal)
                mod |= MemberModifier.Internal;
            if ((type & Private) == Private)
                mod |= MemberModifier.Private;
            if ((type & Static) == Static)
                mod |= MemberModifier.Static;
            if ((type & Abstract) == Abstract)
                mod |= MemberModifier.Abstract;
            if ((type & Final) == Final)
                mod |= MemberModifier.Final;
            if ((type & Native) == Native)
                mod |= MemberModifier.Native;
            return mod == 0 ? null : mod;
        }

        public static ClassType? ClassType(this TokenType type)
        {
            if ((type & TokenType.Class) == TokenType.Class)
                return Model.ClassType.Class;
            if ((type & Interface) == Interface)
                return Model.ClassType.Interface;
            if ((type & TokenType.Enum) == TokenType.Enum)
                return Model.ClassType.Enum;
            if ((type & Annotation) == Annotation)
                return Model.ClassType.Annotation;
            return null;
        }
    }

    public interface ITokenizer
    {
        bool PushToken();
        bool PushToken(Token? token);
        bool PushToken(ref Token? token);
        IList<IToken> Tokenize(string sourcefilePath, string source);
        void Accept(char c, char n, char p, ref int i, ref string str);
    }

    public struct SourcefilePosition : IBytecode
    {
        public string SourcefilePath;
        public int SourcefileLine;
        public int SourcefileCursor;

        public void Write(Stream stream)
        {
            stream.Write(BitConverter.GetBytes(SourcefileLine));
            stream.Write(BitConverter.GetBytes(SourcefileCursor));
            /*var buf = RuntimeBase.Encoding.GetBytes(SourcefilePath);
            stream.Write(BitConverter.GetBytes(buf.Length));
            stream.Write(buf);*/
        }

        public void Load(RuntimeBase vm, byte[] data, ref int index)
        {
            SourcefileLine = BitConverter.ToInt32(data, index);
            index += 4;
            SourcefileCursor = BitConverter.ToInt32(data, index);
            index += 4;
            /*int len = BitConverter.ToInt32(data, index);
            index += 4;
            SourcefilePath = RuntimeBase.Encoding.GetString(data, index, len);*/
        }

        public static SourcefilePosition Read(RuntimeBase vm, byte[] data, ref int i)
        {
            var srcPos = new SourcefilePosition();
            srcPos.Load(vm, data, ref i);
            return srcPos;
        }
    }

    public interface IToken
    {
        TokenType Type { get; set; }
        string? Arg { get; }
        SourcefilePosition SourcefilePosition { get; }
    }

    public abstract class AbstractToken : IToken
    {
        public AbstractToken(SourcefilePosition sourcefilePosition, TokenType type = Whitespace,
            string arg = null!)
        {
            SourcefilePosition = sourcefilePosition;
            Type = type;
            Arg = arg;
        }

        public TokenType Type { get; set; }
        public string? Arg { get; set; }
        public SourcefilePosition SourcefilePosition { get; }

        public override string ToString()
        {
            return $"Token<{Type}{(Arg != null ? ',' + Arg : string.Empty)}>";
        }
    }

    public sealed class Token : AbstractToken
    {
        public Token(SourcefilePosition pos, TokenType type = Whitespace, string arg = null!) : base(pos,
            type, arg)
        {
        }
    }
}