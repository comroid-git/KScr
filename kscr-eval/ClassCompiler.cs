using System;
using System.Collections.Generic;
using System.IO;
using KScr.Lib;
using KScr.Lib.Bytecode;
using KScr.Lib.Exception;
using KScr.Lib.Model;
using KScr.Lib.Store;

namespace KScr.Eval
{
    public sealed class ClassCompiler : IClassCompiler
    {
        public readonly IClassCompiler? Parent;
        public ClassCompilerState State { get; private set; } = ClassCompilerState.Idle;
        private Package _package = Package.RootPackage;
        private Class _class = null!;

        public ClassCompiler(IClassCompiler? parent)
        {
            Parent = parent;
        }

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
            var tokens = new ClassTokenizer().Tokenize(src);
            IClassCompiler use = this;
            for (int i = 0; i < tokens.Count; i++) 
                use = use.AcceptToken(vm, tokens, ref i);

            return _class;
        }

        public IClassCompiler NextPackage(string name) => new ClassCompiler(this)
            { _package = _package.GetOrCreatePackage(name), State = ClassCompilerState.Package };

        public IClassCompiler NextClass(string name) => new ClassCompiler(this)
            { _package = _package, _class = _package.GetOrCreateClass(name), State = ClassCompilerState.Class };

        public IClassCompiler AcceptToken(RuntimeBase vm, IList<ClassToken> tokens, ref int i)
        {
            if (State != ClassCompilerState.Class)
                throw new CompilerException("Invalid compiler state: " + State);
            
            var token = tokens[i];
            var prev = i - 1 < 0 ? null : tokens[i - 1];
            var next = i + 1 >= tokens.Count ? null : tokens[i + 1];
            
            switch (token.Modifier)
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
                case ClassTokenType.Public:
                case ClassTokenType.Internal:
                case ClassTokenType.Protected:
                case ClassTokenType.Private:
                    break;
                case ClassTokenType.Class:
                case ClassTokenType.Interface:
                case ClassTokenType.Enum:
                    break;
                case ClassTokenType.Static:
                case ClassTokenType.Dynamic:
                    break;
                case ClassTokenType.Abstract:
                case ClassTokenType.Final:
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
        
        public IClassCompiler PushElement() => Parent!;
    }
}