using System.Collections.Generic;
using System.IO;
using KScr.Lib.Bytecode;

namespace KScr.Lib.Model
{
    public interface ICompiler
    {
        IRuntimeSite Compile(RuntimeBase vm, IList<IToken> tokens);
        ICompiler AcceptToken(RuntimeBase vm, IToken token, IToken? next, IToken? prev, ref int i);
        IRuntimeSite Compile(RuntimeBase vm);
    }

    public abstract class AbstractCompiler : ICompiler
    {
        public IRuntimeSite Compile(RuntimeBase vm, IList<IToken> tokens) {
            int len = tokens.Count;
            ICompiler use = this;
            for (var i = 0; i < len; i++)
            {
                var token = tokens[i];
                var next = i + 1 >= tokens.Count ? null : tokens[i + 1];
                var prev = i - 1 < 0 ? null : tokens[i - 1];
                use = use.AcceptToken(vm, token, next, prev, ref i);
            }

            return use.Compile(vm);
        }

        public abstract ICompiler AcceptToken(RuntimeBase vm, IToken token, IToken? next, IToken? prev, ref int i);

        public abstract IRuntimeSite Compile(RuntimeBase vm);
    }

    public interface ICodeCompiler : ICompiler
    {
        public ICodeCompiler? Parent { get; }
        public IStatement<IStatementComponent> Statement { get; }
        public CompilerLevel CompilerLevel { get; }
        public IEvaluable Compile(RuntimeBase runtime, IList<IToken> tokens);

        public IEvaluable Compile(RuntimeBase runtime);
    }

    public interface IClassCompiler : ICompiler
    {
        public ClassCompilerState State { get; }
        public Package CompilePackage(RuntimeBase vm, DirectoryInfo dir);
        public void CompileClasses(RuntimeBase vm, DirectoryInfo dir);
        public Class CompileClass(RuntimeBase vm, FileInfo file);
        public IClassCompiler NextPackage(string name);
        public IClassCompiler NextClass(string name);
        public IClassCompiler PushElement();
    }

    public enum CompilerLevel
    {
        Statement, // expression, declaration, return, throw, if, while, ...
        Component // parentheses, generic types, ...
    }

    public enum ClassCompilerState
    {
        Idle,
        Package,
        Class
    }
}