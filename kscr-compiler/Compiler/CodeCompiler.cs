using System;
using System.Collections.Generic;
using KScr.Lib;
using KScr.Lib.Bytecode;
using KScr.Lib.Core;
using KScr.Lib.Exception;
using KScr.Lib.Model;
using KScr.Lib.Store;

namespace KScr.Eval
{
    public sealed class MainCodeCompiler : AbstractCompiler, ICodeCompiler
    {
        internal readonly ExecutableCode ExecutableCode = new ExecutableCode();
        private Statement _statement = new Statement();
        internal StatementComponent Component = new StatementComponent();
        internal StatementComponent? PrevComponent;

        public ICodeCompiler? Parent => null;

        public IStatement<IStatementComponent> Statement
        {
            get => _statement;
            private set => _statement = (value as Statement)!;
        }

        public CompilerLevel CompilerLevel { get; private set; } = CompilerLevel.Statement;

        public IRuntimeSite Compile(RuntimeBase runtime, IList<IToken> tokens)
        {
        }

        public override IRuntimeSite Compile(RuntimeBase runtime)
        {
            return ExecutableCode;
        }

        // fixme proposal: allow precompiler code to generate tokens yourself
        public override ICompiler AcceptToken(RuntimeBase vm, IToken token1, IToken? next1, IToken? prev1, ref int i)
        {
            var token = token1[i];
            var prev = i - 1 < 0 ? null : token1[i - 1];
            var next = i + 1 >= token1.Count ? null : token1[i + 1];

            switch (token.Type)
            {
                case TokenType.None:
                    break;
                case TokenType.Terminator:
                    PushStatement();
                    break;
                case TokenType.Dot:
                    if (PrevComponent != null && (PrevComponent.Type & StatementComponentType.Expression) != 0)
                    {
                        --i;
                        var compiler = new SubCodeCompiler(this, SubCompilerMode.Call, sub =>
                        {
                            Component = sub;
                            PushComponent();
                        });
                        return compiler;
                    }

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
                        _statement.Type = Component.Type = StatementComponentType.Provider;
                        Component.CodeType = BytecodeType.ExpressionVariable;
                        CompilerLevel = CompilerLevel.Component;
                    }

                    PushComponent();

                    break;
                case TokenType.Return:
                    if (CompilerLevel != CompilerLevel.Statement)
                        throw new CompilerException("Unexpected return statement");
                    _statement.Type = Component.Type = StatementComponentType.Code;
                    Component.CodeType = BytecodeType.Return;
                    CompilerLevel = CompilerLevel.Component;
                    PushComponent();
                    --i;
                    var rtnExpr = new SubCodeCompiler(this, SubCompilerMode.Expression, sub =>
                    {
                        Component = sub;
                        PushComponent(true);
                    });
                    Component.SubComponent = rtnExpr;
                    return rtnExpr;
                case TokenType.Throw:
                    break;
                case TokenType.LiteralNum:
                case TokenType.LiteralStr:
                case TokenType.LiteralTrue:
                case TokenType.LiteralFalse:
                case TokenType.LiteralNull:
                case TokenType.ParRoundOpen:
                    --i;
                    CompilerLevel = CompilerLevel.Component;
                    Component.Type = StatementComponentType.Expression;
                    var sub1 = new SubCodeCompiler(this, SubCompilerMode.Expression, sub =>
                    {
                        if (!_statement.TargetType.CanHold(sub.TargetType))
                            throw new CompilerException(
                                $"Incompatible expression type: {sub.TargetType}; expected type: {_statement.TargetType}");
                        // todo: is this old?
                        //if (next?.Type == TokenType.Dot)
                        if (next?.Type != TokenType.Terminator || next?.Type != TokenType.ParRoundClose)
                        {
                            Component = sub;
                            PushComponent();
                        }
                    })
                    {
                        Arg = Component.Arg,
                        Type = Component.Type,
                        CodeType = Component.CodeType,
                        VariableContext = Component.VariableContext
                    };
                    Component.SubComponent = sub1;
                    return sub1;
                case TokenType.ParRoundClose:
                    break;
                case TokenType.ParSquareOpen:
                    --i;
                    return new SubCodeCompiler(this, SubCompilerMode.ParenthesesSquare, sub => { });
                    break;
                case TokenType.ParSquareClose:
                    break;
                case TokenType.ParAccOpen:
                    --i;
                    return new SubCodeCompiler(this, SubCompilerMode.ParenthesesAccolade, sub => { });
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
                    _statement.Type = Component.Type = StatementComponentType.Declaration;
                    return new SubCodeCompiler(this, SubCompilerMode.ParseTypeParameters, sub =>
                    {
                        _statement.TargetType = sub.TargetType;
                        CompilerLevel = CompilerLevel.Component;
                    });
                case TokenType.IdentStr:
                    if (CompilerLevel != CompilerLevel.Statement)
                        throw new CompilerException($"Illegal Identifier {token.Type} at index {i}");
                    _statement.Type = Component.Type = StatementComponentType.Declaration;
                    _statement.TargetType = ClassRef.StringType;
                    CompilerLevel = CompilerLevel.Component;
                    break;
                case TokenType.IdentVoid:
                    if (CompilerLevel != CompilerLevel.Statement)
                        throw new CompilerException($"Illegal Identifier {token.Type} at index {i}");
                    _statement.Type = Component.Type = StatementComponentType.Declaration;
                    _statement.TargetType = ClassRef.VoidType;
                    CompilerLevel = CompilerLevel.Component;
                    break;
                case TokenType.OperatorPlus:
                case TokenType.OperatorMinus:
                case TokenType.OperatorMultiply:
                case TokenType.OperatorDivide:
                case TokenType.OperatorModulus:
                    if (PrevComponent != null && (PrevComponent.Type & StatementComponentType.Expression) != 0)
                    {
                        --i;
                        var compiler = new SubCodeCompiler(this, SubCompilerMode.Operator, sub =>
                        {
                            Component = sub;
                            PushComponent();
                        });
                        return compiler;
                    }

                    break;
                case TokenType.OperatorEquals:
                    if (CompilerLevel == CompilerLevel.Component)
                        // next token may not be null here
                        if (next!.Type != TokenType.OperatorEquals)
                        {
                            // is assignment
                            Component.Type = StatementComponentType.Code;
                            Component.CodeType = BytecodeType.Assignment;
                            CompilerLevel = CompilerLevel.Component;
                            PushComponent();
                            var compiler = new SubCodeCompiler(this, SubCompilerMode.Expression, sub =>
                            {
                                Component = sub;
                                PushComponent(true);
                            });
                            return compiler;
                        }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return this;
        }

