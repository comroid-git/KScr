using System.Collections.Generic;
using KScr.Lib.Core;
using KScr.Lib.Model;

namespace KScr.Eval
{
    public sealed class CodeTokenizer : ICodeTokenizer
    {
        public IList<CodeToken> Tokenize(string source)
        {
            CodeToken codeToken = new CodeToken();
            List<CodeToken> tokens = new List<CodeToken>();
            string str = "";
            int len = source.Length;
            var isLineComment = false;
            var isStringLiteral = false;
            var artParLevel = 0;

            for (var i = 0; i < len; i++)
            {
                char c = source[i];
                char n = i + 1 < len ? source[i + 1] : ' ';
                char p = i - 1 > 0 ? source[i - 1] : ' ';

                // string literals
                if (isStringLiteral)
                {
                    if ((c != '"') & (p != '\\'))
                        codeToken.Arg += c;
                    else if (c == '"' && p != '\\')
                        codeToken.Complete = true;
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

                bool isWhitespace = c == ' ';

                if (isWhitespace && !isStringLiteral)
                    codeToken.Complete = true;

                switch (c)
                {
                    // terminator
                    case ';':
                        while (artParLevel-- > 0)
                            PushToken(tokens, new CodeToken(CodeTokenType.ParRoundClose) { Complete = true }, out str);
                        PushToken(tokens, new CodeToken(CodeTokenType.Terminator) { Complete = true }, out str);
                        continue;
                    // logistical symbols
                    case '.':
                        if (str.Length == 0 || !char.IsDigit(str[^1]))
                            codeToken = new CodeToken(CodeTokenType.Dot) { Complete = true };
                        else
                            LexicalToken(isWhitespace, ref str, c, isStringLiteral, tokens, artParLevel, n,
                                ref codeToken, ref i);
                        break;
                    case ':':
                        codeToken = new CodeToken(CodeTokenType.Colon) { Complete = true };
                        break;
                    case ',':
                        codeToken = new CodeToken(CodeTokenType.Comma) { Complete = true };
                        break;
                    // arithmetic operators
                    case '+':
                        codeToken = new CodeToken(CodeTokenType.OperatorPlus) { Complete = true };
                        break;
                    case '-':
                        codeToken = new CodeToken(CodeTokenType.OperatorMinus) { Complete = true };
                        break;
                    case '*':
                        codeToken = new CodeToken(CodeTokenType.OperatorMultiply) { Complete = true };
                        break;
                    case '/':
                        codeToken = new CodeToken(CodeTokenType.OperatorDivide) { Complete = true };
                        break;
                    case '%':
                        codeToken = new CodeToken(CodeTokenType.OperatorModulus) { Complete = true };
                        break;
                    // parentheses
                    case '(':
                        codeToken = new CodeToken(CodeTokenType.ParRoundOpen) { Complete = true };
                        break;
                    case ')':
                        codeToken = new CodeToken(CodeTokenType.ParRoundClose) { Complete = true };
                        break;
                    case '[':
                        codeToken = new CodeToken(CodeTokenType.ParSquareOpen) { Complete = true };
                        break;
                    case ']':
                        codeToken = new CodeToken(CodeTokenType.ParSquareClose) { Complete = true };
                        break;
                    case '{':
                        codeToken = new CodeToken(CodeTokenType.ParAccOpen) { Complete = true };
                        break;
                    case '}':
                        codeToken = new CodeToken(CodeTokenType.ParAccClose) { Complete = true };
                        break;
                    case '<':
                        codeToken = new CodeToken(CodeTokenType.ParDiamondOpen) { Complete = true };
                        break;
                    case '>':
                        codeToken = new CodeToken(CodeTokenType.ParDiamondClose) { Complete = true };
                        break;
                    case '"':
                        // ReSharper disable once AssignmentInConditionalExpression
                        if (isStringLiteral = !isStringLiteral)
                            codeToken = new CodeToken(CodeTokenType.LiteralStr, "");
                        break;
                    // equals operand
                    case '=':
                        PushToken(tokens, new CodeToken(CodeTokenType.OperatorEquals) { Complete = true }, out str);
                        // create artificial parentheses if this EQUALS operand is of an assignment
                        if (n != '=' && tokens[^2].Type == CodeTokenType.Word)
                        {
                            PushToken(tokens, new CodeToken(CodeTokenType.ParRoundOpen) { Complete = true }, out str);
                            artParLevel++;
                        }

                        continue;
                    // lexical tokens
                    default:
                        LexicalToken(isWhitespace, ref str, c, isStringLiteral, tokens, artParLevel, n, ref codeToken,
                            ref i);
                        break;
                }

                if (codeToken.Complete && codeToken.Type != CodeTokenType.None)
                    codeToken = PushToken(tokens, codeToken, out str);
            }

            return tokens;
        }

        private static void LexicalToken(bool isWhitespace, ref string str, char c, bool isStringLiteral,
            List<CodeToken> tokens,
            int artParLevel, char n, ref CodeToken codeToken, ref int i)
        {
            if (!isWhitespace)
                str += c;
            if (isStringLiteral)
                return;

            switch (str)
            {
                case "return":
                    PushToken(tokens, new CodeToken(CodeTokenType.Return) { Complete = true }, out str);
                    PushToken(tokens, new CodeToken(CodeTokenType.ParRoundOpen) { Complete = true }, out str);
                    artParLevel++;
                    return;
                case "throw":
                    PushToken(tokens, new CodeToken(CodeTokenType.Throw) { Complete = true }, out str);
                    PushToken(tokens, new CodeToken(CodeTokenType.ParRoundOpen) { Complete = true }, out str);
                    artParLevel++;
                    return;
                case "num":
                    codeToken = new CodeToken(CodeTokenType.IdentNum) { Complete = true };
                    break;
                case "str":
                    codeToken = new CodeToken(CodeTokenType.IdentStr) { Complete = true };
                    break;
                case "void":
                    codeToken = new CodeToken(CodeTokenType.IdentVoid) { Complete = true };
                    break;
                case "true":
                    codeToken = new CodeToken(CodeTokenType.LiteralTrue) { Complete = true };
                    break;
                case "false":
                    codeToken = new CodeToken(CodeTokenType.LiteralFalse) { Complete = true };
                    break;
                case "null":
                    codeToken = new CodeToken(CodeTokenType.LiteralNull) { Complete = true };
                    break;
                default:
                    if (Numeric.NumberRegex.IsMatch(str) && !char.IsDigit(n) && n != '.')
                    {
                        if (n == 'b' || n == 'i' || n == 'l' || n == 'f' || n == 'd')
                        {
                            str += n;
                            i++;
                        }

                        codeToken = new CodeToken(CodeTokenType.LiteralNum, str) { Complete = true };
                    }
                    else if (str.Length >= 2 && str[0] == '"' && str[^1] == '"')
                    {
                        codeToken = new CodeToken(CodeTokenType.LiteralStr, str.Substring(1, str.Length - 2))
                            { Complete = true };
                    }
                    else if (!char.IsLetterOrDigit(n) && str != string.Empty && !char.IsDigit(str[^1]))
                    {
                        codeToken = new CodeToken(CodeTokenType.Word, str) { Complete = true };
                    }

                    break;
            }
        }

        private static CodeToken PushToken(List<CodeToken> tokens, CodeToken codeToken, out string str)
        {
            tokens.Add(codeToken);
            codeToken = new CodeToken();
            str = "";
            return codeToken;
        }
    }
}