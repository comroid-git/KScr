using System.Collections.Generic;
using System.Linq.Expressions;
using KScr.Lib.Core;
using KScr.Lib.Model;

namespace KScr.Eval
{
    public sealed class Tokenizer : ITokenizer
    {
        public IList<Token> Tokenize(string source)
        {
            Token token = new Token();
            List<Token> tokens = new List<Token>();
            string str = "";
            var len = source.Length;
            var isLineComment = false;
            var isStringLiteral = false;
            var artParLevel = 0;

            for (var i = 0; i < len; i++)
            {
                var c = source[i];
                var n = i + 1 < len ? source[i + 1] : ' ';
                var p = i - 1 > 0 ? source[i - 1] : ' ';

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
                    continue;
                }

                if (c == '/' && n == '/')
                    isLineComment = true;

                if (isLineComment)
                    continue;

                var isWhitespace = c == ' ';

                if (isWhitespace && !isStringLiteral)
                    token.Complete = true;

                switch (c)
                {
                    // terminator
                    case ';':
                        while (artParLevel-- > 0)
                            PushToken(tokens, new Token(TokenType.ParRoundClose) { Complete = true }, out str);
                        PushToken(tokens, new Token(TokenType.Terminator) { Complete = true }, out str);
                        continue;
                    // logistical symbols
                    case '.':
                        if (str.Length > 0 && !char.IsDigit(str[^1]))
                            token = new Token(TokenType.Dot) { Complete = true };
                        else LexicalToken(isWhitespace, ref str, c, isStringLiteral, tokens, artParLevel, n, ref token, ref i);
                        break;
                    case ':':
                        token = new Token(TokenType.Colon) { Complete = true };
                        break;
                    case ',':
                        token = new Token(TokenType.Comma) { Complete = true };
                        break;
                    // arithmetic operators
                    case '+':
                        token = new Token(TokenType.OperatorPlus) { Complete = true };
                        break;
                    case '-':
                        token = new Token(TokenType.OperatorMinus) { Complete = true };
                        break;
                    case '*':
                        token = new Token(TokenType.OperatorMultiply) { Complete = true };
                        break;
                    case '/':
                        token = new Token(TokenType.OperatorDivide) { Complete = true };
                        break;
                    case '%':
                        token = new Token(TokenType.OperatorModulus) { Complete = true };
                        break;
                    // parentheses
                    case '(':
                        token = new Token(TokenType.ParRoundOpen) { Complete = true };
                        break;
                    case ')':
                        token = new Token(TokenType.ParRoundClose) { Complete = true };
                        break;
                    case '[':
                        token = new Token(TokenType.ParSquareOpen) { Complete = true };
                        break;
                    case ']':
                        token = new Token(TokenType.ParSquareClose) { Complete = true };
                        break;
                    case '{':
                        token = new Token(TokenType.ParAccOpen) { Complete = true };
                        break;
                    case '}':
                        token = new Token(TokenType.ParAccClose) { Complete = true };
                        break;
                    case '<':
                        token = new Token(TokenType.ParDiamondOpen) { Complete = true };
                        break;
                    case '>':
                        token = new Token(TokenType.ParDiamondClose) { Complete = true };
                        break;
                    case '"':
                        // ReSharper disable once AssignmentInConditionalExpression
                        if (isStringLiteral = !isStringLiteral)
                            token = new Token(TokenType.LiteralStr, "");
                        break;
                    // equals operand
                    case '=':
                        PushToken(tokens, new Token(TokenType.OperatorEquals) { Complete = true }, out str);
                        // create artificial parentheses if this EQUALS operand is of an assignment
                        if (n != '=' && tokens[^2].Type == TokenType.Word)
                        {
                            PushToken(tokens, new Token(TokenType.ParRoundOpen) { Complete = true }, out str);
                            artParLevel++;
                        }

                        continue;
                    // lexical tokens
                    default:
                        LexicalToken(isWhitespace, ref str, c, isStringLiteral, tokens, artParLevel, n, ref token, ref i);
                        break;
                }

                if (token.Complete && token.Type != TokenType.None) token = PushToken(tokens, token, out str);
            }

            return tokens;
        }

        private static void LexicalToken(bool isWhitespace, ref string str, char c, bool isStringLiteral, List<Token> tokens,
            int artParLevel, char n, ref Token token, ref int i)
        {
            if (!isWhitespace)
                str += c;
            if (isStringLiteral)
                return;

            switch (str)
            {
                case "return":
                    PushToken(tokens, new Token(TokenType.Return) { Complete = true }, out str);
                    PushToken(tokens, new Token(TokenType.ParRoundOpen) { Complete = true }, out str);
                    artParLevel++;
                    return;
                case "throw":
                    PushToken(tokens, new Token(TokenType.Throw) { Complete = true }, out str);
                    PushToken(tokens, new Token(TokenType.ParRoundOpen) { Complete = true }, out str);
                    artParLevel++;
                    return;
                case "num":
                    token = new Token(TokenType.IdentNum) { Complete = true };
                    break;
                case "str":
                    token = new Token(TokenType.IdentStr) { Complete = true };
                    break;
                case "byte":
                    token = new Token(TokenType.IdentByte) { Complete = true };
                    break;
                case "void":
                    token = new Token(TokenType.IdentVoid) { Complete = true };
                    break;
                case "true":
                    token = new Token(TokenType.LiteralTrue) { Complete = true };
                    break;
                case "false":
                    token = new Token(TokenType.LiteralFalse) { Complete = true };
                    break;
                case "null":
                    token = new Token(TokenType.LiteralNull) { Complete = true };
                    break;
                default:
                    if (Numeric.NumberRegex.IsMatch(str) && !char.IsDigit(n) && n != '.')
                    {
                        if (n == 'b' || n == 'i' || n == 'l' || n == 'f' || n == 'd')
                        {
                            str += n;
                            i++;
                        }

                        token = new Token(TokenType.LiteralNum, str) { Complete = true };
                    }
                    else if (str.Length >= 2 && str[0] == '"' && str[^1] == '"')
                    {
                        token = new Token(TokenType.LiteralStr, str.Substring(1, str.Length - 2))
                            { Complete = true };
                    }
                    else if (!char.IsLetterOrDigit(n) && str != string.Empty && !char.IsDigit(str[^1]))
                    {
                        token = new Token(TokenType.Word, str) { Complete = true };
                    }

                    break;
            }
        }

        private static Token PushToken(List<Token> tokens, Token token, out string str)
        {
            tokens.Add(token);
            token = new Token();
            str = "";
            return token;
        }
    }
}