        private void PushStatement()
        {
            ExecutableCode.Main.Add(_statement);
            Statement = new Statement();
            CompilerLevel = CompilerLevel.Statement;
        }

        private void PushComponent(bool sub = false)
        {
            var swap = PrevComponent;
            PrevComponent = Component;
            Component.Statement = _statement;
            if (sub && swap != null)
                swap.SubComponent = Component;
            else Statement.Main.Add(Component);
            Component = new StatementComponent();
        }

        public static BytecodeType GetCodeType(TokenType tokenType)
        {
            return tokenType switch
            {
                TokenType.OperatorPlus => BytecodeType.OperatorPlus,
                TokenType.OperatorMinus => BytecodeType.OperatorMinus,
                TokenType.OperatorMultiply => BytecodeType.OperatorMultiply,
                TokenType.OperatorDivide => BytecodeType.OperatorDivide,
                TokenType.OperatorModulus => BytecodeType.OperatorModulus,
                _ => throw new ArgumentOutOfRangeException(nameof(tokenType), tokenType, null)
            };
        }
    }

    public enum SubCompilerMode
    {
        Expression,
        ParenthesesSquare,
        ParenthesesAccolade,
        ParseTypeParameters,
        Call,
        Operator
    }

    internal class SubCodeCompiler : StatementComponent, ICodeCompiler
    {
        private readonly Action<SubCodeCompiler> _finishedAction;
        private readonly TokenType[] _firstAllowed;
        private readonly TokenType? _lastExpected;
        private readonly SubCompilerMode _mode;
        private int _c;
        private bool _finished;
        internal IClassRef TargetType = ClassRef.VoidType;

        public SubCodeCompiler(ICodeCompiler parent, SubCompilerMode mode, Action<SubCodeCompiler> finishedAction)
        {
            ;
            Parent = parent;
            _mode = mode;
            _finishedAction = finishedAction;

            _firstAllowed = FirstAllowed(mode);
            _lastExpected = LastExpected(mode);
        }

