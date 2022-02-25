using KScr.Lib;
using KScr.Lib.Bytecode;
using KScr.Lib.Exception;
using KScr.Lib.Model;

namespace KScr.Compiler.Code
{
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
                    return this;
                case TokenType.IdentNum:
                    CompileDeclaration(ctx, Lib.Bytecode.Class.NumericType);
                    return this;
                case TokenType.IdentStr:
                    CompileDeclaration(ctx, Lib.Bytecode.Class.StringType);
                    return this;
                case TokenType.OperatorEquals:
                    if (ctx.Statement.Type == StatementComponentType.Declaration || ctx.Component.CodeType == BytecodeType.ExpressionVariable)
                    {
                        // assignment
                        ctx.Component = new StatementComponent
                        {
                            Type = StatementComponentType.Setter
                        };

                        ctx.TokenIndex += 1;
                        // compile expression
                        var subctx = new CompilerContext(ctx, CompilerType.CodeExpression);
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
                case TokenType.ParAccClose:
                    _active = false;
                    return this;
            }
            
            return base.AcceptToken(vm, ref ctx);
        }

        private static void CompileDeclaration(CompilerContext ctx, Lib.Bytecode.Class targetType)
        {
            if (ctx.NextToken?.Type != TokenType.Word)
                throw new CompilerException("Invalid declaration; missing variable name");

            ctx.Statement = new Statement
            {
                Type = StatementComponentType.Declaration,
                TargetType = targetType
            };/*
            ctx.Component = new StatementComponent
            {
                Type = StatementComponentType.Declaration,
                Arg = ctx.NextToken!.Arg!
            };*/
        }
    }
}