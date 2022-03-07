﻿using System;
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
                case TokenType.Return:
                case TokenType.Throw:
                    bool isReturn = ctx.Token.Type == TokenType.Return;
                    ctx.Statement = new Statement
                    {
                        Type = StatementComponentType.Code,
                        CodeType = isReturn ? BytecodeType.Return : BytecodeType.Throw,
                        TargetType = isReturn 
                            ? Lib.Bytecode.Class.VoidType.DefaultInstance 
                            : Lib.Bytecode.Class.ThrowableType.DefaultInstance
                    };

                    // compile exception expression
                    ctx.TokenIndex += 1;
                    subctx = new CompilerContext(ctx, CompilerType.CodeExpression);
                    subctx.Statement = new Statement
                    {
                        Type = StatementComponentType.Expression,
                        CodeType = isReturn ? BytecodeType.Return : BytecodeType.Throw,
                        TargetType = isReturn 
                            ? Lib.Bytecode.Class.VoidType.DefaultInstance 
                            : Lib.Bytecode.Class.ThrowableType.DefaultInstance
                    };
                    CompilerLoop(vm, new ExpressionCompiler(this), ref subctx);
                    ctx.Component = new StatementComponent
                    {
                        Type = StatementComponentType.Code,
                        CodeType = isReturn ? BytecodeType.Return : BytecodeType.Throw,
                        SubStatement = subctx.Statement,
                        SourcefilePosition = ctx.Token.SourcefilePosition
                    };
                    ctx.TokenIndex = subctx.TokenIndex - 1;
                    break;
                case TokenType.OperatorEquals:
                    if (ctx.Statement.Type == StatementComponentType.Declaration ||
                        ctx.Component.CodeType == BytecodeType.ExpressionVariable)
                    {
                        // assignment
                        ctx.Component = new StatementComponent
                        {
                            Type = StatementComponentType.Setter,
                            SourcefilePosition = ctx.Token.SourcefilePosition
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
                        throw new CompilerException(ctx.Token.SourcefilePosition, "Invalid if-Statement; missing condition");
                    ctx.Statement = new Statement
                    {
                        Type = StatementComponentType.Code,
                        CodeType = BytecodeType.StmtIf
                    };
                    ctx.Component = new StatementComponent
                    {
                        Type = StatementComponentType.Code,
                        CodeType = BytecodeType.StmtIf,
                        SourcefilePosition = ctx.Token.SourcefilePosition
                    };
                    
                    ctx.TokenIndex += 2;
                    // parse condition
                    subctx = new CompilerContext(ctx, CompilerType.CodeExpression);
                    subctx.Statement = new Statement
                    {
                        Type = StatementComponentType.Code,
                        CodeType = BytecodeType.StmtCond
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
                        throw new CompilerException(ctx.Token.SourcefilePosition, "Invalid else-Statement; missing if Statement");
                    ctx.TokenIndex += 1;
                    // parse body
                    subctx = new CompilerContext(ctx, CompilerType.CodeStatement);
                    CompilerLoop(vm, new StatementCompiler(this, false,
                        ctx.Token.Type == TokenType.ParAccOpen ? TokenType.ParAccClose : TokenType.Terminator), ref subctx);
                    ctx.LastComponent!.SubComponent = new StatementComponent
                    {
                        Type = StatementComponentType.Code,
                        CodeType = BytecodeType.StmtElse,
                        InnerCode = subctx.ExecutableCode,
                        SourcefilePosition = ctx.Token.SourcefilePosition
                    };
                    ctx.TokenIndex = subctx.TokenIndex;
                    return this;
                case TokenType.For:
                    if (ctx.NextToken?.Type != TokenType.ParRoundOpen)
                        throw new CompilerException(ctx.Token.SourcefilePosition, "Invalid for-Statement; missing specification");
                    ctx.Statement = new Statement
                    {
                        Type = StatementComponentType.Code,
                        CodeType = BytecodeType.StmtFor
                    };
                    ctx.Component = new StatementComponent
                    {
                        Type = StatementComponentType.Code,
                        CodeType = BytecodeType.StmtFor,
                        SourcefilePosition = ctx.Token.SourcefilePosition
                    };

                    // parse start statement
                    ctx.TokenIndex += 2;
                    subctx = new CompilerContext(ctx, CompilerType.CodeStatement);
                    CompilerLoop(vm, new StatementCompiler(this, false, TokenType.Terminator), ref subctx);
                    ctx.LastComponent!.SubStatement = subctx.Statement;
                    ctx.TokenIndex = subctx.TokenIndex;

                    // parse continue-check
                    subctx = new CompilerContext(ctx, CompilerType.CodeExpression);
                    subctx.Statement = new Statement
                    {
                        Type = StatementComponentType.Code,
                        CodeType = BytecodeType.StmtCond,
                        TargetType = Lib.Bytecode.Class.NumericType.DefaultInstance
                    };
                    CompilerLoop(vm, new ExpressionCompiler(this), ref subctx);
                    ctx.LastComponent!.SubComponent = subctx.Component;
                    ctx.TokenIndex = subctx.TokenIndex + 1;
                    
                    // parse accumulator
                    subctx = new CompilerContext(ctx, CompilerType.CodeStatement);
                    subctx.Statement = new Statement();
                    CompilerLoop(vm, new StatementCompiler(this, false, TokenType.ParRoundClose), ref subctx);
                    ctx.LastComponent!.AltStatement = subctx.Statement;
                    ctx.TokenIndex = subctx.TokenIndex + 1;

                    // parse body
                    subctx = new CompilerContext(ctx, CompilerType.CodeStatement);
                    CompilerLoop(vm, new StatementCompiler(this, false,
                        ctx.Token.Type == TokenType.ParAccOpen ? TokenType.ParAccClose : TokenType.Terminator), ref subctx);
                    ctx.LastComponent!.InnerCode = subctx.ExecutableCode;
                    ctx.TokenIndex = subctx.TokenIndex;
                    return this;
                case TokenType.ForEach:
                    if (ctx.NextToken?.Type != TokenType.ParRoundOpen)
                        throw new CompilerException(ctx.Token.SourcefilePosition, "Invalid foreach-Statement; missing specification");
                    ctx.Statement = new Statement
                    {
                        Type = StatementComponentType.Code,
                        CodeType = BytecodeType.StmtForEach
                    };
                    ctx.Component = new StatementComponent
                    {
                        Type = StatementComponentType.Code,
                        CodeType = BytecodeType.StmtForEach,
                        SourcefilePosition = ctx.Token.SourcefilePosition
                    };
                    
                    ctx.TokenIndex += 2;
                    // parse n's name
                    if (ctx.Token.Type != TokenType.Word)
                        throw new CompilerException(ctx.Token.SourcefilePosition, "Invalid foreach-Statement; missing n Identifier");
                    ctx.LastComponent!.Arg = ctx.Token.Arg!;

                    // parse range
                    ctx.TokenIndex += 2;
                    if (ctx.PrevToken!.Type != TokenType.Colon)
                        throw new CompilerException(ctx.Token.SourcefilePosition, "Invalid foreach-Statement; missing delimiter colon");
                    subctx = new CompilerContext(ctx, CompilerType.CodeExpression);
                    subctx.Statement = new Statement
                    {
                        Type = StatementComponentType.Code,
                        CodeType = BytecodeType.Expression,
                        TargetType = Lib.Bytecode.Class.IterableType.DefaultInstance
                    };
                    CompilerLoop(vm, new ExpressionCompiler(this, false, TokenType.ParRoundClose), ref subctx);
                    ctx.LastComponent!.SubStatement = subctx.Statement;
                    ctx.TokenIndex = subctx.TokenIndex;

                    // parse body
                    ctx.TokenIndex += 1;
                    subctx = new CompilerContext(ctx, CompilerType.CodeStatement);
                    CompilerLoop(vm, new StatementCompiler(this, false,
                        ctx.Token.Type == TokenType.ParAccOpen ? TokenType.ParAccClose : TokenType.Terminator), ref subctx);
                    ctx.LastComponent!.InnerCode = subctx.ExecutableCode;
                    ctx.TokenIndex = subctx.TokenIndex;
                    return this;
                case TokenType.ParAccClose:
                    _active = false;
                    return this;
            }
            
            var use = base.AcceptToken(vm, ref ctx);
            if (_terminators.Contains(ctx.Token.Type) 
                || _terminators.Contains(ctx.NextToken?.Type ?? TokenType.Terminator))
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