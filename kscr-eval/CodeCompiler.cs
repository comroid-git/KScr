using System;
using System.Collections.Generic;
using KScr.Lib;
using KScr.Lib.Core;
using KScr.Lib.Exception;
using KScr.Lib.Model;
using KScr.Lib.Store;

namespace KScr.Eval
{
    public sealed class MainCodeCompiler : ICodeCompiler
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

        public IEvaluable Compile(RuntimeBase runtime, IList<CodeToken> tokens)
        {
            int len = tokens.Count;
            ICodeCompiler use = this;
            for (var i = 0; i < len; i++)
                use = use.AcceptToken(runtime, tokens, ref i);
            return use.Compile(runtime);
        }

        public IEvaluable Compile(RuntimeBase runtime)
        {
            return ExecutableCode;
        }

        // fixme proposal: allow precompiler code to generate tokens yourself
        public ICodeCompiler AcceptToken(RuntimeBase vm, IList<CodeToken> tokens, ref int i)
        {
            var token = tokens[i];
            var prev = i - 1 < 0 ? null : tokens[i - 1];
            var next = i + 1 >= tokens.Count ? null : tokens[i + 1];

            switch (token.Type)
            {
                case CodeTokenType.None:
                    break;
                case CodeTokenType.Terminator:
                    PushStatement();
                    break;
                case CodeTokenType.Dot:
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
                case CodeTokenType.Colon:
                    break;
                case CodeTokenType.Comma:
                    break;
                case CodeTokenType.Word:
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
                case CodeTokenType.Return:
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
                case CodeTokenType.Throw:
                    break;
                case CodeTokenType.LiteralNum:
                case CodeTokenType.LiteralStr:
                case CodeTokenType.LiteralTrue:
                case CodeTokenType.LiteralFalse:
                case CodeTokenType.LiteralNull:
                case CodeTokenType.ParRoundOpen:
                    --i;
                    CompilerLevel = CompilerLevel.Component;
                    Component.Type = StatementComponentType.Expression;
                    var sub1 = new SubCodeCompiler(this, SubCompilerMode.Expression, sub =>
                    {
                        if (!_statement.TargetType.CanHold(sub.TargetType))
                            throw new CompilerException(
                                $"Incompatible expression type: {sub.TargetType}; expected type: {_statement.TargetType}");
                        // todo: is this old?
                        //if (next?.Type == CodeTokenType.Dot)
                        if (next?.Type != CodeTokenType.Terminator || next?.Type != CodeTokenType.ParRoundClose)
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
                case CodeTokenType.ParRoundClose:
                    break;
                case CodeTokenType.ParSquareOpen:
                    --i;
                    return new SubCodeCompiler(this, SubCompilerMode.ParenthesesSquare, sub => { });
                    break;
                case CodeTokenType.ParSquareClose:
                    break;
                case CodeTokenType.ParAccOpen:
                    --i;
                    return new SubCodeCompiler(this, SubCompilerMode.ParenthesesAccolade, sub => { });
                    break;
                case CodeTokenType.ParAccClose:
                    break;
                case CodeTokenType.ParDiamondOpen:
                    break;
                case CodeTokenType.ParDiamondClose:
                    break;
                case CodeTokenType.IdentNum:
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
                case CodeTokenType.IdentStr:
                    if (CompilerLevel != CompilerLevel.Statement)
                        throw new CompilerException($"Illegal Identifier {token.Type} at index {i}");
                    _statement.Type = Component.Type = StatementComponentType.Declaration;
                    _statement.TargetType = ClassRef.StringType;
                    CompilerLevel = CompilerLevel.Component;
                    break;
                case CodeTokenType.IdentVoid:
                    if (CompilerLevel != CompilerLevel.Statement)
                        throw new CompilerException($"Illegal Identifier {token.Type} at index {i}");
                    _statement.Type = Component.Type = StatementComponentType.Declaration;
                    _statement.TargetType = ClassRef.VoidType;
                    CompilerLevel = CompilerLevel.Component;
                    break;
                case CodeTokenType.OperatorPlus:
                case CodeTokenType.OperatorMinus:
                case CodeTokenType.OperatorMultiply:
                case CodeTokenType.OperatorDivide:
                case CodeTokenType.OperatorModulus:
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
                case CodeTokenType.OperatorEquals:
                    if (CompilerLevel == CompilerLevel.Component)
                        // next token may not be null here
                        if (next!.Type != CodeTokenType.OperatorEquals)
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

        public static BytecodeType GetCodeType(CodeTokenType tokenType)
        {
            return tokenType switch
            {
                CodeTokenType.OperatorPlus => BytecodeType.OperatorPlus,
                CodeTokenType.OperatorMinus => BytecodeType.OperatorMinus,
                CodeTokenType.OperatorMultiply => BytecodeType.OperatorMultiply,
                CodeTokenType.OperatorDivide => BytecodeType.OperatorDivide,
                CodeTokenType.OperatorModulus => BytecodeType.OperatorModulus,
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
        private readonly CodeTokenType[] _firstAllowed;
        private readonly CodeTokenType? _lastExpected;
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

        public IEvaluable Compile(RuntimeBase runtime, IList<CodeToken> tokens)
        {
            throw new NotSupportedException("A SubCompiler cannot compile a complete list of tokens");
        }

        public ICodeCompiler Parent { get; }

        public IEvaluable Compile(RuntimeBase runtime)
        {
            return Parent.Compile(runtime);
        }

        public ICodeCompiler AcceptToken(RuntimeBase vm, IList<CodeToken> tokens, ref int i)
        {
            var token = tokens[i];
            var prev = i - 1 < 0 ? null : tokens[i - 1];
            var next = i + 1 >= tokens.Count ? null : tokens[i + 1];

            if (_c++ == 0 && _mode != SubCompilerMode.Expression && !_firstAllowed.Contains(token.Type))
                throw new CompilerException(
                    $"First allowed tokens were {string.Join(",", _firstAllowed)}; got {token}");
            _finished = token.Type == _lastExpected || next?.Type == CodeTokenType.Terminator;

            switch (token.Type)
            {
                case CodeTokenType.None:
                    break;
                case CodeTokenType.Terminator:
                    break;
                case CodeTokenType.Dot:
                    if (CompilerLevel != CompilerLevel.Component)
                        throw new CompilerException("Invalid CompilerLevel for dot Token");
                    if (!(next is { Type: CodeTokenType.Word }))
                        throw new CompilerException("Unexpected token; dot must be followed by a word");
                    Arg = next.Arg!;
                    _statement.Type = Type = StatementComponentType.Provider;
                    CodeType = BytecodeType.Call;
                    i++;
                    _finished = true;
                    break;
                case CodeTokenType.Colon:
                    break;
                case CodeTokenType.Comma:
                    break;
                case CodeTokenType.Word:
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
                case CodeTokenType.Return:
                    break;
                case CodeTokenType.Throw:
                    break;
                case CodeTokenType.ParRoundOpen:
                    break;
                case CodeTokenType.ParRoundClose:
                    break;
                case CodeTokenType.ParSquareOpen:
                    break;
                case CodeTokenType.ParSquareClose:
                    break;
                case CodeTokenType.ParAccOpen:
                    break;
                case CodeTokenType.ParAccClose:
                    break;
                case CodeTokenType.ParDiamondOpen:
                    // expect a set of generic type parameters or definitions
                    break;
                case CodeTokenType.ParDiamondClose:
                    break;
                case CodeTokenType.IdentNum:
                    break;
                case CodeTokenType.IdentStr:
                    break;
                case CodeTokenType.IdentVoid:
                    break;
                case CodeTokenType.LiteralNum:
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
                    //_finished = next?.Type == CodeTokenType.Dot;
                    _finished = next?.Type != CodeTokenType.Terminator || next?.Type != CodeTokenType.ParRoundClose;
                    break;
                case CodeTokenType.LiteralStr:
                    if (Parent.CompilerLevel != CompilerLevel.Component)
                        throw new CompilerException("Unexpected string literal at index " + i);
                    if (!Statement.TargetType.CanHold(ClassRef.StringType))
                        throw new CompilerException("Unexpected string; target type is " + Statement.TargetType);
                    Type = StatementComponentType.Expression;
                    CodeType = BytecodeType.LiteralString;
                    Arg = token.Arg!;
                    TargetType = ClassRef.StringType;
                    _finished = next?.Type != CodeTokenType.Terminator || next?.Type != CodeTokenType.ParRoundClose;
                    break;
                case CodeTokenType.LiteralTrue:
                    if (Parent.CompilerLevel != CompilerLevel.Component)
                        throw new CompilerException("Unexpected bool literal at index " + i);
                    if (!Statement.TargetType.CanHold(ClassRef.NumericShortType))
                        throw new CompilerException("Unexpected bool; target type is " + Statement.TargetType);
                    Type = StatementComponentType.Expression;
                    CodeType = BytecodeType.LiteralTrue;
                    Arg = token.Arg!;
                    TargetType = ClassRef.NumericShortType;
                    _finished = next?.Type == CodeTokenType.Dot;
                    TargetType = ClassRef.NumericShortType;
                    _finished = next?.Type != CodeTokenType.Terminator || next?.Type != CodeTokenType.ParRoundClose;
                    break;
                case CodeTokenType.LiteralFalse:
                    if (Parent.CompilerLevel != CompilerLevel.Component)
                        throw new CompilerException("Unexpected bool literal at index " + i);
                    if (!Statement.TargetType.CanHold(ClassRef.NumericShortType))
                        throw new CompilerException("Unexpected bool; target type is " + Statement.TargetType);
                    Type = StatementComponentType.Expression;
                    CodeType = BytecodeType.LiteralFalse;
                    Arg = token.Arg!;
                    TargetType = ClassRef.NumericShortType;
                    _finished = next?.Type == CodeTokenType.Dot;
                    TargetType = ClassRef.NumericShortType;
                    _finished = next?.Type != CodeTokenType.Terminator || next?.Type != CodeTokenType.ParRoundClose;
                    break;
                case CodeTokenType.LiteralNull:
                    if (Parent.CompilerLevel != CompilerLevel.Component)
                        throw new CompilerException("Unexpected null literal at index " + i);
                    if (!Statement.TargetType.CanHold(ClassRef.VoidType))
                        throw new CompilerException("Unexpected null; target type is " + Statement.TargetType);
                    Type = StatementComponentType.Expression;
                    CodeType = BytecodeType.Null;
                    Arg = token.Arg!;
                    TargetType = ClassRef.VoidType;
                    //_finished = next?.Type == CodeTokenType.Dot;
                    _finished = next?.Type != CodeTokenType.Terminator || next?.Type != CodeTokenType.ParRoundClose;
                    break;
                case CodeTokenType.OperatorPlus:
                case CodeTokenType.OperatorMinus:
                case CodeTokenType.OperatorMultiply:
                case CodeTokenType.OperatorDivide:
                case CodeTokenType.OperatorModulus:
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
                case CodeTokenType.OperatorEquals:
                    break;
            }

            if (_finished)
            {
                _finishedAction(this);
                return Parent;
            }

            return this;
        }

        private static CodeTokenType[] FirstAllowed(SubCompilerMode mode)
        {
            return mode switch
            {
                SubCompilerMode.ParenthesesSquare => new[] { CodeTokenType.ParSquareOpen },
                SubCompilerMode.ParenthesesAccolade => new[] { CodeTokenType.ParAccOpen },
                SubCompilerMode.ParseTypeParameters => new[] { CodeTokenType.ParDiamondOpen },
                SubCompilerMode.Expression => new[] { CodeTokenType.ParRoundOpen },
                SubCompilerMode.Call => new[] { CodeTokenType.Dot },
                SubCompilerMode.Operator => new[]
                {
                    CodeTokenType.OperatorPlus,
                    CodeTokenType.OperatorMinus,
                    CodeTokenType.OperatorMultiply,
                    CodeTokenType.OperatorDivide,
                    CodeTokenType.OperatorModulus
                },
                _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
            };
        }

        private static CodeTokenType? LastExpected(SubCompilerMode mode)
        {
            return mode switch
            {
                SubCompilerMode.ParenthesesSquare => CodeTokenType.ParSquareClose,
                SubCompilerMode.ParenthesesAccolade => CodeTokenType.ParAccClose,
                SubCompilerMode.ParseTypeParameters => CodeTokenType.ParDiamondClose,
                SubCompilerMode.Expression => CodeTokenType.ParRoundClose,
                _ => null
            };
        }
    }
}