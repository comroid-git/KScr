using System.Linq;
using KScr.Lib;
using KScr.Lib.Bytecode;
using KScr.Lib.Core;
using KScr.Lib.Exception;
using KScr.Lib.Model;

namespace KScr.Compiler.Code
{
    public class ExpressionCompiler : AbstractCodeCompiler
    {
        private readonly bool _endBeforeTerminator;
        private readonly TokenType[] _terminators;

        public ExpressionCompiler(ICompiler parent, bool endBeforeTerminator = false, params TokenType[] terminators) :
            base(parent)
        {
            _endBeforeTerminator = endBeforeTerminator;
            _terminators = terminators;
        }

        public ExpressionCompiler(ICompiler parent, bool endBeforeTerminator = false,
            TokenType terminator = TokenType.Terminator)
            : this(parent, endBeforeTerminator, new[] { terminator })
        {
        }

        public override ICompiler? AcceptToken(RuntimeBase vm, ref CompilerContext ctx)
        {
            switch (ctx.Token.Type)
            {
                case TokenType.ParRoundOpen:
                    ctx.Component = new StatementComponent
                    {
                        Type = StatementComponentType.Expression,
                        CodeType = BytecodeType.Parentheses
                    };

                    // compile inner expression
                    var subctx = new CompilerContext(ctx, CompilerType.CodeExpression);
                    subctx.TokenIndex += 1;
                    subctx.Statement = new Statement
                    {
                        Type = StatementComponentType.Expression,
                        TargetType = ctx.Statement.TargetType
                    };
                    CompilerLoop(vm, new ExpressionCompiler(this), ref subctx);
                    ctx.LastComponent!.SubStatement = subctx.Statement;
                    ctx.TokenIndex = subctx.TokenIndex - 1;
                    // finished
                    _active = false;
                    return this;
                case TokenType.LiteralNull:
                    ctx.Component = new StatementComponent
                    {
                        Type = StatementComponentType.Expression,
                        CodeType = BytecodeType.Null
                    };
                    break;
                case TokenType.LiteralNum:
                    if (!ctx.Statement.TargetType.CanHold(Lib.Bytecode.Class.NumericType))
                        throw new CompilerException("Invalid Numeric literal; expected " + ctx.Statement.TargetType);
                    var numstr = Numeric.Compile(vm, ctx.Token.Arg!).Value!.ToString(IObject.ToString_LongName);
                    ctx.Component = new StatementComponent
                    {
                        Type = StatementComponentType.Expression,
                        CodeType = BytecodeType.LiteralNumeric,
                        Arg = numstr
                    };
                    break;
                case TokenType.LiteralStr:
                    if (!ctx.Statement.TargetType.CanHold(Lib.Bytecode.Class.StringType))
                        throw new CompilerException("Invalid Numeric literal; expected " + ctx.Statement.TargetType);
                    ctx.Component = new StatementComponent
                    {
                        Type = StatementComponentType.Expression,
                        CodeType = BytecodeType.LiteralString,
                        Arg = ctx.Token.Arg!
                    };
                    break;
                case TokenType.LiteralTrue:
                    if (!ctx.Statement.TargetType.CanHold(Lib.Bytecode.Class.NumericType))
                        throw new CompilerException("Invalid Numeric literal; expected " + ctx.Statement.TargetType);
                    ctx.Component = new StatementComponent
                    {
                        Type = StatementComponentType.Expression,
                        CodeType = BytecodeType.LiteralTrue
                    };
                    break;
                case TokenType.LiteralFalse:
                    if (!ctx.Statement.TargetType.CanHold(Lib.Bytecode.Class.NumericType))
                        throw new CompilerException("Invalid Numeric literal; expected " + ctx.Statement.TargetType);
                    ctx.Component = new StatementComponent
                    {
                        Type = StatementComponentType.Expression,
                        CodeType = BytecodeType.LiteralFalse
                    };
                    break;
            }

            var use = base.AcceptToken(vm, ref ctx);
            if (_terminators.Contains(ctx.NextToken?.Type ?? TokenType.Terminator))
            {
                if (_endBeforeTerminator)
                    ctx.TokenIndex -= 1;
                _active = false;
                return _endBeforeTerminator ? Parent : this;
            }

            return use;
        }
    }
}