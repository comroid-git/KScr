namespace KScr.Lib.Model
{
    public enum TokenType : byte
    {
        None,

        // basic symbols
        Terminator,
        Var,
        Return,
        Throw,

        // parentheses
        ParRoundOpen,
        ParRoundClose,
        ParSquareOpen,
        ParSquareClose,
        ParAccOpen,
        ParAccClose,

        // identifiers
        IdentVar,
        IdentNum,
        IdentStr,
        IdentByte,
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

    public class Token
    {
        public const byte Version = 1;
        public bool Complete;

        public Token(TokenType type = TokenType.None, string arg = null)
        {
            Type = type;
            Arg = arg;
        }

        public TokenType Type { get; }
        public string? Arg { get; set; }

        public override string ToString()
        {
            return $"Token<{Type}{(Arg != null ? ',' + Arg : string.Empty)}>";
        }
    }
}