using System.Linq;
using KScr.Lib;
using KScr.Lib.Bytecode;
using KScr.Lib.Exception;
using KScr.Lib.Model;

namespace KScr.Compiler.Code
{
    public class StatementCompiler : AbstractCodeCompiler
    {
        private readonly bool _endBeforeTerminator;
        private readonly TokenType[] _terminators;

        public StatementCompiler() : this(null!)
        {
        }

        public StatementCompiler(ICompiler parent, bool endBeforeTerminator = false, params TokenType[] terminators) : base(parent)
        {
            _endBeforeTerminator = endBeforeTerminator;
            _terminators = terminators;
        }

        public override ICompiler? AcceptToken(RuntimeBase vm, ref CompilerContext ctx)
        {
            CompilerContext? subctx;
            switch (ctx.Token.Type)
            {
                case TokenType.OperatorEquals:
                    if (ctx.Statement.Type == StatementComponentType.Declaration ||
                        ctx.Component.CodeType == BytecodeType.ExpressionVariable)
                    {
                        // assignment
                        ctx.Component = new StatementComponent
                        {
                            Type = StatementComponentType.Setter
                        };

                        ctx.TokenIndex += 1;
                        // compile expression
                        subctx = new CompilerContext(ctx, CompilerType.CodeExpression);
                        subctx.Statement = new Statement
                        {
                            Type = StatementComponentType.Expression,
                            TargetType = ctx.Statement.TargetType
                        };
                        CompilerLoop(vm, new ExpressionCompiler(this), ref subctx);
                        ctx.LastComponent!.SubStatement = subctx.Statement;
                        ctx.TokenIndex = subctx.TokenIndex - 1;
                    }

                    return this;
                case TokenType.If:
                    if (ctx.NextToken?.Type != TokenType.ParRoundOpen)
                        throw new CompilerException("Invalid if-Statement; missing parentheses");
                    if (ctx.Statement != null && ctx.Statement.Main.Count > 0)
                        throw new CompilerException("Invalid if-Statement; must be first statement");
                    if (ctx.Statement == null)
                        ctx.Statement = new Statement
                        {
                            Type = StatementComponentType.Code,
                            CodeType = BytecodeType.StmtIf
                        };
                    ctx.Component = new StatementComponent
                    {
                        Type = StatementComponentType.Code,
                        CodeType = BytecodeType.StmtIf
                    };
                    
                    ctx.TokenIndex += 2;
                    // parse condition
                    subctx = new CompilerContext(ctx, CompilerType.CodeExpression);
                    subctx.Statement = new Statement
                    {
                        Type = StatementComponentType.Code,
                        CodeType = BytecodeType.StmtIfCond
                    };
                    CompilerLoop(vm, new ExpressionCompiler(this, false, 
                        TokenType.ParRoundClose), ref subctx);
                    ctx.LastComponent!.SubStatement = subctx.Statement;
                    ctx.TokenIndex = subctx.TokenIndex;
                    
                    ctx.TokenIndex += 1;
                    // parse body
                    subctx = new CompilerContext(ctx, CompilerType.CodeStatement);
                    CompilerLoop(vm, new StatementCompiler(this, false,
                        ctx.Token.Type == TokenType.ParAccOpen ? TokenType.ParAccClose : TokenType.Terminator), ref subctx);
                    ctx.LastComponent!.InnerCode = subctx.ExecutableCode;
                    ctx.TokenIndex = subctx.TokenIndex;
                    return this;
                case TokenType.Else:
                    if (ctx.LastComponent?.CodeType != BytecodeType.StmtIf)
                        throw new CompilerException("Invalid else-Statement; missing if Statement");
                    ctx.TokenIndex += 1;
                    // parse body
                    subctx = new CompilerContext(ctx, CompilerType.CodeStatement);
                    CompilerLoop(vm, new StatementCompiler(this, false,
                        ctx.Token.Type == TokenType.ParAccOpen ? TokenType.ParAccClose : TokenType.Terminator), ref subctx);
                    ctx.LastComponent!.SubComponent = new StatementComponent
                    {
                        Type = StatementComponentType.Code,
                        CodeType = BytecodeType.StmtElse,
                        InnerCode = subctx.ExecutableCode
                    };
                    ctx.TokenIndex = subctx.TokenIndex;
                    return this;
                case TokenType.ParAccClose:
                    _active = false;
                    return this;
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