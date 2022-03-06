using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using KScr.Lib.Bytecode;
using KScr.Lib.Exception;

namespace KScr.Lib.Model
{
    public enum CompilerType
    {
        // class compiler types
        Package = 1, // dir-level compiler
        Class = 2, // file-level compiler
        TypeParameterDefinition = 4,
        ParameterDefintion = 3, // parameterdefinition

        // code compiler types
        CodeStatement = 10, // statement component compiler
        CodeExpression = 11, // expression component compiler
        CodeParameterExpression = 15, // method parameter expression compiler 

        PipeEmitter = 20, // into-pipe emitter <<
        PipeConsumer = 21 // from-pipe consumer >>
    }

    public class TokenContext
    {
        public int TokenIndex;

        protected TokenContext(TokenContext? parent = null)
        {
            Tokens = null!;
            if (parent != null)
                TokenIndex = parent.TokenIndex;
        }

        public TokenContext(IList<IToken> tokens, int tokenIndex = 0)
        {
            Tokens = tokens;
            TokenIndex = tokenIndex;
        }

        public virtual IList<IToken> Tokens { get; }

        public IToken Token => Tokens[TokenIndex];
        public IToken? NextToken => Tokens.Count > TokenIndex + 1 ? Tokens[TokenIndex + 1] : null;

        public IToken? PrevToken => TokenIndex - 1 >= 0 ? Tokens[TokenIndex - 1] : null;

        public int SkipTrailingTokens(TokenType type = TokenType.Whitespace)
        {
            for (var i = 0; Token.Type == type && i < Tokens.Count; i++)
                if (++TokenIndex > Tokens.Count)
                {
                    TokenIndex--;
                    return -1;
                }
                else if (Token.Type != type)
                {
                    return i;
                }

            return -1;
        }

        public string FindCompoundWord(TokenType delimiter = TokenType.Dot, TokenType terminator = TokenType.Terminator)
        {
            var str = "";
            while (Token.Type != terminator && Token.Type == TokenType.Word || Token.Type == delimiter)
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
                do
                {
                    TokenIndex++;
                } while (Token.Type != TokenType.Terminator);
        }
    }

    public sealed class CompilerContext : TokenContext
    {
        public readonly Class Class;
        public readonly ExecutableCode ExecutableCode;
        public readonly Package Package;

        public readonly CompilerContext? Parent;
        public readonly CompilerType Type;
        public int ComponentIndex;
        public int StatementIndex;
        private StatementComponent? _lastComponent;

        public CompilerContext()
            : this(null, CompilerType.Package, null!, Package.RootPackage, null!, null!)
        {
        }

        public CompilerContext(CompilerContext ctx, Package package)
            : this(ctx, CompilerType.Package, ctx.TokenContext, package, null!, null!)
        {
        }

        public CompilerContext(CompilerContext ctx, Class @class, TokenContext tokens,
            [Range(10, 19)] CompilerType type)
            : this(ctx, type, tokens, ctx.Package, @class, null!)
        {
        }

        public CompilerContext(CompilerContext ctx, TokenContext tokens, [Range(10, 19)] CompilerType type)
            : this(ctx, type, tokens, ctx.Package, ctx.Class, new ExecutableCode())
        {
        }

        public CompilerContext(CompilerContext ctx, CompilerType type, bool inheritCode = false)
            : this(ctx, type, ctx.TokenContext, ctx.Package, ctx.Class,
                inheritCode ? ctx.ExecutableCode : new ExecutableCode())
        {
            if (inheritCode)
            {
                StatementIndex = ctx.StatementIndex;
                ComponentIndex = ctx.ComponentIndex;
            }
        }

        private CompilerContext(
            CompilerContext? parent,
            CompilerType type,
            TokenContext tokens,
            Package package,
            Class @class,
            ExecutableCode executableCode,
            int statementIndex = -1,
            int componentIndex = -1) : base(parent)
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

        public override IList<IToken> Tokens => TokenContext.Tokens;
        public TokenContext TokenContext { get; }


        public CompilerContext this[int i1delta, int i2delta] => new(Parent, Type, TokenContext, Package, Class,
            ExecutableCode,
            StatementIndex + i1delta, ComponentIndex + i2delta);

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

        public Statement? NextStatement => ExecutableCode.Main.Count < StatementIndex + 1
            ? ExecutableCode.Main[StatementIndex + 1]
            : null;

        public Statement? PrevStatement => StatementIndex - 1 >= 0 ? ExecutableCode.Main[StatementIndex - 1] : null;

        public StatementComponent Component
        {
            get => Statement.Main[ComponentIndex];
            set
            {
                if (StatementIndex == -1)
                    Statement = new Statement();
                if (NextIntoSub)
                {
                    NextIntoSub = false;
                    value.Statement = Statement;
                    LastComponent.SubComponent = LastComponent = value;
                }
                else
                {
                    (value.Statement = Statement).Main.Add(LastComponent = value);
                    ComponentIndex += 1;
                }
            }
        }

        public StatementComponent? NextComponent =>
            Statement.Main.Count < ComponentIndex + 1 ? Statement.Main[ComponentIndex + 1] : null;

