using System.Collections.Generic;

namespace KScr.Lib.Model
{
    public enum CodeTokenType : byte
    {
        None,

        // basic symbols
        Terminator,
        Word,
        Return,
        Throw,

        // logistical symbols
        Dot,
        Colon,
        Comma,

        // parentheses
        ParRoundOpen,
        ParRoundClose,
        ParSquareOpen,
        ParSquareClose,
        ParAccOpen,
        ParAccClose,
        ParDiamondOpen,
        ParDiamondClose,

        // identifiers
        IdentNum,
        IdentStr,
        IdentVoid,

        // literals
        LiteralNum,
        LiteralStr,
        LiteralTrue,
        LiteralFalse,
        LiteralNull,

        // operators
        OperatorPlus,
        OperatorMinus,
        OperatorMultiply,
        OperatorDivide,
        OperatorModulus,
        OperatorEquals
    }

    public class CodeToken
    {
        public const byte Version = 1;
        public bool Complete;

        public CodeToken(CodeTokenType type = CodeTokenType.None, string arg = null)
        {
            Type = type;
            Arg = arg;
        }

        public CodeTokenType Type { get; }
        public string? Arg { get; set; }

        public override string ToString()
        {
            return $"Token<{Type}{(Arg != null ? ',' + Arg : string.Empty)}>";
        }
    }

    public interface ICodeTokenizer
    {
        IList<CodeToken> Tokenize(string source);
    }
}