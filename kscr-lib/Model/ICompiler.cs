using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading;
using KScr.Lib.Bytecode;
using KScr.Lib.Exception;

namespace KScr.Lib.Model
{
    public enum CompilerType
    {
        // class compiler types
        Package = 1, // dir-level compiler
        Class = 2,   // file-level compiler
        ParameterDefintion = 3, // parameterdefinition
        
        // code compiler types
        CodeStatement = 10, // statement component compiler
        CodeExpression = 11, // expression component compiler
        CodeParameterExpression = 15 // method parameter expression compiler 
    }
    
    public sealed class CompilerContext
    {
        public CompilerContext() : this(null, CompilerType.Package, null!, Package.RootPackage, null!, null!) {}
        public CompilerContext(CompilerContext ctx, Package package) : this(ctx, CompilerType.Package, ctx.Tokens, package, null!, null!) {}
        public CompilerContext(CompilerContext ctx, Class @class, IList<IToken> tokens, [Range(10, 19)] CompilerType type) : this(ctx, type, tokens, ctx.Package, @class, null!) {}
        public CompilerContext(CompilerContext ctx, IList<IToken> tokens, [Range(10, 19)] CompilerType type) : this(ctx, type, tokens, ctx.Package, ctx.Class, null!) {}
        public CompilerContext(CompilerContext ctx, CompilerType type) : this(ctx, type, ctx.Tokens, ctx.Package, ctx.Class, new ExecutableCode()) {}

        private CompilerContext(
            CompilerContext? parent,
            CompilerType type,
            IList<IToken> tokens,
            Package package,
            Class @class,
            ExecutableCode executableCode, 
            int statementIndex = -1, 
            int componentIndex = -1)
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
                ComponentIndex = -1;
            }
        }
        public Statement? NextStatement => ExecutableCode.Main.Count < StatementIndex + 1 ? ExecutableCode.Main[StatementIndex + 1] : null;
        public Statement? PrevStatement => StatementIndex - 1 >= 0 ? ExecutableCode.Main[StatementIndex - 1] : null;
        
        public StatementComponent Component
        {
            get => Statement.Main[ComponentIndex];
            set
            {
                if (StatementIndex == -1)
                    Statement = new Statement();
                (value.Statement = Statement).Main.Add(value);
                ComponentIndex += 1;
            }
        }
        public StatementComponent? NextComponent => Statement.Main.Count < ComponentIndex + 1 ? Statement.Main[ComponentIndex + 1] : null;
        public StatementComponent? PrevComponent => ComponentIndex - 1 >= 0 ? Statement.Main[ComponentIndex - 1] : null;
    }
    
    public interface ICompiler
    {
        ICompiler? Parent { get; }
        bool Active { get; }

        // compile at package level. context is created from root package
        CompilerContext Compile(RuntimeBase vm, DirectoryInfo dir);
        // compile at class level. context type must be class-level
        CompilerContext Compile(RuntimeBase vm, CompilerContext context, IList<IToken> tokens);
        ICompiler? AcceptToken(RuntimeBase vm, ref CompilerContext context);
    }

    public abstract class AbstractCompiler : ICompiler
    {
        public const string FileAppendix = ".kscr";
        
        protected AbstractCompiler(ICompiler? parent = null)
        {
            Parent = parent;
        }

        public ICompiler? Parent { get; }
        public virtual bool Active => true;

        public CompilerContext Compile(RuntimeBase vm, DirectoryInfo dir)
        {
            var context = new CompilerContext();

            CompilePackage(vm, dir, ref context);
            
            return context;
        }

        private void CompilePackage(RuntimeBase vm, DirectoryInfo dir, ref CompilerContext context)
        {
            foreach (var subDir in dir.EnumerateDirectories())
            {
                var pkg = new Package(context.Package, subDir.Name);
                var prev = context;
                context = new CompilerContext(context, pkg);
                CompilePackage(vm, subDir, ref context);
                context = prev;
            }

            foreach (var subFile in dir.EnumerateFiles('*'+FileAppendix)) 
                CompileClass(vm, subFile, ref context);
        }

        private void CompileClass(RuntimeBase vm, FileInfo file, ref CompilerContext context)
        {
            var source = File.ReadAllText(file.FullName);
            var tokens = vm.Tokenizer.Tokenize(vm, source ?? throw new FileNotFoundException("Source file not found: " + file.FullName));
            string clsName = file.Name.Substring(0, file.Name.Length - FileAppendix.Length);
            var cls = context.Package.GetOrCreateClass(clsName, FindClassModifiers(tokens, clsName, ref context.TokenIndex), context.Package);
            var prev = context;
            context = new CompilerContext(context, cls, tokens, CompilerType.Class);
            ICompiler use = vm.Compiler;
            CompilerLoop(vm, ref use, ref context);
            context = prev;
        }

        private static MemberModifier FindClassModifiers(IList<IToken> tokens, string clsName, ref int i)
        {
            var mod = MemberModifier.Protected;

            switch (tokens[i].Type)
            {
                case TokenType.Public:
                    mod = MemberModifier.Public;
                    i+=1;
                    break;
                case TokenType.Internal:
                    mod = MemberModifier.Internal;
                    i+=1;
                    break;
                case TokenType.Private:
                    mod = MemberModifier.Private;
                    i+=1;
                    break;
            }

            if (tokens[i].Type == TokenType.Static)
            {
                mod |= MemberModifier.Static;
                i+=1;
            }

            switch (tokens[i].Type)
            {
                case TokenType.Final:
                    mod |= MemberModifier.Final;
                    i+=1;
                    break;
                case TokenType.Abstract:
                    mod |= MemberModifier.Abstract;
                    i+=1;
                    break;
            }

            if (tokens[i].Type != TokenType.Word || tokens[i].Arg != clsName)
                throw new CompilerException("Declared Class name was not found or mismatches File name");

            return mod;
        }

        public CompilerContext Compile(RuntimeBase vm, CompilerContext context, IList<IToken> tokens)
        {
            ICompiler use = this;
            context = new CompilerContext(context, tokens, context.Type);
            CompilerLoop(vm, ref use, ref context);
            return context;
        }

        protected static void CompilerLoop(RuntimeBase vm, ref ICompiler use, ref CompilerContext context)
        {
            while (context.TokenIndex < context.Tokens.Count && use.Active)
            {
                use = use.AcceptToken(vm, ref context) ?? use.Parent!;
                context.TokenIndex += 1;
            }
        }

        protected static string FindCompoundWord(CompilerContext ctx, TokenType delimiter = TokenType.Whitespace)
        {
            string str = "";
            while (ctx.Token.Type != delimiter)
            {
                str += ctx.Token.String();
                ctx.TokenIndex += 1;
            }
            return str;
        }

        public abstract ICompiler? AcceptToken(RuntimeBase vm, ref CompilerContext ctx);
    }
}