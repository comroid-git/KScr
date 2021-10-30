using System;
using System.Collections.Generic;
using System.IO;
using KScr.Lib;
using KScr.Lib.Bytecode;
using KScr.Lib.Exception;
using KScr.Lib.Model;

namespace KScr.Eval
{
    public sealed class ClassCompiler : AbstractCompiler, IClassCompiler
    {
        public readonly IClassCompiler? Parent;
        private Class _class = null!;
        private Package _package = Package.RootPackage;
        private MemberModifier Modifier = MemberModifier.None;

        public ClassCompiler(IClassCompiler? parent = null)
        {
            Parent = parent;
        }

        public ClassCompilerState State { get; private set; } = ClassCompilerState.Idle;

        public Package CompilePackage(RuntimeBase vm, DirectoryInfo dir)
        {
            IClassCompiler use = this;

            // compile package metadata
            // todo

            // compile directories as subpackages
            foreach (var subp in dir.GetDirectories())
            {
                _package.Add((use = use.NextPackage(subp.Name)).CompilePackage(vm, subp));
                use = use.PushElement();
            }

            // compile code if not in root namespace
            if (!_package.IsRoot) // no classes allowed in Root directory
                CompileClasses(vm, dir);
            return _package;
        }

        public void CompileClasses(RuntimeBase vm, DirectoryInfo dir)
        {
            IClassCompiler use = this;

            foreach (var clsf in dir.GetFiles("*.kscr"))
            {
                _package.Add((use = use.NextClass(clsf.Name)).CompileClass(vm, clsf));
                use = use.PushElement();
            }
        }

        public Class CompileClass(RuntimeBase vm, FileInfo file)
        {
            if (_class == null)
                throw new Exception("unexpected");
            string src = File.ReadAllText(file.FullName);
            var tokens = vm.ClassTokenizer.Tokenize(src);
            IClassCompiler use = this;
            for (var i = 0; i < tokens.Count; i++)
                use = use.AcceptToken(vm, tokens, TODO, TODO, ref i);

            return _class;
        }

        public IClassCompiler NextPackage(string name)
        {
            return new ClassCompiler(this)
                { _package = _package.GetOrCreatePackage(name), State = ClassCompilerState.Package };
        }

        public IClassCompiler NextClass(string name)
        {
            return new ClassCompiler(this)
            {
                _package = _package, _class = _package.GetOrCreateClass(name, Modifier),
                State = ClassCompilerState.Class
            };
        }

        public IClassCompiler PushElement()
        {
            return Parent!;
        }

        public override ICompiler AcceptToken(RuntimeBase vm, IToken token, IToken? next, IToken? prev, ref int i)
        {
            if (State != ClassCompilerState.Class)
                throw new CompilerException("Invalid compiler state: " + State);

            switch (token.Type)
            {
                case TokenType.None:
                    break;
                case TokenType.Word:
                    break;
                case TokenType.Dot:
                    break;
                case TokenType.Colon:
                    break;
                case TokenType.Comma:
                    break;
                case TokenType.Equals:
                    break;
                case TokenType.IdentNum:
                case TokenType.IdentStr:
                case TokenType.IdentVoid:
                    break;
                case TokenType.Extends:
                    break;
                case TokenType.Implements:
                    break;
                case TokenType.Class:
                case TokenType.Interface:
                case TokenType.Enum:
                    break;
                case TokenType.Public:
                case TokenType.Internal:
                case TokenType.Protected:
                case TokenType.Private:

                case TokenType.Static:
                //case TokenType.Dynamic:

                case TokenType.Abstract:
                case TokenType.Final:
                    Modifier |= FindModifier(token.Type);
                    break;
                case TokenType.ParRoundOpen:
                    break;
                case TokenType.ParRoundClose:
                    break;
                case TokenType.ParSquareOpen:
                    break;
                case TokenType.ParSquareClose:
                    break;
                case TokenType.ParAccOpen:
                    break;
                case TokenType.ParAccClose:
                    break;
                case TokenType.ParDiamondOpen:
                    break;
                case TokenType.ParDiamondClose:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override IRuntimeSite Compile(RuntimeBase vm)
        {
            return _class;
        }

        private MemberModifier FindModifier(TokenType tokenType)
        {
            return tokenType switch
            {
                TokenType.Public => MemberModifier.Public,
                TokenType.Internal => MemberModifier.Internal,
                TokenType.Protected => MemberModifier.Protected,
                TokenType.Private => MemberModifier.Private,
                TokenType.Static => MemberModifier.Static,
                TokenType.Abstract => MemberModifier.Abstract,
                TokenType.Final => MemberModifier.Final,
                _ => throw new ArgumentOutOfRangeException(nameof(tokenType), tokenType, "No MemberModifier available")
            };
        }
    }
}