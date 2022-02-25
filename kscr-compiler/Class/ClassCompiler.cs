﻿using KScr.Compiler.Code;
using KScr.Lib;
using KScr.Lib.Bytecode;
using KScr.Lib.Exception;
using KScr.Lib.Model;

namespace KScr.Compiler.Class
{
    public class ClassCompiler : AbstractCompiler
    {
        private Field field = null!;
        private bool inBody;
        private string? memberName;
        private int memberType;
        private Method method = null!;
        private MemberModifier? modifier;
        private ITypeInfo targetType = null!;

        public override ICompiler? AcceptToken(RuntimeBase vm, ref CompilerContext ctx)
        {
            while (ctx.Type != CompilerType.Class && ctx.Type != CompilerType.Package)
            {
                ctx.Parent!.TokenIndex = ctx.TokenIndex;
                ctx = ctx.Parent!;
            }

            switch (ctx.Token.Type)
            {
                case TokenType.Package:
                    ctx.SkipPackage();
                    ctx.SkipImports();
                    break;
                case TokenType.Public:
                case TokenType.Protected:
                case TokenType.Internal:
                case TokenType.Private:
                case TokenType.Abstract:
                case TokenType.Final:
                case TokenType.Static:
                    if (!inBody)
                        break;
                    var mod = ctx.Token.Type.Modifier() ?? MemberModifier.Protected;
                    if (modifier == null)
                        modifier = mod;
                    else modifier |= mod;
                    break;
                case TokenType.IdentVar:
                case TokenType.IdentVoid:
                    if (!inBody)
                        break;
                    targetType = Lib.Bytecode.Class.VoidType;
                    break;
                case TokenType.IdentNum:
                    if (!inBody)
                        break;
                    targetType = Lib.Bytecode.Class.NumericType;
                    break;
                case TokenType.IdentNumByte:
                    if (!inBody)
                        break;
                    targetType = Lib.Bytecode.Class.NumericByteType;
                    break;
                case TokenType.IdentNumShort:
                    if (!inBody)
                        break;
                    targetType = Lib.Bytecode.Class.NumericShortType;
                    break;
                case TokenType.IdentNumInt:
                    if (!inBody)
                        break;
                    targetType = Lib.Bytecode.Class.NumericIntegerType;
                    break;
                case TokenType.IdentNumLong:
                    if (!inBody)
                        break;
                    targetType = Lib.Bytecode.Class.NumericLongType;
                    break;
                case TokenType.IdentNumFloat:
                    if (!inBody)
                        break;
                    targetType = Lib.Bytecode.Class.NumericFloatType;
                    break;
                case TokenType.IdentNumDouble:
                    if (!inBody)
                        break;
                    targetType = Lib.Bytecode.Class.NumericDoubleType;
                    break;
                case TokenType.IdentStr:
                    if (!inBody)
                        break;
                    targetType = Lib.Bytecode.Class.StringType;
                    break;
                case TokenType.Word:
                    if (!inBody)
                        break;
                    if (targetType == null)
                    {
                        // is return type
                        string targetTypeIdentifier = ctx.Token.Arg!;
                        targetType = vm.FindTypeInfo(targetTypeIdentifier, ctx.Class, ctx.Package)
                                     ?? throw new CompilerException("Could not find type: " + targetTypeIdentifier);
                    }
                    else if (memberName == null)
                        // is name
                    {
                        memberName = ctx.Token.Arg!;
                    }

                    memberType = 2; // field
                    break;
                // into field
                case TokenType.OperatorEquals:
                    if (!inBody)
                        break;
                    break;
                // into method
                case TokenType.ParRoundOpen:
                    if (!inBody)
                        break;
                    if (memberType != 2)
                        throw new CompilerException("Could not create method; invalid memberType = " + memberType);
                    memberType = 1; // method

                    // compile parameter definition
                    method = new Method(ctx.Class, memberName!,
                        modifier ?? (ctx.Class.ClassType is ClassType.Interface or ClassType.Annotation
                            ? MemberModifier.Public
                            : MemberModifier.Protected));
                    ctx = new CompilerContext(ctx, CompilerType.ParameterDefintion);
                    ctx.TokenIndex += 1;
                    CompilerLoop(vm, new ParameterDefinitionCompiler(this, method), ref ctx);
                    ctx.Parent!.TokenIndex = ctx.TokenIndex - 1;
                    ctx = ctx.Parent!;

                    break;
                // compile type parameter definition
                case TokenType.ParDiamondOpen:
                    if (!inBody)
                    {
                        ctx = new CompilerContext(ctx, CompilerType.TypeParameterDefinition);
                        ctx.TokenIndex += 1;
                        CompilerLoop(vm, new TypeParameterDefinitionCompiler(this, ctx.Class), ref ctx);
                        ctx.Parent!.TokenIndex = ctx.TokenIndex;
                        ctx = ctx.Parent!;
                    }

                    break;
                case TokenType.ParAccOpen:
                    if (!inBody)
                    {
                        inBody = true;
                        break;
                    }

                    if (method == null)
                        break;

                    // compile method body
                    ctx = new CompilerContext(ctx, CompilerType.CodeStatement);
                    ctx.TokenIndex += 1;
                    CompilerLoop(vm, new StatementCompiler(this), ref ctx);
                    method.Body = ctx.ExecutableCode;
                    ctx.Class.DeclaredMembers[memberName!] = method;
                    ctx.Parent!.TokenIndex = ctx.TokenIndex;
                    ctx = ctx.Parent!;

                    break;
            }

            return this;
        }
    }
}