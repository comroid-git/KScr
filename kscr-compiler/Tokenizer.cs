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
        private string filePath;

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
                lineNumber += 1;
            
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

                switch (c)
                {
                    // terminator
                    case ';':
                        token = new Token(new SourcefilePosition{SourcefilePath = filePath, SourcefileLine = lineNumber}, TokenType.Terminator);
                        return;
                    // logistical symbols
                    case '.':
                        if (str.Length == 0 || !char.IsDigit(str[^1]))
                            token = new Token(new SourcefilePosition{SourcefilePath = filePath, SourcefileLine = lineNumber}, TokenType.Dot);
                        else
                            LexicalToken(isWhitespace, ref str, c, n, ref i);
                        break;
                    case ',':
                        token = new Token(new SourcefilePosition{SourcefilePath = filePath, SourcefileLine = lineNumber}, TokenType.Comma);
                        break;
                    // arithmetic operators
                    case '+':
                        token = new Token(new SourcefilePosition{SourcefilePath = filePath, SourcefileLine = lineNumber}, TokenType.OperatorPlus);
                        break;
                    case '-':
                        token = new Token(new SourcefilePosition{SourcefilePath = filePath, SourcefileLine = lineNumber}, TokenType.OperatorMinus);
                        break;
                    case '*':
                        token = new Token(new SourcefilePosition{SourcefilePath = filePath, SourcefileLine = lineNumber}, TokenType.OperatorMultiply);
                        break;
                    case '/':
                        token = new Token(new SourcefilePosition{SourcefilePath = filePath, SourcefileLine = lineNumber}, TokenType.OperatorDivide);
                        break;
                    case '%':
                        token = new Token(new SourcefilePosition{SourcefilePath = filePath, SourcefileLine = lineNumber}, TokenType.OperatorModulus);
                        break;
                    case '^':
                        token = new Token(new SourcefilePosition{SourcefilePath = filePath, SourcefileLine = lineNumber}, TokenType.Circumflex);
                        break;
                    case '&':
                        token = new Token(new SourcefilePosition{SourcefilePath = filePath, SourcefileLine = lineNumber}, TokenType.Ampersand);
                        break;
                    case '|':
                        token = new Token(new SourcefilePosition{SourcefilePath = filePath, SourcefileLine = lineNumber}, TokenType.VertBar);
                        break;
                    case '!':
                        token = new Token(new SourcefilePosition{SourcefilePath = filePath, SourcefileLine = lineNumber}, TokenType.Exclamation);
                        break;
                    case '?':
                        token = new Token(new SourcefilePosition{SourcefilePath = filePath, SourcefileLine = lineNumber}, TokenType.Question);
                        break;
                    case ':':
                        token = new Token(new SourcefilePosition{SourcefilePath = filePath, SourcefileLine = lineNumber}, TokenType.Colon);
                        break;
                    case '~':
                        token = new Token(new SourcefilePosition{SourcefilePath = filePath, SourcefileLine = lineNumber}, TokenType.Tilde);
                        break;
                    // parentheses
                    case '(':
                        token = new Token(new SourcefilePosition{SourcefilePath = filePath, SourcefileLine = lineNumber}, TokenType.ParRoundOpen);
                        break;
                    case ')':
                        token = new Token(new SourcefilePosition{SourcefilePath = filePath, SourcefileLine = lineNumber}, TokenType.ParRoundClose);
                        break;
                    case '[':
                        token = new Token(new SourcefilePosition{SourcefilePath = filePath, SourcefileLine = lineNumber}, TokenType.ParSquareOpen);
                        break;
                    case ']':
                        token = new Token(new SourcefilePosition{SourcefilePath = filePath, SourcefileLine = lineNumber}, TokenType.ParSquareClose);
                        break;
                    case '{':
                        token = new Token(new SourcefilePosition{SourcefilePath = filePath, SourcefileLine = lineNumber}, TokenType.ParAccOpen);
                        break;
                    case '}':
                        token = new Token(new SourcefilePosition{SourcefilePath = filePath, SourcefileLine = lineNumber}, TokenType.ParAccClose);
                        break;
                    case '<':
                        token = new Token(new SourcefilePosition{SourcefilePath = filePath, SourcefileLine = lineNumber}, TokenType.ParDiamondOpen);
                        break;
                    case '>':
                        token = new Token(new SourcefilePosition{SourcefilePath = filePath, SourcefileLine = lineNumber}, TokenType.ParDiamondClose);
                        break;
                    case '"':
                        // ReSharper disable once AssignmentInConditionalExpression
                        isStringLiteral = true;
                        token = new Token(new SourcefilePosition{SourcefilePath = filePath, SourcefileLine = lineNumber}, TokenType.LiteralStr, "");
                        break;
                    // equals operand
                    case '=':
                        token = new Token(new SourcefilePosition{SourcefilePath = filePath, SourcefileLine = lineNumber}, TokenType.OperatorEquals);
                        break;
                    // lexical tokens
                    default:
                        LexicalToken(isWhitespace, ref str, c, n, ref i);
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

        private void LexicalToken(bool isWhitespace, ref string str, char c, char n, ref int i)
        {
            if (!isWhitespace)
                str += c;
            if (isStringLiteral)
                return;

            switch (str)
            {
                case "return":
                    token = new Token(new SourcefilePosition{SourcefilePath = filePath, SourcefileLine = lineNumber}, TokenType.Return);
                    return;
                case "throw":
                    token = new Token(new SourcefilePosition{SourcefilePath = filePath, SourcefileLine = lineNumber}, TokenType.Throw);
                    return;
                case "this":
                    token = new Token(new SourcefilePosition{SourcefilePath = filePath, SourcefileLine = lineNumber}, TokenType.This);
                    return;
                case "stdio":
                    token = new Token(new SourcefilePosition{SourcefilePath = filePath, SourcefileLine = lineNumber}, TokenType.StdIo);
                    return;
                case "try":
                    token = new Token(new SourcefilePosition{SourcefilePath = filePath, SourcefileLine = lineNumber}, TokenType.Try);
                    return;
                case "catch":
                    token = new Token(new SourcefilePosition{SourcefilePath = filePath, SourcefileLine = lineNumber}, TokenType.Catch);
                    return;
                case "finally":
                    token = new Token(new SourcefilePosition{SourcefilePath = filePath, SourcefileLine = lineNumber}, TokenType.Finally);
                    return;
                case "if":
                    token = new Token(new SourcefilePosition{SourcefilePath = filePath, SourcefileLine = lineNumber}, TokenType.If);
                    return;
                case "else":
                    token = new Token(new SourcefilePosition{SourcefilePath = filePath, SourcefileLine = lineNumber}, TokenType.Else);
                    return;
                case "do":
                    token = new Token(new SourcefilePosition{SourcefilePath = filePath, SourcefileLine = lineNumber}, TokenType.Do);
                    return;
                case "while":
                    token = new Token(new SourcefilePosition{SourcefilePath = filePath, SourcefileLine = lineNumber}, TokenType.While);
                    return;
                case "for":
                    if (n is 'n' or 'e')
                        break;
                    token = new Token(new SourcefilePosition{SourcefilePath = filePath, SourcefileLine = lineNumber}, TokenType.For);
                    return;
                case "forn":
                    token = new Token(new SourcefilePosition{SourcefilePath = filePath, SourcefileLine = lineNumber}, TokenType.ForN);
                    return;
                case "foreach":
                    token = new Token(new SourcefilePosition{SourcefilePath = filePath, SourcefileLine = lineNumber}, TokenType.ForEach);
                    return;
                case "switch":
                    token = new Token(new SourcefilePosition{SourcefilePath = filePath, SourcefileLine = lineNumber}, TokenType.Switch);
                    return;
                case "case":
                    token = new Token(new SourcefilePosition{SourcefilePath = filePath, SourcefileLine = lineNumber}, TokenType.Case);
                    return;
                case "default":
                    token = new Token(new SourcefilePosition{SourcefilePath = filePath, SourcefileLine = lineNumber}, TokenType.Default);
                    return;
                case "break":
                    token = new Token(new SourcefilePosition{SourcefilePath = filePath, SourcefileLine = lineNumber}, TokenType.Break);
                    return;
                case "continue":
                    token = new Token(new SourcefilePosition{SourcefilePath = filePath, SourcefileLine = lineNumber}, TokenType.Continue);
                    return;
                case "new":
                    token = new Token(new SourcefilePosition{SourcefilePath = filePath, SourcefileLine = lineNumber}, TokenType.New);
                    return;
                case "num":
                    token = new Token(new SourcefilePosition{SourcefilePath = filePath, SourcefileLine = lineNumber}, TokenType.IdentNum);
                    break;
                case "byte":
                    token = new Token(new SourcefilePosition{SourcefilePath = filePath, SourcefileLine = lineNumber}, TokenType.IdentNumByte);
                    break;
                case "short":
                    token = new Token(new SourcefilePosition{SourcefilePath = filePath, SourcefileLine = lineNumber}, TokenType.IdentNumShort);
                    break;
                case "int":
                    token = new Token(new SourcefilePosition{SourcefilePath = filePath, SourcefileLine = lineNumber}, TokenType.IdentNumInt);
                    break;
                case "long":
                    token = new Token(new SourcefilePosition{SourcefilePath = filePath, SourcefileLine = lineNumber}, TokenType.IdentNumLong);
                    break;
                case "float":
                    token = new Token(new SourcefilePosition{SourcefilePath = filePath, SourcefileLine = lineNumber}, TokenType.IdentNumFloat);
                    break;
                case "double":
                    token = new Token(new SourcefilePosition{SourcefilePath = filePath, SourcefileLine = lineNumber}, TokenType.IdentNumDouble);
                    break;
                case "str":
                    token = new Token(new SourcefilePosition{SourcefilePath = filePath, SourcefileLine = lineNumber}, TokenType.IdentStr);
                    break;
                case "void":
                    token = new Token(new SourcefilePosition{SourcefilePath = filePath, SourcefileLine = lineNumber}, TokenType.IdentVoid);
                    break;
                case "true":
                    token = new Token(new SourcefilePosition{SourcefilePath = filePath, SourcefileLine = lineNumber}, TokenType.LiteralTrue);
                    break;
                case "false":
                    token = new Token(new SourcefilePosition{SourcefilePath = filePath, SourcefileLine = lineNumber}, TokenType.LiteralFalse);
                    break;
                case "null":
                    token = new Token(new SourcefilePosition{SourcefilePath = filePath, SourcefileLine = lineNumber}, TokenType.LiteralNull);
                    break;
                case "super":
                    token = new Token(new SourcefilePosition{SourcefilePath = filePath, SourcefileLine = lineNumber}, TokenType.Super);
                    break;
                case "extends":
                    token = new Token(new SourcefilePosition{SourcefilePath = filePath, SourcefileLine = lineNumber}, TokenType.Extends);
                    break;
                case "implements":
                    token = new Token(new SourcefilePosition{SourcefilePath = filePath, SourcefileLine = lineNumber}, TokenType.Implements);
                    break;
                case "package":
                    token = new Token(new SourcefilePosition{SourcefilePath = filePath, SourcefileLine = lineNumber}, TokenType.Package);
                    break;
                case "import":
                    token = new Token(new SourcefilePosition{SourcefilePath = filePath, SourcefileLine = lineNumber}, TokenType.Import);
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

                        token = new Token(new SourcefilePosition{SourcefilePath = filePath, SourcefileLine = lineNumber}, TokenType.LiteralNum, str);
                    }
                    else if (str.Length >= 2 && str[0] == '"' && str[^1] == '"')
                    {
                        token = new Token(new SourcefilePosition{SourcefilePath = filePath, SourcefileLine = lineNumber}, TokenType.LiteralStr, str.Substring(1, str.Length - 2))
                           ;
                    }
                    else if (!char.IsLetterOrDigit(n) && str != string.Empty && !char.IsDigit(str[^1]))
                    {
                        token = new Token(new SourcefilePosition{SourcefilePath = filePath, SourcefileLine = lineNumber}, TokenType.Word, str);
                    }

                    break;
            }
        }

        private void AddToToken(TokenType tokenType)
        {
            if (lastToken.Type.Modifier() != null)
                lastToken.Type |= tokenType;
            else token = new Token(new SourcefilePosition{SourcefilePath = filePath, SourcefileLine = lineNumber}, tokenType);
            doneAnything = true;
        }
    }
}