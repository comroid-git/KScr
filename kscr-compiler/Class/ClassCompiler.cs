using KScr.Compiler.Code;
using KScr.Lib;
using KScr.Lib.Bytecode;
using KScr.Lib.Exception;
using KScr.Lib.Model;

namespace KScr.Compiler.Class
{
    public class ClassCompiler : AbstractCompiler
    {
        private bool inBody;
        private string? memberName;
        private int memberType;
        private Method method = null!;
        private Field property = null!;
        private MemberModifier? modifier;
        private ITypeInfo targetType = null!;

        private void ResetData()
        {
            property = null!;
            method = null!;
            memberName = null;
            modifier = null!;
            targetType = null!;
        }

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
                    break;
               case TokenType.Import:
                   var type = ctx.FindType(vm, ctx.FindCompoundWord())!.BaseClass;
                   ctx.Class.Imports[type.Name] = type;
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
                    targetType = Lib.Bytecode.Class.NumericIntType;
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

                    memberType = 2; // property
                    break;
                // into computed property
                // todo: setter
                case TokenType.OperatorMinus:
                    if (!inBody)
                        break;
                    if (ctx.NextToken!.Type != TokenType.ParDiamondClose)
                        break;
                    if (memberType != 2) // computed property
                        throw new CompilerException("Could not create field; invalid memberType = " + memberType);
                    property = new Field(ctx.Class, memberName!, modifier ?? MemberModifier.Protected);
                    ctx = new CompilerContext(ctx, CompilerType.CodeExpression);
                    ctx.Statement = new Statement
                    {
                        Type = StatementComponentType.Expression,
                        TargetType = Lib.Bytecode.Class.VoidType.DefaultInstance // todo support type parameters
                    };
                    ctx.TokenIndex += 2;
                    CompilerLoop(vm, new ExpressionCompiler(this), ref ctx);
                    property.Getter = ctx.ExecutableCode;
                    ctx.Class.DeclaredMembers[memberName!] = property;
                    ctx.Parent!.TokenIndex = ctx.TokenIndex - 1;
                    ResetData();
                    
                    break;
                // into method
                case TokenType.ParRoundOpen:
                    if (!inBody)
                        break;
                    if (memberType != 2)
                        throw new CompilerException("Could not create method; unexpected memberType");
                    memberType = memberName == null 
                        ? 3  // ctor 
                        : 1; // method
                    if (memberType == 3 && (modifier & MemberModifier.Static) == 0)
                        modifier |= MemberModifier.Static; // constructor must be static
                    // compile parameter definition
                    method = new Method(ctx.Class, memberName ?? "ctor", targetType,
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
                    ctx.Class.DeclaredMembers[memberName ?? "ctor"] = method;
                    ctx.Parent!.TokenIndex = ctx.TokenIndex - 1;
                    ctx = ctx.Parent!;
                    ResetData();

                    break;
            }

            return this;
        }
    }
}