using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading;
using KScr.Lib.Bytecode;
using KScr.Lib.Exception;
using Microsoft.VisualBasic;

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

    public class TokenContext
    {
        public virtual IList<IToken> Tokens { get; }
        public int TokenIndex;

        protected TokenContext()
        {
            Tokens = null!;
        }

        public TokenContext(IList<IToken> tokens, int tokenIndex = 0)
        {
            Tokens = tokens;
            TokenIndex = tokenIndex;
        }

        public IToken Token => Tokens[TokenIndex];
        public IToken? NextToken => Tokens.Count > TokenIndex + 1 ? Tokens[TokenIndex + 1] : null;

        public IToken? PrevToken => TokenIndex - 1 >= 0 ? Tokens[TokenIndex - 1] : null;

        public int SkipTrailingTokens(TokenType type = TokenType.Whitespace)
        {
            for (int i = 0; Token.Type == type && i < Tokens.Count; i++)
                if (++TokenIndex > Tokens.Count)
                {
                    TokenIndex--;
                    return -1;
                }
                else if (Token.Type != type)
                    return i;
            return -1;
        }

        public string FindCompoundWord(TokenType delimiter = TokenType.Whitespace)
        {
            string str = "";
            while (Token.Type == TokenType.Word || Token.Type == delimiter)
            {
                str += Token.String();
                TokenIndex += 1;
            }
            return str;
        }

        public void SkipPackage()
        {
            if (Token.Type != TokenType.Package)
                return;
            do
            {
                TokenIndex++;
            } while (Token.Type != TokenType.Terminator);
        }

        public void SkipImports()
        {
            while (NextToken?.Type == TokenType.Import)
            {
                do
                {
                    TokenIndex++;
                } while (Token.Type != TokenType.Terminator);
            }
        }
    }

    public sealed class CompilerContext : TokenContext
    {
        public override IList<IToken> Tokens => TokenContext.Tokens;
        public TokenContext TokenContext { get; }
        public CompilerContext() 
            : this(null, CompilerType.Package, null!, Package.RootPackage, null!, null!) {}
        public CompilerContext(CompilerContext ctx, Package package)
            : this(ctx, CompilerType.Package, ctx.TokenContext, package, null!, null!) {}
        public CompilerContext(CompilerContext ctx, Class @class, TokenContext tokens, [Range(10, 19)] CompilerType type) 
            : this(ctx, type, tokens, ctx.Package, @class, null!) {}
        public CompilerContext(CompilerContext ctx, TokenContext tokens, [Range(10, 19)] CompilerType type) 
            : this(ctx, type, tokens, ctx.Package, ctx.Class, null!) {}
        public CompilerContext(CompilerContext ctx, CompilerType type) 
            : this(ctx, type, ctx.TokenContext, ctx.Package, ctx.Class, new ExecutableCode()) {}

        private CompilerContext(
            CompilerContext? parent,
            CompilerType type,
            TokenContext tokens,
            Package package,
            Class @class,
            ExecutableCode executableCode, 
            int statementIndex = -1, 
            int componentIndex = -1)
        {
            Parent = parent;
            Type = type;
            TokenContext = tokens;
            Package = package;
            Class = @class;
            ExecutableCode = executableCode;
            StatementIndex = statementIndex;
            ComponentIndex = componentIndex;
        }


        public CompilerContext this[int i1delta, int i2delta] => new CompilerContext(Parent, Type, TokenContext, Package, Class, ExecutableCode,
            StatementIndex + i1delta, ComponentIndex + i2delta);

        public readonly CompilerContext? Parent;
        public readonly CompilerType Type;
        public readonly Package Package;
        public readonly Class Class;
        public readonly ExecutableCode ExecutableCode;
        public int StatementIndex;
        public int ComponentIndex;

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

        public override string ToString()
        {
            return $"CompilerContext<{Type};{PrevToken?.Type},{Token.Type},{NextToken?.Type};{Class.FullName}>";
        }
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
            var token = new TokenContext(tokens);
            string clsName = file.Name.Substring(0, file.Name.Length - FileAppendix.Length);
            var pkg = context.Package;
            pkg = pkg.GetOrCreatePackage(FindClassPackageName(token));
            var cls = pkg.GetOrCreateClass(clsName, FindClassHeader(token, clsName), context.Package);
            var prev = context;
            context = new CompilerContext(context, cls, token, CompilerType.Class);
            CompilerLoop(vm, vm.Compiler, ref context);
            context = prev;
        }

        private string FindClassPackageName(TokenContext ctx)
        {
            ctx.TokenIndex = 0;
            if (ctx.Token.Type != TokenType.Package)
                throw new CompilerException("Missing Package name at index 0");
            ctx.TokenIndex += 2;
            return ctx.FindCompoundWord(TokenType.Dot);
        }

        private static IList<string> FindClassImports(TokenContext ctx)
        {
            // skip package name if necessary
            ctx.SkipPackage();
            
            var yields = new List<string>();
            while (ctx.Token.Type != TokenType.Terminator && ctx.NextToken?.Type == TokenType.Import)
            {
                // todo
            }

            return yields;
        }

        private static MemberModifier FindClassHeader(TokenContext ctx, string clsName)
        {
            // skip package and imports if necessary
            ctx.SkipPackage();
            ctx.SkipImports();
            
            string name = "";
            return FindClassHeader(ctx, clsName, ref name);
        }

        private static MemberModifier FindClassHeader(TokenContext ctx, string? clsName, ref string name)
        {
            var mod = MemberModifier.Protected;
            
            // fixme todo!!!

            if (ctx.Token.Type != TokenType.Word)
                if (clsName != null && clsName != ctx.Token.Arg)
                    throw new CompilerException("Declared Class name mismatches File name");
                else name = ctx.Token.Arg!;
            else throw new CompilerException("Missing Class name");
            return mod;
        }

        public CompilerContext Compile(RuntimeBase vm, CompilerContext context, IList<IToken> tokens)
        {
            var token = new TokenContext(tokens);
            Package pkg = Package.RootPackage.GetOrCreatePackage(FindClassPackageName(token));
            string cname = "";
            MemberModifier mod = FindClassHeader(token, null, ref cname);
            Class cls = pkg.GetOrCreateClass(cname, mod, context.Package);
            context = new CompilerContext(context, cls, token, context.Type);
            CompilerLoop(vm, this, ref context);
            return context;
        }

        protected static void CompilerLoop(RuntimeBase vm, ICompiler use, ref CompilerContext context)
        {
            while (context.TokenIndex < context.Tokens.Count && use.Active)
            {
                use = use.AcceptToken(vm, ref context) ?? use.Parent!;
                context.TokenIndex += 1;
            }
        }

        public abstract ICompiler? AcceptToken(RuntimeBase vm, ref CompilerContext ctx);
    }
}