        private Statement _statement => (Statement as Statement)!;

        public IStatement<IStatementComponent> Statement => Parent.Statement;

        public CompilerLevel CompilerLevel => Parent.CompilerLevel;

        public IRuntimeSite Compile(RuntimeBase runtime, IList<IToken> tokens)
        {
            throw new NotSupportedException("A SubCompiler cannot compile a complete list of tokens");
        }

        public ICodeCompiler Parent { get; }

        public IRuntimeSite Compile(RuntimeBase runtime)
        {
            return Parent.Compile(runtime);
        }

        public ICompiler AcceptToken(RuntimeBase vm, IToken token1, IToken? next1, IToken? prev1, ref int i)
        {
            var token = token1[i];
            var prev = i - 1 < 0 ? null : token1[i - 1];
            var next = i + 1 >= token1.Count ? null : token1[i + 1];

            if (_c++ == 0 && _mode != SubCompilerMode.Expression && !_firstAllowed.Contains(token.Type))
                throw new CompilerException(
                    $"First allowed tokens were {string.Join(",", _firstAllowed)}; got {token}");
            _finished = token.Type == _lastExpected || next?.Type == TokenType.Terminator;

            switch (token.Type)
            {
                case TokenType.None:
                    break;
                case TokenType.Terminator:
                    break;
                case TokenType.Dot:
                    if (CompilerLevel != CompilerLevel.Component)
                        throw new CompilerException("Invalid CompilerLevel for dot Token");
                    if (!(next is { Type: TokenType.Word }))
                        throw new CompilerException("Unexpected token; dot must be followed by a word");
                    Arg = next.Arg!;
                    _statement.Type = Type = StatementComponentType.Provider;
                    CodeType = BytecodeType.Call;
                    i++;
                    _finished = true;
                    break;
                case TokenType.Colon:
                    break;
                case TokenType.Comma:
                    break;
                case TokenType.Word:
                    switch (_mode)
                    {
                        case SubCompilerMode.Expression:
                            Type = StatementComponentType.Provider;
                            CodeType = BytecodeType.ExpressionVariable;
                            Arg = token.Arg!;
                            _finished = true;
                            break;
                        case SubCompilerMode.ParenthesesSquare:
                            break;
                        case SubCompilerMode.ParenthesesAccolade:
                            break;
                        case SubCompilerMode.ParseTypeParameters:
                            TargetType = vm.FindType(token.Arg!) ??
                                         throw new CompilerException("Type not found: " + token.Arg);
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
                case TokenType.IdentVoid:
                    break;
                case TokenType.LiteralNum:
                    if (Parent.CompilerLevel != CompilerLevel.Component)
                        throw new CompilerException("Unexpected numeric literal at index " + i);
                    var num = Numeric.Compile(vm, token.Arg!);
                    if (!Statement.TargetType.CanHold(num.Type))
                        throw new CompilerException("Unexpected numeric; target type is " + Statement.TargetType);
                    Type = StatementComponentType.Expression;
                    CodeType = BytecodeType.LiteralNumeric;
                    Arg = num.Value?.ToString(IObject.ToString_LongName) ?? token.Arg!;
                    TargetType = num.Type;
                    // todo: again; is this old????
                    //_finished = next?.Type == TokenType.Dot;
                    _finished = next?.Type != TokenType.Terminator || next?.Type != TokenType.ParRoundClose;
                    break;
                case TokenType.LiteralStr:
                    if (Parent.CompilerLevel != CompilerLevel.Component)
                        throw new CompilerException("Unexpected string literal at index " + i);
                    if (!Statement.TargetType.CanHold(ClassRef.StringType))
                        throw new CompilerException("Unexpected string; target type is " + Statement.TargetType);
                    Type = StatementComponentType.Expression;
                    CodeType = BytecodeType.LiteralString;
                    Arg = token.Arg!;
                    TargetType = ClassRef.StringType;
                    _finished = next?.Type != TokenType.Terminator || next?.Type != TokenType.ParRoundClose;
                    break;
                case TokenType.LiteralTrue:
                    if (Parent.CompilerLevel != CompilerLevel.Component)
                        throw new CompilerException("Unexpected bool literal at index " + i);
                    if (!Statement.TargetType.CanHold(ClassRef.NumericShortType))
                        throw new CompilerException("Unexpected bool; target type is " + Statement.TargetType);
                    Type = StatementComponentType.Expression;
                    CodeType = BytecodeType.LiteralTrue;
                    Arg = token.Arg!;
                    TargetType = ClassRef.NumericShortType;
                    _finished = next?.Type == TokenType.Dot;
                    TargetType = ClassRef.NumericShortType;
                    _finished = next?.Type != TokenType.Terminator || next?.Type != TokenType.ParRoundClose;
                    break;
                case TokenType.LiteralFalse:
                    if (Parent.CompilerLevel != CompilerLevel.Component)
                        throw new CompilerException("Unexpected bool literal at index " + i);
                    if (!Statement.TargetType.CanHold(ClassRef.NumericShortType))
                        throw new CompilerException("Unexpected bool; target type is " + Statement.TargetType);
                    Type = StatementComponentType.Expression;
                    CodeType = BytecodeType.LiteralFalse;
                    Arg = token.Arg!;
                    TargetType = ClassRef.NumericShortType;
                    _finished = next?.Type == TokenType.Dot;
                    TargetType = ClassRef.NumericShortType;
                    _finished = next?.Type != TokenType.Terminator || next?.Type != TokenType.ParRoundClose;
                    break;
                case TokenType.LiteralNull:
                    if (Parent.CompilerLevel != CompilerLevel.Component)
                        throw new CompilerException("Unexpected null literal at index " + i);
                    if (!Statement.TargetType.CanHold(ClassRef.VoidType))
                        throw new CompilerException("Unexpected null; target type is " + Statement.TargetType);
                    Type = StatementComponentType.Expression;
                    CodeType = BytecodeType.Null;
                    Arg = token.Arg!;
                    TargetType = ClassRef.VoidType;
                    //_finished = next?.Type == TokenType.Dot;
                    _finished = next?.Type != TokenType.Terminator || next?.Type != TokenType.ParRoundClose;
                    break;
                case TokenType.OperatorPlus:
                case TokenType.OperatorMinus:
                case TokenType.OperatorMultiply:
                case TokenType.OperatorDivide:
                case TokenType.OperatorModulus:
                    if (CompilerLevel != CompilerLevel.Component)
                        throw new CompilerException("Invalid CompilerLevel for operator Token");
                    if (_mode != SubCompilerMode.Operator)
                        throw new CompilerException("Invalid compiler mode for operator Token");
                    Arg = token.Type.ToString();
                    _statement.Type = Type = StatementComponentType.Provider;
                    CodeType = BytecodeType.Call;
                    i++;
                    _finished = true;
                    break;
                case TokenType.OperatorEquals:
                    break;
            }

            if (_finished)
            {
                _finishedAction(this);
                return Parent;
            }

            return this;
        }

        private static TokenType[] FirstAllowed(SubCompilerMode mode)
        {
            return mode switch
            {
                SubCompilerMode.ParenthesesSquare => new[] { TokenType.ParSquareOpen },
                SubCompilerMode.ParenthesesAccolade => new[] { TokenType.ParAccOpen },
                SubCompilerMode.ParseTypeParameters => new[] { TokenType.ParDiamondOpen },
                SubCompilerMode.Expression => new[] { TokenType.ParRoundOpen },
                SubCompilerMode.Call => new[] { TokenType.Dot },
                SubCompilerMode.Operator => new[]
                {
                    TokenType.OperatorPlus,
                    TokenType.OperatorMinus,
                    TokenType.OperatorMultiply,
                    TokenType.OperatorDivide,
                    TokenType.OperatorModulus
                },
                _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
            };
        }

        private static TokenType? LastExpected(SubCompilerMode mode)
        {
            return mode switch
            {
                SubCompilerMode.ParenthesesSquare => TokenType.ParSquareClose,
                SubCompilerMode.ParenthesesAccolade => TokenType.ParAccClose,
                SubCompilerMode.ParseTypeParameters => TokenType.ParDiamondClose,
                SubCompilerMode.Expression => TokenType.ParRoundClose,
                _ => null
            };
        }
    }
}