using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using KScr.Lib.Bytecode;

namespace KScr.Lib.Model
{
    public enum CompilerType
    {
        // class compiler types
        Package, // dir-level compiler
        Class,   // file-level compiler
        
        // code compiler types
        CodeStatement, // statement component compiler
        CodeExpression // expression component compiler
    }
    
    public sealed class CompilerContext
    {
        public CompilerContext(IList<IToken> tokens, Package package) : this(null, CompilerType.Package, tokens, package, null!, null!) {}
        public CompilerContext(CompilerContext ctx, Package package) : this(ctx, CompilerType.Package, ctx.Tokens, package, null!, null!) {}
        public CompilerContext(CompilerContext ctx, Class @class) : this(ctx, CompilerType.Class, ctx.Tokens, ctx.Package, @class, null!) {}
        public CompilerContext(CompilerContext ctx, CompilerType type) : this(ctx, type, ctx.Tokens, ctx.Package, ctx.Class, new ExecutableCode()) {}
        
        private CompilerContext(
            CompilerContext? parent,
            CompilerType type,
            IList<IToken> tokens,
            Package package,
            Class @class,
            ExecutableCode executableCode, 
            int statementIndex = 0, 
            int componentIndex = 0)
        {
            Parent = parent;
            Type = type;
            Tokens = tokens;
            Package = package;
            Class = @class;
            ExecutableCode = executableCode;
            StatementIndex = statementIndex;
            ComponentIndex = componentIndex;
        }

        public CompilerContext this[int i1delta, int i2delta] => new CompilerContext(Parent, Type, Tokens, Package, Class, ExecutableCode,
            StatementIndex + i1delta, ComponentIndex + i2delta);

        public readonly CompilerContext? Parent;
        public readonly CompilerType Type;
        public readonly IList<IToken> Tokens;
        public readonly Package Package;
        public readonly Class Class;
        public readonly ExecutableCode ExecutableCode;
        public int TokenIndex;
        public int StatementIndex;
        public int ComponentIndex;

        public IToken Token => Tokens[TokenIndex]; 
        public IToken? NextToken => Tokens.Count < TokenIndex + 1 ? Tokens[TokenIndex + 1] : null; 
        public IToken? PrevToken => TokenIndex - 1 >= 0 ? Tokens[TokenIndex - 1] : null; 
        
        public Statement Statement
        {
            get => ExecutableCode.Main[StatementIndex];
            set
            {
                ExecutableCode.Main.Add(value);
                StatementIndex += 1;
                ComponentIndex = 0;
            }
        }

        public Statement? NextStatement => ExecutableCode.Main.Count < StatementIndex + 1 ? ExecutableCode.Main[StatementIndex + 1] : null;
        public Statement? PrevStatement => StatementIndex - 1 >= 0 ? ExecutableCode.Main[StatementIndex - 1] : null;
        public StatementComponent Component
        {
            get => Statement.Main[ComponentIndex];
            set
            {
                Statement.Main.Add(value);
                ComponentIndex += 1;
            }
        }

        public StatementComponent? NextComponent => Statement.Main.Count < ComponentIndex + 1 ? Statement.Main[ComponentIndex + 1] : null;
        public StatementComponent? PrevComponent => ComponentIndex - 1 >= 0 ? Statement.Main[ComponentIndex - 1] : null;
    }
    
    public interface ICompiler
    {
        CompilerContext Compile(RuntimeBase vm, IList<IToken> tokens);
        ICompiler? AcceptToken(RuntimeBase vm, ref CompilerContext ctx);
    }

    public abstract class AbstractCompiler : ICompiler
    {
        protected CompilerContext? _context;

        protected AbstractCompiler(ICompiler? parent = null)
        {
            Parent = parent;
        }

        public ICompiler? Parent { get; }

        public CompilerContext Context => _context!;

        public CompilerContext Compile(RuntimeBase vm, IList<IToken> tokens)
        {
            if (_context?.Type != CompilerType.Package)
                _context = new CompilerContext(tokens, Package.RootPackage);
            
            int len = Context.Tokens.Count;
            ICompiler use = this;
            while (Context.TokenIndex < len)
            {
                use = use.AcceptToken(vm, ref _context) ?? Parent!;
                Context.TokenIndex += 1;
            }

            return Context;
        }

        public abstract ICompiler? AcceptToken(RuntimeBase vm, ref CompilerContext ctx);
    }
    
    #region Obsolete
    [Obsolete]
    public enum CompilerLevel
    {
        Statement, // expression, declaration, return, throw, if, while, ...
        Component // parentheses, generic types, ...
    }

    [Obsolete]
    public enum ClassCompilerState
    {
        Idle,
        Package,
        Class
    }

    [Obsolete]
    public interface ICodeCompiler : ICompiler
    {
        public ICodeCompiler? Parent { get; }
        public IStatement<IStatementComponent> Statement { get; }
        public CompilerLevel CompilerLevel { get; }

        public IEvaluable Compile(RuntimeBase runtime);
    }

    [Obsolete]
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
    #endregion
}