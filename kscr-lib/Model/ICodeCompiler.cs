using System.Collections.Generic;

namespace KScr.Lib.Model
{
    public enum CompilerLevel
    {
        Statement, // expression, declaration, return, throw, if, while, ...
        Component // parentheses, generic types, ...
    }

    public interface ICodeCompiler
    {
        public ICodeCompiler? Parent { get; }
        public IStatement<IStatementComponent> Statement { get; }
        public CompilerLevel CompilerLevel { get; }
        public IEvaluable Compile(RuntimeBase runtime, IList<CodeToken> tokens);

        public ICodeCompiler AcceptToken(RuntimeBase vm, IList<CodeToken> tokens, ref int i);
        public IEvaluable Compile(RuntimeBase runtime);
    }
}