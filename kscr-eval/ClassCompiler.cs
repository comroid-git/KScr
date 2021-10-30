﻿using System;
using System.Collections.Generic;
using System.IO;
using KScr.Lib;
using KScr.Lib.Bytecode;
using KScr.Lib.Exception;
using KScr.Lib.Model;

namespace KScr.Eval
{
    public sealed class ClassCompiler : IClassCompiler
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
                use = use.AcceptToken(vm, tokens, ref i);

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

        public IClassCompiler AcceptToken(RuntimeBase vm, IList<ClassToken> tokens, ref int i)
        {
            if (State != ClassCompilerState.Class)
                throw new CompilerException("Invalid compiler state: " + State);

            var token = tokens[i];
            var prev = i - 1 < 0 ? null : tokens[i - 1];
            var next = i + 1 >= tokens.Count ? null : tokens[i + 1];

            switch (token.Type)
            {
                case ClassTokenType.None:
                    break;
                case ClassTokenType.Word:
                    break;
                case ClassTokenType.Dot:
                    break;
                case ClassTokenType.Colon:
                    break;
                case ClassTokenType.Comma:
                    break;
                case ClassTokenType.Equals:
                    break;
                case ClassTokenType.IdentNum:
                case ClassTokenType.IdentStr:
                case ClassTokenType.IdentVoid:
                    break;
                case ClassTokenType.Extends:
                    break;
                case ClassTokenType.Implements:
                    break;
                case ClassTokenType.Class:
                case ClassTokenType.Interface:
                case ClassTokenType.Enum:
                    break;
                case ClassTokenType.Public:
                case ClassTokenType.Internal:
                case ClassTokenType.Protected:
                case ClassTokenType.Private:

                case ClassTokenType.Static:
                //case ClassTokenType.Dynamic:

                case ClassTokenType.Abstract:
                case ClassTokenType.Final:
                    Modifier |= FindModifier(token.Type);
                    break;
                case ClassTokenType.ParRoundOpen:
                    break;
                case ClassTokenType.ParRoundClose:
                    break;
                case ClassTokenType.ParSquareOpen:
                    break;
                case ClassTokenType.ParSquareClose:
                    break;
                case ClassTokenType.ParAccOpen:
                    break;
                case ClassTokenType.ParAccClose:
                    break;
                case ClassTokenType.ParDiamondOpen:
                    break;
                case ClassTokenType.ParDiamondClose:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private MemberModifier FindModifier(ClassTokenType tokenType)
        {
            return tokenType switch
            {
                ClassTokenType.Public => MemberModifier.Public,
                ClassTokenType.Internal => MemberModifier.Internal,
                ClassTokenType.Protected => MemberModifier.Protected,
                ClassTokenType.Private => MemberModifier.Private,
                ClassTokenType.Static => MemberModifier.Static,
                ClassTokenType.Abstract => MemberModifier.Abstract,
                ClassTokenType.Final => MemberModifier.Final,
                _ => throw new ArgumentOutOfRangeException(nameof(tokenType), tokenType, "No MemberModifier available")
            };
        }
    }
}