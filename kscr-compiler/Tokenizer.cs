using System.Collections.Generic;
using KScr.Lib;
using KScr.Lib.Core;
using KScr.Lib.Model;

namespace KScr.Compiler
{
    public sealed class Tokenizer : ITokenizer
    {
        private readonly List<IToken> Tokens = new();

        private bool isLineComment;
        private bool isStringLiteral;
        private int lineNumber = 1;
        private int charNumber = 1;
        private string filePath;
        private SourcefilePosition srcPos;

        private bool doneAnything = false;
        private Token token;
        private Token lastToken;

        public bool PushToken()
        {
            return PushToken(ref token);
        }

        public bool PushToken(Token? token)
        {
            return PushToken(ref token);
        }

        public bool PushToken(ref Token? token)
        {
            if (token == null)
                return false;
            Tokens.Add(lastToken = token);
            token = null;
            return true;
        }

        public IList<IToken> Tokenize(RuntimeBase vm, string sourcefilePath, string source)
        {
            filePath=sourcefilePath;
            int len = source.Length;
            var str = "";

            for (var i = 0; i < len; i++)
            {
                char c = source[i];
                char n = i + 1 < len ? source[i + 1] : ' ';
                char p = i - 1 > -1 ? source[i - 1] : ' ';
                Accept(c, n, p, ref i, ref str);

                if (token == null)
                    continue;
                PushToken();
                str = "";
            }

            return Tokens;
        }

        public void Accept(char c, char n, char p, ref int i, ref string str)
        {
            if (c == '\n')
            {
                lineNumber += 1;
                charNumber = 1;
            }
            else charNumber += 1;

            // string literals
            if (isStringLiteral)
            {
                if (c != '"' && p != '\\')
                {
                    lastToken.Arg += c;
                }
                else if (c == '"' && p != '\\')
                
                    isStringLiteral = false;
            }
            else
            {
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
                    //token = new Token{Complete = true};
                }

                srcPos = new SourcefilePosition{SourcefilePath = filePath, SourcefileLine = lineNumber, SourcefileCursor = charNumber};
                switch (c)
                {
                    // terminator
                    case ';':
                        token = new Token(srcPos, TokenType.Terminator);
                        return;
                    // logistical symbols
                    case '.':
                        if (str.Length == 0 || !char.IsDigit(str[^1]))
                            token = new Token(srcPos, TokenType.Dot);
                        else
                            LexicalToken(srcPos, isWhitespace, ref str, c, n, ref i);
                        break;
                    case ',':
                        token = new Token(srcPos, TokenType.Comma);
                        break;
                    // arithmetic operators
                    case '+':
                        token = new Token(srcPos, TokenType.OperatorPlus);
                        break;
                    case '-':
                        token = new Token(srcPos, TokenType.OperatorMinus);
                        break;
                    case '*':
                        token = new Token(srcPos, TokenType.OperatorMultiply);
                        break;
                    case '/':
                        token = new Token(srcPos, TokenType.OperatorDivide);
                        break;
                    case '%':
                        token = new Token(srcPos, TokenType.OperatorModulus);
                        break;
                    case '^':
                        token = new Token(srcPos, TokenType.Circumflex);
                        break;
                    case '&':
                        token = new Token(srcPos, TokenType.Ampersand);
                        break;
                    case '|':
                        token = new Token(srcPos, TokenType.VertBar);
                        break;
                    case '!':
                        token = new Token(srcPos, TokenType.Exclamation);
                        break;
                    case '?':
                        token = new Token(srcPos, TokenType.Question);
                        break;
                    case ':':
                        token = new Token(srcPos, TokenType.Colon);
                        break;
                    case '~':
                        token = new Token(srcPos, TokenType.Tilde);
                        break;
                    // parentheses
                    case '(':
                        token = new Token(srcPos, TokenType.ParRoundOpen);
                        break;
                    case ')':
                        token = new Token(srcPos, TokenType.ParRoundClose);
                        break;
                    case '[':
                        token = new Token(srcPos, TokenType.ParSquareOpen);
                        break;
                    case ']':
                        token = new Token(srcPos, TokenType.ParSquareClose);
                        break;
                    case '{':
                        token = new Token(srcPos, TokenType.ParAccOpen);
                        break;
                    case '}':
                        token = new Token(srcPos, TokenType.ParAccClose);
                        break;
                    case '<':
                        token = new Token(srcPos, TokenType.ParDiamondOpen);
                        break;
                    case '>':
                        token = new Token(srcPos, TokenType.ParDiamondClose);
                        break;
                    case '"':
                        // ReSharper disable once AssignmentInConditionalExpression
                        isStringLiteral = true;
                        token = new Token(srcPos, TokenType.LiteralStr, "");
                        break;
                    // equals operand
                    case '=':
                        token = new Token(srcPos, TokenType.OperatorEquals);
                        break;
                    // lexical tokens
                    default:
                        LexicalToken(srcPos, isWhitespace, ref str, c, n, ref i);
                        break;
                }
            }

