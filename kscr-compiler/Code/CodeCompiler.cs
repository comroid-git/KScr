using KScr.Lib;
using KScr.Lib.Bytecode;
using KScr.Lib.Exception;
using KScr.Lib.Model;

namespace KScr.Compiler.Code
{
    public abstract class AbstractCodeCompiler : AbstractCompiler
    {
        protected bool _active = true;

        public override bool Active => _active;

        protected AbstractCodeCompiler(ICompiler parent) : base(parent)
        {
        }
    }
    
    public class StatementCompiler : AbstractCodeCompiler
    {
        public StatementCompiler(ICompiler parent) : base(parent)
        {
        }

        public override ICompiler? AcceptToken(RuntimeBase vm, ref CompilerContext ctx)
        {
            switch (ctx.Token.Type)
            {
                case TokenType.IdentVoid:
                    CompileDeclaration(ctx, Lib.Bytecode.Class.VoidType);
                    break;
                case TokenType.IdentNum:
                    CompileDeclaration(ctx, Lib.Bytecode.Class.NumericType);
                    break;
                case TokenType.IdentStr:
                    CompileDeclaration(ctx, Lib.Bytecode.Class.StringType);
                    break;
                case TokenType.OperatorEquals:
                    if (ctx.Statement.Type == StatementComponentType.Declaration)
                    {
                        // assignment
                        ctx.Component = new StatementComponent
                        {
                            Type = StatementComponentType.Consumer,
                            CodeType = BytecodeType.Assignment
                        };
                    
                        // compile expression
                        ICompiler sub = new ExpressionCompiler(this);
                        var subctx = new CompilerContext(ctx, CompilerType.CodeExpression);
                        CompilerLoop(vm, ref sub, ref subctx);
                        ctx.Component.SubComponent = subctx.Component;
                    }

                    break;
                case TokenType.ParAccClose:
                    _active = false;
                    return Parent;
            }

            return this;
        }

        private static void CompileDeclaration(CompilerContext ctx, Lib.Bytecode.Class targetType)
        {
            if (ctx.NextToken?.Type != TokenType.Word)
                throw new CompilerException("Invalid declaration; missing variable name");

            ctx.Statement = new Statement
            {
                Type = StatementComponentType.Declaration,
                TargetType = targetType
            };
            ctx.Component = new StatementComponent
            {
                Arg = ctx.NextToken!.Arg!
            };
            ctx.TokenIndex += 1;
        }
    }
    
    public class ExpressionCompiler : AbstractCodeCompiler
    {
        private enum Mode
        {
            Parentheses
        }
        
        public ExpressionCompiler(ICompiler parent) : base(parent)
        {
        }

        public override ICompiler? AcceptToken(RuntimeBase vm, ref CompilerContext ctx)
        {
            switch (ctx.Token.Type)
            {
                case TokenType.Terminator:
                    // todo finalize & push expression
                    _active = false;
                    return Parent;
            }

            return this;
        }
    }
}