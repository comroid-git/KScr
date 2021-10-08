using System;
using System.Collections.Generic;
using KScr.Lib.Bytecode;
using KScr.Lib.Model;

namespace KScr.Eval
{
    public sealed class ClassTokenizer : IClassTokenizer
    {

        public List<ClassToken> Tokenize(string source)
        {
            ClassToken token = new ClassToken();
            List<ClassToken> tokens = new List<ClassToken>();
            var len = source.Length;
            string str = "";

            for (var i = 0; i < len; i++)
            {
                char c = source[i];
                char n = i + 1 < len ? source[i + 1] : ' ';
                char p = i - 1 > 0 ? source[i - 1] : ' ';

                switch (c)
                {
                    // parentheses
                    case '(':
                        token.Modifier |= ClassTokenType.ParRoundOpen;
                        token.Complete = true;
                        break;
                    case ')':
                        token.Modifier |= ClassTokenType.ParRoundClose;
                        token.Complete = true;
                        break;
                    case '[':
                        token.Modifier |= ClassTokenType.ParSquareOpen;
                        token.Complete = true;
                        break;
                    case ']':
                        token.Modifier |= ClassTokenType.ParSquareClose;
                        token.Complete = true;
                        break;
                    case '{':
                        token.Modifier |= ClassTokenType.ParAccOpen;
                        token.Complete = true;
                        break;
                    case '}':
                        token.Modifier |= ClassTokenType.ParAccClose;
                        token.Complete = true;
                        break;
                    case '<':
                        token.Modifier |= ClassTokenType.ParDiamondOpen;
                        token.Complete = true;
                        break;
                    case '>':
                        token.Modifier |= ClassTokenType.ParDiamondClose;
                        token.Complete = true;
                        break;
                    // lexical tokens
                    default:
                        LexicalToken(ref token, ref str, c,n,p);
                        break;
                }

                if (token.Complete)
                {
                    tokens.Add(token);
                    token = new ClassToken();
                    str = "";
                }
            }

            return tokens;
        }

        private void LexicalToken(ref ClassToken token, ref string str, char c, char n, char p)
        {
            if (!char.IsWhiteSpace(c))
                str += c;

            switch (str)
            {
                case "num":
                    token.Modifier = ClassTokenType.IdentNum;
                    token.Complete = true;
                    break;
                case "str":
                    token.Modifier = ClassTokenType.IdentStr;
                    token.Complete = true;
                    break;
                case "void":
                    token.Modifier = ClassTokenType.IdentVoid;
                    token.Complete = true;
                    break;
                case "extends":
                    token.Modifier = ClassTokenType.Extends;
                    token.Complete = true;
                    break;
                case "implements":
                    token.Modifier = ClassTokenType.Implements;
                    token.Complete = true;
                    break;
                case "public":
                    token.Modifier |= ClassTokenType.Public;
                    break;
                case "internal":
                    token.Modifier |= ClassTokenType.Internal;
                    break;
                case "protected":
                    token.Modifier |= ClassTokenType.Protected;
                    break;
                case "private":
                    token.Modifier |= ClassTokenType.Private;
                    break;
                case "class":
                    token.Modifier |= ClassTokenType.Class;
                    break;
                case "interface":
                    token.Modifier |= ClassTokenType.Interface;
                    break;
                case "enum":
                    token.Modifier |= ClassTokenType.Enum;
                    break;
                case "static":
                    token.Modifier |= ClassTokenType.Static;
                    break;
                case "dynamic":
                    token.Modifier |= ClassTokenType.Dynamic;
                    break;
                case "abstract":
                    token.Modifier |= ClassTokenType.Abstract;
                    break;
                case "final":
                    token.Modifier |= ClassTokenType.Final;
                    break;
                default:
                    token.Modifier = ClassTokenType.Word;
                    token.Arg = str;
                    token.Complete = true;
                    break;
            }
        }
    }
}