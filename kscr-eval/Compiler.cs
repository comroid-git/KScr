using System;
using System.Collections.Generic;
using KScr.Lib;
using KScr.Lib.Exception;
using KScr.Lib.Model;
using KScr.Lib.Store;

namespace KScr.Eval
{
    public sealed class MainCompiler : ICompiler
    {
        public StatementComponentType OutputType { get; private set; }
        private BytecodePacket packet;
        private TypeRef TargetType = TypeRef.VoidType;
        private string Arg = string.Empty;

        public Bytecode Compile(RuntimeBase runtime, IList<Token> tokens)
        {
            var output = new Bytecode();
            return output;
        }

        // fixme proposal: allow precompiler code to generate tokens yourself
        public ICompiler AcceptToken(RuntimeBase vm, ref Bytecode bytecode, IList<Token> tokens, ref int i)
        {
            var token = tokens[i];
            var prev = i - 1 < 0 ? tokens[i - 1] : null;
            var next = i + 1 > tokens.Count ? tokens[i + 1] : null;

            switch (token.Type)
            {
                case TokenType.None:
                    break;
                case TokenType.Terminator:
                    break;
                case TokenType.Dot:
                    break;
                case TokenType.Colon:
                    break;
                case TokenType.Comma:
                    break;
                case TokenType.Word:
                    // find type or just store word as arg because we cant use it here
                    TargetType = vm.FindType(Arg = token.Arg!) ?? TypeRef.VoidType;
                    break;
                case TokenType.Return:
                    break;
                case TokenType.Throw:
                    break;
                case TokenType.ParRoundOpen:
                    break;
                case TokenType.ParRoundClose:
                    break;
                case TokenType.ParSquareOpen:
                    break;
                case TokenType.ParSquareClose:
                    break;
                case TokenType.ParAccOpen:
                    break;
                case TokenType.ParAccClose:
                    break;
                case TokenType.ParDiamondOpen:
                    break;
                case TokenType.ParDiamondClose:
                    break;
                case TokenType.IdentNum:
                    OutputType = StatementComponentType.Declaration;
                    // todo:
                    // expect generic type parameters and compile them;
                    // THEN assign them here
                    // TargetType = TypeRef.NumericType();
                    OutputType = StatementComponentType.Declaration;
                    return new SubCompiler(this, SubCompilerMode.ParseTypeParameters, sub => TargetType = sub.TargetType);
                case TokenType.IdentStr:
                    OutputType = StatementComponentType.Declaration;
                    TargetType = TypeRef.StringType;
                    break;
                case TokenType.IdentByte:
                    OutputType = StatementComponentType.Declaration;
                    TargetType = TypeRef.NumericByteType;
                    break;
                case TokenType.IdentVoid:
                    OutputType = StatementComponentType.Declaration;
                    TargetType = TypeRef.VoidType;
                    break;
                case TokenType.LiteralNum:
                    OutputType = StatementComponentType.Expression;
                    TargetType = TypeRef.VoidType;
                    break;
                case TokenType.LiteralStr:
                    break;
                case TokenType.LiteralTrue:
                    break;
                case TokenType.LiteralFalse:
                    break;
                case TokenType.LiteralNull:
                    break;
                case TokenType.OperatorPlus:
                    break;
                case TokenType.OperatorMinus:
                    break;
                case TokenType.OperatorMultiply:
                    break;
                case TokenType.OperatorDivide:
                    break;
                case TokenType.OperatorModulus:
                    break;
                case TokenType.OperatorEquals:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return this;
        }

        public IStatementComponent Compose(RuntimeBase runtime)
        {
            throw new NotImplementedException();
        }

        public BytecodePacket Compile(RuntimeBase runtime)
        {
            throw new NotImplementedException();
        }
    }

    public enum SubCompilerMode
    {
        ParenthesesRound,
        ParenthesesSquare,
        ParenthesesAccolade,
        ParseTypeParameters
    }

    internal  class SubCompiler : ICompiler
    {
        private readonly ICompiler _parent;
        private readonly SubCompilerMode _mode;
        private readonly Action<SubCompiler> _finishedAction;
        private readonly TokenType _firstExpected;
        private readonly TokenType _lastExpected;
        internal TypeRef TargetType = TypeRef.VoidType;
        private int _c;
        private bool _finished;
        public StatementComponentType OutputType { get; }

