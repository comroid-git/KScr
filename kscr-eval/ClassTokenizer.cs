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
                        token.Type |= ClassTokenType.ParRoundOpen;
                        token.Complete = true;
                        break;
                    case ')':
                        token.Type |= ClassTokenType.ParRoundClose;
                        token.Complete = true;
                        break;
                    case '[':
                        token.Type |= ClassTokenType.ParSquareOpen;
                        token.Complete = true;
                        break;
                    case ']':
                        token.Type |= ClassTokenType.ParSquareClose;
                        token.Complete = true;
                        break;
                    case '{':
                        token.Type |= ClassTokenType.ParAccOpen;
                        token.Complete = true;
                        break;
                    case '}':
                        token.Type |= ClassTokenType.ParAccClose;
                        token.Complete = true;
                        break;
                    case '<':
                        token.Type |= ClassTokenType.ParDiamondOpen;
                        token.Complete = true;
                        break;
                    case '>':
                        token.Type |= ClassTokenType.ParDiamondClose;
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
                    token.Type = ClassTokenType.IdentNum;
                    token.Complete = true;
                    break;
                case "str":
                    token.Type = ClassTokenType.IdentStr;
                    token.Complete = true;
                    break;
                case "void":
                    token.Type = ClassTokenType.IdentVoid;
                    token.Complete = true;
                    break;
                case "extends":
                    token.Type = ClassTokenType.Extends;
                    token.Complete = true;
                    break;
                case "implements":
                    token.Type = ClassTokenType.Implements;
                    token.Complete = true;
                    break;
                case "public":
                    token.Type |= ClassTokenType.Public;
                    break;
                case "internal":
                    token.Type |= ClassTokenType.Internal;
                    break;
                case "protected":
                    token.Type |= ClassTokenType.Protected;
                    break;
                case "private":
                    token.Type |= ClassTokenType.Private;
                    break;
                case "class":
                    token.Type |= ClassTokenType.Class;
                    break;
                case "interface":
                    token.Type |= ClassTokenType.Interface;
                    break;
                case "enum":
                    token.Type |= ClassTokenType.Enum;
                    break;
                case "static":
                    token.Type |= ClassTokenType.Static;
                    break;
                case "dynamic":
                    token.Type |= ClassTokenType.Dynamic;
                    break;
                case "abstract":
                    token.Type |= ClassTokenType.Abstract;
                    break;
                case "final":
                    token.Type |= ClassTokenType.Final;
                    break;
                default:
                    token.Type = ClassTokenType.Word;
                    token.Arg = str;
                    token.Complete = true;
                    break;
            }
        }
    }
}