        public StatementComponent? PrevComponent => ComponentIndex - 1 >= 0 ? Statement.Main[ComponentIndex - 1] : null;

        public StatementComponent? LastComponent
        {
            get => _lastComponent ?? Parent?.LastComponent;
            private set => _lastComponent = value;
        }

        public bool NextIntoSub { get; set; }

        public override string ToString()
        {
            return $"CompilerContext<{Type};{PrevToken?.Type},{Token.Type},{NextToken?.Type};{Class.FullName}>";
        }

        public void Clear()
        {
            ExecutableCode.Clear();
        }

        public IClassInstance? FindType(RuntimeBase vm, string name)
        {
            if (Class.Imports.Contains(name))
                return vm.FindType(name);
            var imported = Class.Imports.FirstOrDefault(x => x.EndsWith(name));
            if (imported != null)
                return vm.FindType(imported);
            return vm.FindType(name, Package);
        }

        public ITypeInfo FindFullTypeInfo(RuntimeBase vm, bool _rec = false)
        {
            var type = FindType(vm, Token.Arg!);
            if (type?.Name == Token.Arg && NextToken?.Type != TokenType.ParDiamondOpen)
                return type!;
            var baseCls = type?.BaseClass ?? Class;
            if (baseCls == null && Class.TypeParameters.Any(x => x.Name == Token.Arg))
            { // find base type param
                return Class.TypeParameters.Find(x => x.Name == Token.Arg)!;
            }
            if (baseCls!.TypeParameters.Count > 0 && NextToken?.Type == TokenType.ParDiamondOpen)
            {
                List<ITypeInfo> args = new List<ITypeInfo>();
                do
                {
                    TokenIndex += 2;
                    args.Add(FindFullTypeInfo(vm, true));
                } while (NextToken?.Type == TokenType.Comma);

                TokenIndex += 1;
                return baseCls.CreateInstance(vm, Class, args.ToArray());
            }

            return baseCls.TypeParameters.First(x => x.Name == Token.Arg);
        }
    }

    public interface ICompiler
    {
        ICompiler? Parent { get; }
        bool Active { get; }

        // compile at package level. context is created from root package
        CompilerContext Compile(RuntimeBase vm, DirectoryInfo dir);

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

            vm.CompilePackage(dir, ref context, this);

            return context;
        }

        public abstract ICompiler? AcceptToken(RuntimeBase vm, ref CompilerContext ctx);

        public static Package ResolvePackage(Package inside, string[] names, int i = 0)
        {
            inside = inside.GetOrCreatePackage(names[i]);
            if (i + 1 >= names.Length)
                return inside;
            return ResolvePackage(inside, names, i + 1);
        }

        public static string FindClassPackageName(TokenContext ctx)
        {
            ctx.TokenIndex = 0;
            if (ctx.Token.Type != TokenType.Package)
                throw new CompilerException(ctx.Token.SourcefilePosition, "Missing Package name at index 0");
            ctx.TokenIndex += 1;
            return ctx.FindCompoundWord();
        }

        private static IList<string> FindClassImports(TokenContext ctx)
        {
            // skip package name if necessary
            ctx.SkipPackage();

            var yields = new List<string>();
            while (ctx.Token.Type != TokenType.Terminator && ctx.NextToken?.Type == TokenType.Import)
            {
                ctx.TokenIndex += 2;
                yields.Add(ctx.FindCompoundWord());
            }

            return yields;
        }

        public static ClassInfo FindClassInfo(FileInfo file, ITokenizer tokenizer)
        {
            var tokens = new TokenContext(tokenizer.Tokenize(file.FullName, File.ReadAllText(file.FullName)));
            return FindClassInfo(FindClassPackageName(tokens), tokens, file.Name.Substring(0, file.Name.IndexOf('.')));
        }

        public static ClassInfo FindClassInfo(string findClassPackageName, TokenContext ctx, string? clsName)
        {
            ctx.TokenIndex = 0;
            // skip package and imports if necessary
            string packageName = findClassPackageName;
            ctx.SkipPackage();
            ctx.SkipImports();

            if (ctx.Token.Type == TokenType.Terminator)
                ctx.TokenIndex += 1;

            var mod = ctx.Token.Type.Modifier();
            ClassType? type = ctx.Token.Type.ClassType();
            string name;
            
            ctx.TokenIndex += 1;

            if (ctx.Token.Type == TokenType.Word)
                if (clsName != null && clsName != ctx.Token.Arg)
                    throw new CompilerException(ctx.Token.SourcefilePosition, "Declared Class name mismatches File name");
                else name = ctx.Token.Arg!;
            else throw new CompilerException(ctx.Token.SourcefilePosition, "Missing Class name");

            return new ClassInfo(mod.Value, type.Value, name, packageName + '.' + name);
        }

        public static void CompilerLoop(RuntimeBase vm, ICompiler use, ref CompilerContext context)
        {
            while (context.TokenIndex < context.Tokens.Count && use.Active)
            {
                use = use.AcceptToken(vm, ref context) ?? use.Parent!;
                context.TokenIndex += 1;
            }
        }
    }
}