        public SubCompiler(ICompiler parent, SubCompilerMode mode, Action<SubCompiler> finishedAction)
        {
            _parent = parent;
            _mode = mode;
            _finishedAction = finishedAction;

            _firstExpected = FirstExpected(mode);
            _lastExpected = LastExpected(mode);
        }

        public Bytecode Compile(RuntimeBase runtime, IList<Token> tokens) => throw new NotSupportedException("A SubCompiler cannot compile a complete list of tokens");

        public ICompiler AcceptToken(RuntimeBase vm, IList<Token> tokens, ref int i)
        {
            var token = tokens[i];
            var prev = i - 1 < 0 ? tokens[i - 1] : null;
            var next = i + 1 > tokens.Count ? tokens[i + 1] : null;

            if (_c++ == 0 && token.Type != _firstExpected)
                throw new CompilerException($"First expected token was {_firstExpected}; got {token}");
            _finished = token.Type == _lastExpected;

            switch (token.Type)
            {
                case TokenType.None:
                    break;
                case TokenType.Terminator:
                    break;
                case TokenType.Dot:
                    break;
                case TokenType.Colon:
                    break;
                case TokenType.Comma:
                    break;
                case TokenType.Word:
                    switch (_mode)
                    {
                        case SubCompilerMode.ParenthesesRound:
                            break;
                        case SubCompilerMode.ParenthesesSquare:
                            break;
                        case SubCompilerMode.ParenthesesAccolade:
                            break;
                        case SubCompilerMode.ParseTypeParameters:
                            TargetType = vm.FindType(token.Arg);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    break;
                case TokenType.Return:
                    break;
                case TokenType.Throw:
                    break;
                case TokenType.ParRoundOpen:
                    break;
                case TokenType.ParRoundClose:
                    break;
                case TokenType.ParSquareOpen:
                    break;
                case TokenType.ParSquareClose:
                    break;
                case TokenType.ParAccOpen:
                    break;
                case TokenType.ParAccClose:
                    break;
                case TokenType.ParDiamondOpen:
                    // expect a set of generic type parameters or definitions
                    break;
                case TokenType.ParDiamondClose:
                    break;
                case TokenType.IdentNum:
                    break;
                case TokenType.IdentStr:
                    break;
                case TokenType.IdentByte:
                    break;
                case TokenType.IdentVoid:
                    break;
                case TokenType.LiteralNum:
                    break;
                case TokenType.LiteralStr:
                    break;
                case TokenType.LiteralTrue:
                    break;
                case TokenType.LiteralFalse:
                    break;
                case TokenType.LiteralNull:
                    break;
                case TokenType.OperatorPlus:
                    break;
                case TokenType.OperatorMinus:
                    break;
                case TokenType.OperatorMultiply:
                    break;
                case TokenType.OperatorDivide:
                    break;
                case TokenType.OperatorModulus:
                    break;
                case TokenType.OperatorEquals:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (_finished)
            {
                _finishedAction(this);
                return _parent;
            }
            return this;
        }

        public IStatementComponent Compose(RuntimeBase runtime)
        {
            throw new NotImplementedException();
        }

        public BytecodePacket Compile(RuntimeBase runtime)
        {
            throw new NotImplementedException();
        }

        private static TokenType FirstExpected(SubCompilerMode mode) => mode switch
        {
            SubCompilerMode.ParenthesesRound => TokenType.ParRoundOpen,
            SubCompilerMode.ParenthesesSquare => TokenType.ParSquareOpen,
            SubCompilerMode.ParenthesesAccolade => TokenType.ParAccOpen,
            SubCompilerMode.ParseTypeParameters => TokenType.ParDiamondOpen,
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };

        private static TokenType LastExpected(SubCompilerMode mode) => mode switch
        {
            SubCompilerMode.ParenthesesRound => TokenType.ParRoundClose,
            SubCompilerMode.ParenthesesSquare => TokenType.ParSquareClose,
            SubCompilerMode.ParenthesesAccolade => TokenType.ParAccClose,
            SubCompilerMode.ParseTypeParameters => TokenType.ParDiamondClose,
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };
    }
}