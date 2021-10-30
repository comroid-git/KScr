using System.Collections.Generic;
using System.IO;
using KScr.Lib.Bytecode;

namespace KScr.Lib.Model
{
    public interface ICompiler
    {
        ICompiler AcceptToken(RuntimeBase vm, IList<IToken> tokens, ref int i);
    }

    public enum CompilerLevel
    {
        Statement, // expression, declaration, return, throw, if, while, ...
        Component // parentheses, generic types, ...
    }

    public interface ICodeCompiler : ICompiler
    {
        public ICodeCompiler? Parent { get; }
        public IStatement<IStatementComponent> Statement { get; }
        public CompilerLevel CompilerLevel { get; }
        public IEvaluable Compile(RuntimeBase runtime, IList<CodeToken> tokens);

        public IEvaluable Compile(RuntimeBase runtime);
    }

    public enum ClassCompilerState
    {
        Idle,
        Package,
        Class
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
}