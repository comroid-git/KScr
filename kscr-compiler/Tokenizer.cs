using System.Collections.Generic;
using KScr.Lib;
using KScr.Lib.Bytecode;
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
                Accept(c, n, p, ref i, ref str);

                if (!((Token as AbstractToken)?.Complete ?? false)) 
                    continue;
                PushToken();
                str = "";
            }

            return Tokens;
        }

        private int artParLevel;
        private bool isLineComment;
        private bool isStringLiteral;

        private Token token
        {
            get => (Token as Token)!;
            set => Token = value;
        }

        public void Accept(char c, char n, char p, ref int i, ref string str)
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
                return;
            }

            if (c == '/' && n == '/')
                isLineComment = true;

            if (isLineComment)
                return;

            IToken? buf;

            bool isWhitespace = c == ' ';
            /*if (isWhitespace && !isStringLiteral && token != null)
            {
                token.Complete = true;
                PushToken(ref Token);
            }*/
            if (isWhitespace && Tokens[^1].Type != TokenType.Whitespace)
            {
                token = new Token{Complete = true};
            }

            switch (c)
            {
                // terminator
                case ';':
                    while (artParLevel-- > 0)
                    {
                        buf = new Token(TokenType.ParRoundClose) { Complete = true };
                        PushToken(ref buf);
                    }

                    token = new Token(TokenType.Terminator) { Complete = true };
                    return;
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
                    PushToken(new Token(TokenType.OperatorEquals) { Complete = true });
                    // create artificial parentheses if this EQUALS operand is of an assignment
                    if (n != '=' && Tokens[^2].Type == TokenType.Word)
                    {
                        token = new Token(TokenType.ParRoundOpen) { Complete = true };
                        artParLevel++;
                    }

                    return;
                // lexical tokens
                default:
                    LexicalToken(isWhitespace, ref str, c, n, ref i);
                    break;
            }

            if (token?.Complete ?? false)
            {
                //PushToken(token);
                str = string.Empty;
            }
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
                    token = new Token(TokenType.Return) { Complete = true };
                    artParLevel++;
                    return;
                case "throw":
                    token = new Token(TokenType.Throw) { Complete = true };
                    artParLevel++;
                    return;
                case "this":
                    token = new Token(TokenType.This) { Complete = true };
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
                case "package":
                    token = new Token(TokenType.Package) { Complete = true };
                    break;
                case "import":
                    token = new Token(TokenType.Import) { Complete = true };
                    break;
                case "public":
                    AddToToken(TokenType.Public);
                    break;
                case "internal":
                    AddToToken(TokenType.Internal);
                    break;
                case "protected":
                    AddToToken(TokenType.Protected);
                    break;
                case "private":
                    AddToToken(TokenType.Private);
                    break;
                case "class":
                    AddToToken(TokenType.Class);
                    break;
                case "interface":
                    AddToToken(TokenType.Interface);
                    break;
                case "enum":
                    AddToToken(TokenType.Enum);
                    break;
                case "static":
                    AddToToken(TokenType.Static);
                    break;
                case "dynamic":
                    AddToToken(TokenType.Dynamic);
                    break;
                case "abstract":
                    AddToToken(TokenType.Abstract);
                    break;
                case "final":
                    AddToToken(TokenType.Final);
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

        private void AddToToken(TokenType tokenType)
        {
            /*if (Tokens[^2].Type.Modifier() != null)
                Tokens[^2].Type |= tokenType;
            else*/ token = new Token(tokenType){Complete = true};
        }
    }
}