using KScr.Lib.Model;

namespace KScr.Compiler.Class
{
    public sealed class ClassTokenizer : AbstractTokenizer
    {
        private ClassToken token
        {
            get => (Token as ClassToken)!;
            set => Token = value;
        }

        public override bool PushToken(ref IToken? token)
        {
            return base.PushToken(ref token) && (Token = new ClassToken()) != null;
        }

        public override IToken? Accept(char c, char n, char p, ref int i, ref string str)
        {
            switch (c)
            {
                // parentheses
                case '(':
                    token.Type = TokenType.ParRoundOpen;
                    token.Complete = true;
                    break;
                case ')':
                    token.Type = TokenType.ParRoundClose;
                    token.Complete = true;
                    break;
                case '[':
                    token.Type = TokenType.ParSquareOpen;
                    token.Complete = true;
                    break;
                case ']':
                    token.Type = TokenType.ParSquareClose;
                    token.Complete = true;
                    break;
                case '{':
                    token.Type = TokenType.ParAccOpen;
                    token.Complete = true;
                    break;
                case '}':
                    token.Type = TokenType.ParAccClose;
                    token.Complete = true;
                    break;
                case '<':
                    token.Type = TokenType.ParDiamondOpen;
                    token.Complete = true;
                    break;
                case '>':
                    token.Type = TokenType.ParDiamondClose;
                    token.Complete = true;
                    break;
                // lexical tokens
                default:
                    LexicalToken(ref str, c, n, p);
                    break;
            }

            return token;
        }

        private void LexicalToken(ref string str, char c, char n, char p)
        {
            if (!char.IsWhiteSpace(c))
                str += c;

            switch (str)
            {
                case "num":
                    token.Type = TokenType.IdentNum;
                    token.Complete = true;
                    break;
                case "str":
                    token.Type = TokenType.IdentStr;
                    token.Complete = true;
                    break;
                case "void":
                    token.Type = TokenType.IdentVoid;
                    token.Complete = true;
                    break;
                case "extends":
                    token.Type = TokenType.Extends;
                    token.Complete = true;
                    break;
                case "implements":
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
                    token.Type = TokenType.Word;
                    token.Arg = str;
                    token.Complete = true;
                    break;
            }
        }
    }
}