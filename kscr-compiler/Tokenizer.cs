using System.Collections.Generic;
using KScr.Lib;
using KScr.Lib.Core;
using KScr.Lib.Model;

namespace KScr.Compiler
{
    public sealed class Tokenizer : ITokenizer
    {
        private IToken? Token;
        private readonly List<IToken> Tokens = new List<IToken>();

        public bool PushToken()
        {
            return PushToken(ref Token);
        }

        public bool PushToken(IToken? token)
        {
            return PushToken(ref token);
        }

        public bool PushToken(ref IToken? token)
        {
            if (token == null)
                return false;
            Tokens.Add(token);
            token = new Token();
            return true;
        }

        public IList<IToken> Tokenize(RuntimeBase vm, string source)
        {
            int len = source.Length;
            string str = "";

            for (var i = 0; i < len; i++)
            {
                char c = source[i];
                char n = i + 1 < len ? source[i + 1] : ' ';
                char p = i - 1 > 0 ? source[i - 1] : ' ';
                Token = (Accept(c, n, p, ref i, ref str) as ClassToken)!;

                if (!(Token as AbstractToken)!.Complete) 
                    continue;
                PushToken();
                str = "";
            }

            return Tokens;
        }

        private int artParLevel;
        private bool isLineComment;
        private bool isStringLiteral;
        private readonly List<IToken> tokens = new List<IToken>();

        private Token token
        {
            get => (Token as Token)!;
            set => Token = value;
        }

        public IToken? Accept(char c, char n, char p, ref int i, ref string str)
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

            IToken? buf;
            bool isWhitespace = c == ' ';
            if (isWhitespace)
            {
                buf = new Token{Complete = true};
                PushToken(ref buf);
            }

            if (isWhitespace && !isStringLiteral)
                token.Complete = true;

            switch (c)
            {
                // terminator
                case ';':
                    while (artParLevel-- > 0)
                    {
                        buf = new Token(TokenType.ParRoundClose) { Complete = true };
                        PushToken(ref buf);
                    }

                    return new Token(TokenType.Terminator) { Complete = true };
                // logistical symbols
                case '.':
                    if (str.Length == 0 || !char.IsDigit(str[^1]))
                        token = new Token(TokenType.Dot) { Complete = true };
                    else
                        LexicalToken(isWhitespace, ref str, c, n, ref i);
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
                    buf = new Token(TokenType.OperatorEquals) { Complete = true };
                    PushToken(ref buf);
                    // create artificial parentheses if this EQUALS operand is of an assignment
                    if (n != '=' && tokens[^2].Type == TokenType.Word)
                    {
                        buf = new Token(TokenType.ParRoundOpen) { Complete = true };
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
                    PushToken(new Token(TokenType.Return) { Complete = true });
                    PushToken(new Token(TokenType.ParRoundOpen) { Complete = true });
                    artParLevel++;
                    return;
                case "throw":
                    PushToken(new Token(TokenType.Throw) { Complete = true });
                    PushToken(new Token(TokenType.ParRoundOpen) { Complete = true });
                    artParLevel++;
                    return;
                case "this":
                    PushToken(new Token(TokenType.This) { Complete = true });
                    return;
                case "num":
                    token = new Token(TokenType.IdentNum) { Complete = true };
                    break;
                case "str":
                    token = new Token(TokenType.IdentStr) { Complete = true };
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
                case "extends":
                    token = new Token(TokenType.Extends) { Complete = true };
                    break;
                case "implements":
                    token = new Token(TokenType.Implements) { Complete = true };
                    token.Type = TokenType.Implements;
                    token.Complete = true;
                    break;
                case "public":
                    token.Type |= TokenType.Public;
                    break;
                case "internal":
                    token.Type |= TokenType.Internal;
                    break;
                case "protected":
                    token.Type |= TokenType.Protected;
                    break;
                case "private":
                    token.Type |= TokenType.Private;
                    break;
                case "class":
                    token.Type |= TokenType.Class;
                    break;
                case "interface":
                    token.Type |= TokenType.Interface;
                    break;
                case "enum":
                    token.Type |= TokenType.Enum;
                    break;
                case "static":
                    token.Type |= TokenType.Static;
                    break;
                case "dynamic":
                    token.Type |= TokenType.Dynamic;
                    break;
                case "abstract":
                    token.Type |= TokenType.Abstract;
                    break;
                case "final":
                    token.Type |= TokenType.Final;
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
    }
}