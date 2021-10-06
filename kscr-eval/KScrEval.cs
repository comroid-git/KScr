using System;
using System.Collections.Generic;
using System.Globalization;
using KScr.Lib.Core;
using KScr.Lib.Model;
using KScr.Lib.VM;

namespace KScr.Eval
{
    public sealed class KScrEval : VirtualMachine
    {
        private int nextIntoAlt = -1;
        private int nextIntoSub = -1;

        private Bytecode output;
        private string parentheses = "";
        private readonly List<BytecodePacket> parenthesesPackets = new List<BytecodePacket>();
        private BytecodePacket prevPacket;

        public KScrEval()
        {
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
        }

        private static Token PushToken(List<Token> tokens, Token token, out string str)
        {
            tokens.Add(token);
            token = new Token();
            str = "";
            return token;
        }

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
                    case '"':
                        if (isStringLiteral = !isStringLiteral)
                            token = new Token(TokenType.LiteralStr, "");
                        break;
                    // equals operand
                    case '=':
                        PushToken(tokens, new Token(TokenType.OperatorEquals) { Complete = true }, out str);
                        // create artificial parentheses if this EQUALS operand is of an assignment
                        if (n != '=' && tokens[^2].Type == TokenType.Var)
                        {
                            PushToken(tokens, new Token(TokenType.ParRoundOpen) { Complete = true }, out str);
                            artParLevel++;
                        }

                        continue;
                    // lexical tokens
                    default:
                        if (!isWhitespace)
                            str += c;
                        if (isStringLiteral)
                            break;

                        switch (str)
                        {
                            case "return":
                                PushToken(tokens, new Token(TokenType.Return) { Complete = true }, out str);
                                PushToken(tokens, new Token(TokenType.ParRoundOpen) { Complete = true }, out str);
                                artParLevel++;
                                continue;
                            case "throw":
                                PushToken(tokens, new Token(TokenType.Throw) { Complete = true }, out str);
                                PushToken(tokens, new Token(TokenType.ParRoundOpen) { Complete = true }, out str);
                                artParLevel++;
                                continue;
                            case "var":
                                token = new Token(TokenType.IdentVar) { Complete = true };
                                break;
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
                                    token = new Token(TokenType.Var, str) { Complete = true };
                                }

                                break;
                        }

