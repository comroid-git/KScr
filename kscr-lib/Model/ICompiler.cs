using System;
using System.Collections.Generic;

namespace KScr.Lib.Model
{
    public enum CompilerLevel
    {
        Statement, // expression, declaration, return, throw, if, while, ...
        Component // parentheses, generic types, ...
    }

    public interface ICompiler
    {
        public ICompiler? Parent { get; }
        public IStatement<IStatementComponent> Statement { get; }
        public CompilerLevel CompilerLevel { get; }
        public IEvaluable Compile(RuntimeBase runtime, IList<Token> tokens);

        public ICompiler AcceptToken(RuntimeBase vm, IList<Token> tokens, ref int i);
        public IEvaluable Compile(RuntimeBase runtime);
    }
}