            if (token != null || doneAnything)
                //PushToken(token);
            {
                str = string.Empty;
                doneAnything = false;
            }
        }

        private void LexicalToken(SourcefilePosition srcPos, bool isWhitespace, ref string str, char c,
            char n, ref int i)
        {
            if (!isWhitespace)
                str += c;
            if (isStringLiteral)
                return;

            switch (str)
            {
                case "return":
                    token = new Token(srcPos, TokenType.Return);
                    return;
                case "throw":
                    token = new Token(srcPos, TokenType.Throw);
                    return;
                case "this":
                    token = new Token(srcPos, TokenType.This);
                    return;
                case "stdio":
                    token = new Token(srcPos, TokenType.StdIo);
                    return;
                case "try":
                    token = new Token(srcPos, TokenType.Try);
                    return;
                case "catch":
                    token = new Token(srcPos, TokenType.Catch);
                    return;
                case "finally":
                    token = new Token(srcPos, TokenType.Finally);
                    return;
                case "if":
                    token = new Token(srcPos, TokenType.If);
                    return;
                case "else":
                    token = new Token(srcPos, TokenType.Else);
                    return;
                case "do":
                    token = new Token(srcPos, TokenType.Do);
                    return;
                case "while":
                    token = new Token(srcPos, TokenType.While);
                    return;
                case "for":
                    if (n is 'n' or 'e')
                        break;
                    token = new Token(srcPos, TokenType.For);
                    return;
                case "forn":
                    token = new Token(srcPos, TokenType.ForN);
                    return;
                case "foreach":
                    token = new Token(srcPos, TokenType.ForEach);
                    return;
                case "switch":
                    token = new Token(srcPos, TokenType.Switch);
                    return;
                case "case":
                    token = new Token(srcPos, TokenType.Case);
                    return;
                case "default":
                    token = new Token(srcPos, TokenType.Default);
                    return;
                case "break":
                    token = new Token(srcPos, TokenType.Break);
                    return;
                case "continue":
                    token = new Token(srcPos, TokenType.Continue);
                    return;
                case "new":
                    token = new Token(srcPos, TokenType.New);
                    return;
                case "num":
                    token = new Token(srcPos, TokenType.IdentNum);
                    break;
                case "byte":
                    token = new Token(srcPos, TokenType.IdentNumByte);
                    break;
                case "short":
                    token = new Token(srcPos, TokenType.IdentNumShort);
                    break;
                case "int":
                    token = new Token(srcPos, TokenType.IdentNumInt);
                    break;
                case "long":
                    token = new Token(srcPos, TokenType.IdentNumLong);
                    break;
                case "float":
                    token = new Token(srcPos, TokenType.IdentNumFloat);
                    break;
                case "double":
                    token = new Token(srcPos, TokenType.IdentNumDouble);
                    break;
                case "str":
                    token = new Token(srcPos, TokenType.IdentStr);
                    break;
                case "void":
                    token = new Token(srcPos, TokenType.IdentVoid);
                    break;
                case "true":
                    token = new Token(srcPos, TokenType.LiteralTrue);
                    break;
                case "false":
                    token = new Token(srcPos, TokenType.LiteralFalse);
                    break;
                case "null":
                    token = new Token(srcPos, TokenType.LiteralNull);
                    break;
                case "super":
                    token = new Token(srcPos, TokenType.Super);
                    break;
                case "extends":
                    token = new Token(srcPos, TokenType.Extends);
                    break;
                case "implements":
                    token = new Token(srcPos, TokenType.Implements);
                    break;
                case "package":
                    token = new Token(srcPos, TokenType.Package);
                    break;
                case "import":
                    token = new Token(srcPos, TokenType.Import);
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
                case "annotation":
                    AddToToken(TokenType.Annotation);
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

                        token = new Token(srcPos, TokenType.LiteralNum, str);
                    }
                    else if (str.Length >= 2 && str[0] == '"' && str[^1] == '"')
                    {
                        token = new Token(srcPos, TokenType.LiteralStr, str.Substring(1, str.Length - 2))
                           ;
                    }
                    else if (!char.IsLetterOrDigit(n) && str != string.Empty && !char.IsDigit(str[^1]))
                    {
                        token = new Token(srcPos, TokenType.Word, str);
                    }

                    break;
            }
        }

        private void AddToToken(TokenType tokenType)
        {
            if (lastToken.Type.Modifier() != null)
                lastToken.Type |= tokenType;
            else token = new Token(new SourcefilePosition{SourcefilePath = filePath, SourcefileLine = lineNumber, SourcefileCursor = charNumber}, tokenType);
            doneAnything = true;
        }
    }
}