using System.Collections.Generic;
using KScr.Lib.Core;
using KScr.Lib.Model;

namespace KScr.Compiler.Code
{
    public sealed class CodeTokenizer : AbstractTokenizer
    {
        private int artParLevel;
        private bool isLineComment;
        private bool isStringLiteral;
        private readonly List<IToken> tokens = new List<IToken>();

        private CodeToken token
        {
            get => (Token as CodeToken)!;
            set => Token = value;
        }

        public override bool PushToken(ref IToken? token)
        {
            return base.PushToken(ref token) && (Token = new CodeToken()) != null;
        }

        public override IToken? Accept(char c, char n, char p, ref int i, ref string str)
        {
            // string literals
            if (isStringLiteral)
            {
                if ((c != '"') & (p != '\\'))
                    token.Arg += c;
                else if (c == '"' && p != '\\')
                    token.Complete = true;
            }

            // skip linefeeds
            if (c == '\n' || c == '\r')
            {
                if (isLineComment)
                    isLineComment = false;
                return null;
            }

            if (c == '/' && n == '/')
                isLineComment = true;

            if (isLineComment)
                return null;

            bool isWhitespace = c == ' ';

            if (isWhitespace && !isStringLiteral)
                token.Complete = true;

            IToken? buf;
            switch (c)
            {
                // terminator
                case ';':
                    while (artParLevel-- > 0)
                    {
                        buf = new CodeToken(TokenType.ParRoundClose) { Complete = true };
                        PushToken(ref buf);
                    }

                    return new CodeToken(TokenType.Terminator) { Complete = true };
                // logistical symbols
                case '.':
                    if (str.Length == 0 || !char.IsDigit(str[^1]))
                        token = new CodeToken(TokenType.Dot) { Complete = true };
                    else
                        LexicalToken(isWhitespace, ref str, c, n, ref i);
                    break;
                case ':':
                    token = new CodeToken(TokenType.Colon) { Complete = true };
                    break;
                case ',':
                    token = new CodeToken(TokenType.Comma) { Complete = true };
                    break;
                // arithmetic operators
                case '+':
                    token = new CodeToken(TokenType.OperatorPlus) { Complete = true };
                    break;
                case '-':
                    token = new CodeToken(TokenType.OperatorMinus) { Complete = true };
                    break;
                case '*':
                    token = new CodeToken(TokenType.OperatorMultiply) { Complete = true };
                    break;
                case '/':
                    token = new CodeToken(TokenType.OperatorDivide) { Complete = true };
                    break;
                case '%':
                    token = new CodeToken(TokenType.OperatorModulus) { Complete = true };
                    break;
                // parentheses
                case '(':
                    token = new CodeToken(TokenType.ParRoundOpen) { Complete = true };
                    break;
                case ')':
                    token = new CodeToken(TokenType.ParRoundClose) { Complete = true };
                    break;
                case '[':
                    token = new CodeToken(TokenType.ParSquareOpen) { Complete = true };
                    break;
                case ']':
                    token = new CodeToken(TokenType.ParSquareClose) { Complete = true };
                    break;
                case '{':
                    token = new CodeToken(TokenType.ParAccOpen) { Complete = true };
                    break;
                case '}':
                    token = new CodeToken(TokenType.ParAccClose) { Complete = true };
                    break;
                case '<':
                    token = new CodeToken(TokenType.ParDiamondOpen) { Complete = true };
                    break;
                case '>':
                    token = new CodeToken(TokenType.ParDiamondClose) { Complete = true };
                    break;
                case '"':
                    // ReSharper disable once AssignmentInConditionalExpression
                    if (isStringLiteral = !isStringLiteral)
                        token = new CodeToken(TokenType.LiteralStr, "");
                    break;
                // equals operand
                case '=':
                    buf = new CodeToken(TokenType.OperatorEquals) { Complete = true };
                    PushToken(ref buf);
                    // create artificial parentheses if this EQUALS operand is of an assignment
                    if (n != '=' && tokens[^2].Type == TokenType.Word)
                    {
                        buf = new CodeToken(TokenType.ParRoundOpen) { Complete = true };
                        PushToken(ref buf);
                        artParLevel++;
                    }

                    return null;
                // lexical tokens
                default:
                    LexicalToken(isWhitespace, ref str, c, n, ref i);
                    break;
            }

            return null;
        }

        private void LexicalToken(bool isWhitespace, ref string str, char c, char n, ref int i)
        {
            if (!isWhitespace)
                str += c;
            if (isStringLiteral)
                return;

            switch (str)
            {
                case "return":
                    PushToken(new CodeToken(TokenType.Return) { Complete = true });
                    PushToken(new CodeToken(TokenType.ParRoundOpen) { Complete = true });
                    artParLevel++;
                    return;
                case "throw":
                    PushToken(new CodeToken(TokenType.Throw) { Complete = true });
                    PushToken(new CodeToken(TokenType.ParRoundOpen) { Complete = true });
                    artParLevel++;
                    return;
                case "num":
                    token = new CodeToken(TokenType.IdentNum) { Complete = true };
                    break;
                case "str":
                    token = new CodeToken(TokenType.IdentStr) { Complete = true };
                    break;
                case "void":
                    token = new CodeToken(TokenType.IdentVoid) { Complete = true };
                    break;
                case "true":
                    token = new CodeToken(TokenType.LiteralTrue) { Complete = true };
                    break;
                case "false":
                    token = new CodeToken(TokenType.LiteralFalse) { Complete = true };
                    break;
                case "null":
                    token = new CodeToken(TokenType.LiteralNull) { Complete = true };
                    break;
                default:
                    if (Numeric.NumberRegex.IsMatch(str) && !char.IsDigit(n) && n != '.')
                    {
                        if (n == 'b' || n == 'i' || n == 'l' || n == 'f' || n == 'd')
                        {
                            str += n;
                            i++;
                        }

                        token = new CodeToken(TokenType.LiteralNum, str) { Complete = true };
                    }
                    else if (str.Length >= 2 && str[0] == '"' && str[^1] == '"')
                    {
                        token = new CodeToken(TokenType.LiteralStr, str.Substring(1, str.Length - 2))
                            { Complete = true };
                    }
                    else if (!char.IsLetterOrDigit(n) && str != string.Empty && !char.IsDigit(str[^1]))
                    {
                        token = new CodeToken(TokenType.Word, str) { Complete = true };
                    }

                    break;
            }
        }
    }
}