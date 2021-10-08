using System;
using System.Collections.Generic;
using KScr.Lib;
using KScr.Lib.Exception;
using KScr.Lib.Model;
using KScr.Lib.Store;

namespace KScr.Eval
{
    internal enum CompilerLevel
    {
        Statement,
        Component
    }

    public sealed class MainCompiler : ICompiler
    {
        internal readonly Bytecode Bytecode = new Bytecode();
        internal CompilerLevel CompilerLevel = CompilerLevel.Statement;
        internal Statement Statement = new Statement();
        internal StatementComponent Component = new StatementComponent();

        public IEvaluable Compile(RuntimeBase runtime, IList<Token> tokens)
        {
            var output = new Bytecode();
            var len = tokens.Count;
            ICompiler use = this;
            for (int i = 0; i < len; i++) 
                use = use.AcceptToken(runtime, tokens, ref i);
            return output;
        }

        private void PushStatement()
        {
            Bytecode.Main.Add(Statement);
            Statement = new Statement();
        }

        private void PushComponent()
        {
            Component.Statement = Statement;
            Statement.Main.Add(Component);
            Component = new StatementComponent();
        }

        // fixme proposal: allow precompiler code to generate tokens yourself
        public ICompiler AcceptToken(RuntimeBase vm, IList<Token> tokens, ref int i)
        {
            var token = tokens[i];
            var prev = i - 1 < 0 ? tokens[i - 1] : null;
            var next = i + 1 > tokens.Count ? tokens[i + 1] : null;

            switch (token.Type)
            {
                case TokenType.None:
                    break;
                case TokenType.Terminator:
                    PushStatement();
                    break;
                case TokenType.Dot:
                    break;
                case TokenType.Colon:
                    break;
                case TokenType.Comma:
                    break;
                case TokenType.Word:
                    // find type or just store word as arg because we cant use it here
                    Component.Arg = token.Arg!;
                    if (CompilerLevel == CompilerLevel.Statement)
                    {
                        Statement.Type = Component.Type = StatementComponentType.Provider;
                        Component.CodeType = BytecodeType.ExpressionVariable;
                    }
                    PushComponent();
                    break;
                case TokenType.Return:
                    break;
                case TokenType.Throw:
                    break;
                case TokenType.ParRoundOpen:
                    return new SubCompiler(this, SubCompilerMode.ParenthesesRound, sub => { });
                case TokenType.ParRoundClose:
                    break;
                case TokenType.ParSquareOpen:
                    return new SubCompiler(this, SubCompilerMode.ParenthesesSquare, sub => { });
                    break;
                case TokenType.ParSquareClose:
                    break;
                case TokenType.ParAccOpen:
                    return new SubCompiler(this, SubCompilerMode.ParenthesesAccolade, sub => { });
                    break;
                case TokenType.ParAccClose:
                    break;
                case TokenType.ParDiamondOpen:
                    break;
                case TokenType.ParDiamondClose:
                    break;
                case TokenType.IdentNum:
                    // todo:
                    // expect generic type parameters and compile them;
                    // THEN assign them here
                    // TargetType = TypeRef.NumericType();
                    if (CompilerLevel != CompilerLevel.Statement)
                        return null; // todo
                    Statement.Type = Component.Type = StatementComponentType.Declaration;
                    return new SubCompiler(this, SubCompilerMode.ParseTypeParameters, sub =>
                    {
                        Statement.TargetType = sub.TargetType;
                        CompilerLevel = CompilerLevel.Component;
                    });
                case TokenType.IdentStr:
                    if (CompilerLevel != CompilerLevel.Statement)
                        throw new CompilerException($"Illegal Identifier {token.Type} at index {i}");
                    Statement.Type = Component.Type = StatementComponentType.Declaration;
                    Statement.TargetType = TypeRef.StringType;
                    CompilerLevel = CompilerLevel.Component;
                    break;
                case TokenType.IdentByte:
                    if (CompilerLevel != CompilerLevel.Statement)
                        throw new CompilerException($"Illegal Identifier {token.Type} at index {i}");
                    Statement.Type = Component.Type = StatementComponentType.Declaration;
                    Statement.TargetType = TypeRef.NumericByteType;
                    CompilerLevel = CompilerLevel.Component;
                    break;
                case TokenType.IdentVoid:
                    if (CompilerLevel != CompilerLevel.Statement)
                        throw new CompilerException($"Illegal Identifier {token.Type} at index {i}");
                    Statement.Type = Component.Type = StatementComponentType.Declaration;
                    Statement.TargetType = TypeRef.VoidType;
                    CompilerLevel = CompilerLevel.Component;
                    break;
                case TokenType.LiteralNum:
                    if (CompilerLevel != CompilerLevel.Component)
                        throw new CompilerException("Unexpected numeric literal at index " + i);
                    if (!Statement.TargetType.FullName.StartsWith("num"))
                        throw new CompilerException("Unexpected numeric; target type is " + Statement.TargetType);
                    Component.Type = StatementComponentType.Expression;
                    Component.CodeType = BytecodeType.LiteralNumeric;
                    Component.Arg = token.Arg!;
                    PushComponent();
                    break;
                case TokenType.LiteralStr:
                    if (CompilerLevel != CompilerLevel.Component)
                        throw new CompilerException("Unexpected string literal at index " + i);
                    if (!Statement.TargetType.FullName.StartsWith("num"))
                        throw new CompilerException("Unexpected string; target type is " + Statement.TargetType);
                    Component.Type = StatementComponentType.Expression;
                    Component.CodeType = BytecodeType.LiteralString;
                    Component.Arg = token.Arg!;
                    PushComponent();
                    break;
                case TokenType.LiteralTrue:
                    if (CompilerLevel != CompilerLevel.Component)
                        throw new CompilerException("Unexpected bool literal at index " + i);
                    if (!Statement.TargetType.FullName.StartsWith("num"))
                        throw new CompilerException("Unexpected bool; target type is " + Statement.TargetType);
                    Component.Type = StatementComponentType.Expression;
                    Component.CodeType = BytecodeType.LiteralTrue;
                    Component.Arg = token.Arg!;
                    PushComponent();
                    break;
                case TokenType.LiteralFalse:
                    if (CompilerLevel != CompilerLevel.Component)
                        throw new CompilerException("Unexpected bool literal at index " + i);
                    if (!Statement.TargetType.FullName.StartsWith("num"))
                        throw new CompilerException("Unexpected bool; target type is " + Statement.TargetType);
                    Component.Type = StatementComponentType.Expression;
                    Component.CodeType = BytecodeType.LiteralFalse;
                    Component.Arg = token.Arg!;
                    PushComponent();
                    break;
                case TokenType.LiteralNull:
                    if (CompilerLevel != CompilerLevel.Component)
                        throw new CompilerException("Unexpected null literal at index " + i);
                    if (!Statement.TargetType.FullName.StartsWith("num"))
                        throw new CompilerException("Unexpected null; target type is " + Statement.TargetType);
                    Component.Type = StatementComponentType.Expression;
                    Component.CodeType = BytecodeType.LiteralNumeric;
                    Component.Arg = token.Arg!;
                    PushComponent();
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
                    if (CompilerLevel == CompilerLevel.Component)
                    {
                        // next token may not be null here
                        if (next!.Type != TokenType.OperatorEquals)
                        {
                            // is assignment
                            Component.Type = StatementComponentType.Code;
                            Component.CodeType = BytecodeType.Assignment;
                        }
                        else
                        {
                            // is equality operator
                        }
                    }
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

        public IEvaluable Compile(RuntimeBase runtime)
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

    internal  class SubCompiler : StatementComponent, ICompiler
    {
        private readonly MainCompiler _parent;
        private readonly SubCompilerMode _mode;
        private readonly Action<SubCompiler> _finishedAction;
        private readonly TokenType _firstExpected;
        private readonly TokenType _lastExpected;
        internal TypeRef TargetType = TypeRef.VoidType;
        private int _c;
        private bool _finished;
        public StatementComponentType OutputType { get; }

        public SubCompiler(MainCompiler parent, SubCompilerMode mode, Action<SubCompiler> finishedAction)
        {
            _parent = parent;
            _mode = mode;
            _finishedAction = finishedAction;

            _firstExpected = FirstExpected(mode);
            _lastExpected = LastExpected(mode);
        }

        public IEvaluable Compile(RuntimeBase runtime, IList<Token> tokens) => throw new NotSupportedException("A SubCompiler cannot compile a complete list of tokens");

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

        public IEvaluable Compile(RuntimeBase runtime)
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