                        break;
                }

                if (token.Complete && token.Type != TokenType.None) token = PushToken(tokens, token, out str);
            }

            return tokens;
        }

        private void PushPacket(BytecodePacket packet)
        {
            if (nextIntoAlt == 0)
                prevPacket.AltPacket = packet;
            else if (nextIntoSub == 0)
                prevPacket.SubPacket = packet;
            else if (parenthesesPackets.Count > 0)
                parenthesesPackets[^1].SubStack.Add(packet);
            else output.main.Add(packet);

            prevPacket = packet;

            if (nextIntoAlt >= 0)
                nextIntoAlt--;
            if (nextIntoSub >= 0)
                nextIntoSub--;
        }

        public Bytecode Compile(IList<Token> tokens)
        {
            output = new Bytecode();
            var len = tokens.Count;

            for (var i = 0; i < len; i++)
            {
                Token token = tokens[i];
                var next = i + 1 < len ? tokens[i + 1] : null;

                int mylen;
                switch (token.Type)
                {
                    // terminator
                    case TokenType.Terminator:
                        PushPacket(new BytecodePacket());
                        break;
                    // syntax statements
                    case TokenType.Return:
                        nextIntoAlt = 1;
                        PushPacket(new BytecodePacket(BytecodeType.Return));
                        break;
                    case TokenType.Throw:
                        nextIntoAlt = 1;
                        PushPacket(new BytecodePacket(BytecodeType.Throw));
                        break;
                    // declarations
                    case TokenType.IdentVar:
                        if (next.Type != TokenType.Var)
                            throw new Exception("Invalid declaration: Missing variable name");
                        PushPacket(new BytecodePacket(BytecodeType.DeclarationVariable, next.Arg));
                        i++; // skip next because variable name is included in declaration
                        continue;
                    case TokenType.IdentNum:
                        if (next.Type != TokenType.Var)
                            throw new Exception("Invalid declaration: Missing variable name");
                        PushPacket(new BytecodePacket(BytecodeType.DeclarationNumeric, next.Arg));
                        i++;
                        continue;
                    case TokenType.IdentStr:
                        if (next.Type != TokenType.Var)
                            throw new Exception("Invalid declaration: Missing variable name");
                        PushPacket(new BytecodePacket(BytecodeType.DeclarationString, next.Arg));
                        i++;
                        continue;
                    case TokenType.IdentByte:
                        if (next.Type != TokenType.Var)
                            throw new Exception("Invalid declaration: Missing variable name");
                        PushPacket(new BytecodePacket(BytecodeType.DeclarationByte, next.Arg));
                        i++;
                        continue;
                    case TokenType.IdentVoid:
                        // ignored
                        break;
                    // operators
                    case TokenType.OperatorPlus:
                        nextIntoAlt = 1;
                        PushPacket(new BytecodePacket(BytecodeType.OperatorPlus));
                        continue;
                    case TokenType.OperatorMinus:
                        nextIntoAlt = 1;
                        PushPacket(new BytecodePacket(BytecodeType.OperatorMinus));
                        continue;
                    case TokenType.OperatorMultiply:
                        nextIntoAlt = 1;
                        PushPacket(new BytecodePacket(BytecodeType.OperatorMultiply));
                        continue;
                    case TokenType.OperatorDivide:
                        nextIntoAlt = 1;
                        PushPacket(new BytecodePacket(BytecodeType.OperatorDivide));
                        continue;
                    case TokenType.OperatorModulus:
                        nextIntoAlt = 1;
                        PushPacket(new BytecodePacket(BytecodeType.OperatorModulus));
                        continue;
                    // EQUALS-operator
                    case TokenType.OperatorEquals:
                        // assignments
                        if (((prevPacket.Type & BytecodeType.Declaration) != 0 ||
                             (prevPacket.Type & BytecodeType.ExpressionVariable) != 0)
                            && next.Type ==
                            TokenType.ParRoundOpen) // assignment is properly delimited by round parentheses
                        {
                            nextIntoSub = 1;
                            PushPacket(new BytecodePacket(BytecodeType.Assignment));
                        }

                        break;
                    // literals
                    case TokenType.LiteralNum:
                        PushPacket(new BytecodePacket(BytecodeType.LiteralNumeric, Numeric.Compile(this, token.Arg)));
                        continue;
                    case TokenType.LiteralStr:
                        PushPacket(new BytecodePacket(BytecodeType.LiteralString, token.Arg));
                        continue;
                    case TokenType.LiteralTrue:
                        PushPacket(new BytecodePacket(BytecodeType.LiteralTrue));
                        continue;
                    case TokenType.LiteralFalse:
                        PushPacket(new BytecodePacket(BytecodeType.LiteralFalse));
                        continue;
                    case TokenType.LiteralNull:
                        PushPacket(new BytecodePacket(BytecodeType.Null));
                        continue;
                    case TokenType.Var:
                        PushPacket(new BytecodePacket(BytecodeType.ExpressionVariable, token.Arg));
                        continue;
                    // parentheses
                    case TokenType.ParRoundOpen:
                        PushPacket(new BytecodePacket(BytecodeType.Parentheses, '('));
                        parentheses += '(';
                        parenthesesPackets.Add(prevPacket);
                        continue;
                    case TokenType.ParRoundClose:
                        if (parentheses[^1] != '(')
                            throw new Exception("Invalid Parentheses sequence: ')' cannot close '" + parentheses[^1] +
                                                '\'');
                        mylen = parentheses.Length - 1;
                        parentheses = parentheses.Substring(0, mylen);
                        parenthesesPackets.RemoveAt(mylen);
                        continue;
                    case TokenType.ParSquareOpen:
                        PushPacket(new BytecodePacket(BytecodeType.Parentheses, '['));
                        parentheses += '[';
                        parenthesesPackets.Add(prevPacket);
                        continue;
                    case TokenType.ParSquareClose:
                        if (parentheses[^1] != '[')
                            throw new Exception("Invalid Parentheses sequence: ']' cannot close '" + parentheses[^1] +
                                                '\'');
                        mylen = parentheses.Length - 1;
                        parentheses = parentheses.Substring(0, mylen);
                        parenthesesPackets.RemoveAt(mylen);
                        continue;
                    case TokenType.ParAccOpen:
                        PushPacket(new BytecodePacket(BytecodeType.Parentheses, '{'));
                        parentheses += '{';
                        parenthesesPackets.Add(prevPacket);
                        continue;
                    case TokenType.ParAccClose:
                        if (parentheses[^1] != '{')
                            throw new Exception("Invalid Parentheses sequence: '}' cannot close '" + parentheses[^1] +
                                                '\'');
                        mylen = parentheses.Length - 1;
                        parentheses = parentheses.Substring(0, mylen);
                        parenthesesPackets.RemoveAt(mylen);
                        continue;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return output;
        }

        public IObject? Execute(Bytecode bytecode, VirtualMachine vm, out long timeµs)
        {
            timeµs = UnixTime();
            var yield = Execute(bytecode, vm);
            timeµs = UnixTime() - timeµs;
            return yield;
        }

        public IObject? Execute(Bytecode bytecode, VirtualMachine vm)
        {
            return bytecode.main.Evaluate(vm);
        }

        private long UnixTime()
        {
            var epochStart = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return (DateTime.UtcNow - epochStart).Ticks / 10;
        }

        public override ObjectStore ObjectStore { get; } = new ObjectStore();
        public override TypeStore TypeStore { get; } = new TypeStore();
    }
}