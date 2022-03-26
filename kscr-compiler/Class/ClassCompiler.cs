using System.Linq;
using KScr.Compiler.Code;
using KScr.Lib;
using KScr.Lib.Bytecode;
using KScr.Lib.Exception;
using KScr.Lib.Model;

namespace KScr.Compiler.Class
{
    public class ClassCompiler : AbstractCompiler
    {
        private bool _active = true;
        private bool inBody;
        private string? memberName;
        private int memberType;
        private Method method = null!;
        private MemberModifier? modifier;
        private Property property = null!;
        private ITypeInfo targetType = null!;

        public override bool Active => _active;

        private void ResetData()
        {
            property = null!;
            method = null!;
            memberType = 0;
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
                    ctx.TokenIndex += 1;
                    var type = ctx.FindType(vm, ctx.FindCompoundWord())!.BaseClass;
                    ctx.Class.Imports.Add(type.FullName);
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
                case TokenType.Extends:
                    if (inBody)
                        throw new CompilerException(ctx.Token.SourcefilePosition,
                            "Invalid extends-Token inside class body");
                    while (ctx.NextToken?.Type == TokenType.Word)
                    {
                        ctx.TokenIndex += 1;
                        var cls = ctx.FindTypeInfo(vm) as IClassInstance;
                        if (cls == null)
                            throw new CompilerException(ctx.Token.SourcefilePosition,
                                "Invalid extends-Token; Type not found");
                        if (cls.ClassType != ctx.Class.ClassType)
                            throw new CompilerException(ctx.Token.SourcefilePosition,
                                "Invalid extends-Token; Type is not " + ctx.Class.ClassType + ": " + cls.FullName);
                        ctx.Class.Interfaces.Add(cls);
                        ctx.TokenIndex += 1;
                    }

                    ctx.TokenIndex -= 1;
                    break;
                case TokenType.Implements:
                    if (inBody)
                        throw new CompilerException(ctx.Token.SourcefilePosition,
                            "Invalid implements-Token inside class body");
                    while (ctx.NextToken?.Type == TokenType.Word)
                    {
                        ctx.TokenIndex += 1;
                        var cls = ctx.FindTypeInfo(vm) as IClassInstance;
                        if (cls == null)
                            throw new CompilerException(ctx.Token.SourcefilePosition,
                                "Invalid implements-Token; Type not found: " + ctx.NextToken?.String());
                        if (cls.ClassType != ClassType.Interface)
                            throw new CompilerException(ctx.Token.SourcefilePosition,
                                "Invalid implements-Token; Type is not interface: " + cls.FullName);
                        ctx.Class.Interfaces.Add(cls);
                        ctx.TokenIndex += 1;
                    }

                    ctx.TokenIndex -= 1;
                    break;
                case TokenType.Word:
                    if (!inBody)
                        break;
                    if (targetType == null)
                        // is return type
                        targetType = ctx.FindTypeInfo(vm);
                    else if (memberName == null)
                        // is name
                        memberName = ctx.Token.Arg!;

                    memberType = 2; // property
                    if (ctx.NextToken?.Type == TokenType.ParAccOpen)
                    {
                        ctx.TokenIndex += 3;
                        bool gettable, settable;
                        gettable = ctx.PrevToken?.Type == TokenType.Word && ctx.PrevToken?.Arg == "get";
                        settable = ctx.NextToken?.Type == TokenType.Word && ctx.NextToken?.Arg == "set";

                        ctx.Class.DeclaredMembers[memberName!] = property = new Property(ctx.Token.SourcefilePosition, ctx.Class, memberName!,
                            targetType,
                            modifier ?? (ctx.Class.ClassType is ClassType.Interface or ClassType.Annotation
                                ? MemberModifier.Public | MemberModifier.Abstract
                                : MemberModifier.Protected))
                        {
                            Gettable = gettable,
                            Settable = settable
                        };
                        ctx.TokenIndex += settable ? 3 : 1;

                        if (ctx.NextToken?.Type == TokenType.OperatorEquals)
                        {
                            //todo: parse initializer
                        }

                        ResetData();
                    }

                    break;
                // into computed property
                // todo: setter
                case TokenType.OperatorEquals:
                    if (!inBody)
                        break;
                    if (ctx.NextToken!.Type != TokenType.ParDiamondClose)
                        break;
                    if (memberType != 2) // computed property
                        throw new CompilerException(ctx.Token.SourcefilePosition,
                            "Could not create field; invalid memberType = " + memberType);
                    ctx.Class.DeclaredMembers[memberName!] = property = new Property(ctx.Token.SourcefilePosition, ctx.Class, memberName!, targetType,
                        modifier ?? (ctx.Class.ClassType is ClassType.Interface or ClassType.Annotation
                            ? MemberModifier.Public
                            : MemberModifier.Protected));
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
                    ctx.Parent!.TokenIndex = ctx.TokenIndex;
                    ResetData();

                    break;
                // into method
                case TokenType.ParRoundOpen:
                    if (!inBody)
                        break;
                    if (memberType != 2)
                        throw new CompilerException(ctx.Token.SourcefilePosition,
                            "Could not create method; unexpected memberType");
                    memberType = memberName == null
                        ? 3 // ctor 
                        : 1; // method
                    if (memberType == 3 && (modifier & MemberModifier.Static) == 0)
                        modifier |= MemberModifier.Static; // constructor must be static
                    // compile parameter definition
                    ctx.Class.DeclaredMembers[memberName ?? Method.ConstructorName] = method
                        = new Method(ctx.Token.SourcefilePosition, ctx.Class, memberName ?? Method.ConstructorName, targetType,
                            modifier ?? (ctx.Class.ClassType is ClassType.Interface or ClassType.Annotation
                                ? MemberModifier.Public | MemberModifier.Abstract
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
                        CompilerLoop(vm, new TypeParameterDefinitionCompiler(this), ref ctx);
                        ctx.Parent!.TokenIndex = ctx.TokenIndex - 1;
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
                        if (modifier == MemberModifier.Static && memberName == null)
                            ctx.Class.DeclaredMembers[memberName ?? Method.StaticInitializerName]
                                = method = new Method(ctx.Token.SourcefilePosition, ctx.Class, Method.StaticInitializerName,
                                    Lib.Bytecode.Class.VoidType, MemberModifier.Private | MemberModifier.Static);
                        else break;

                    if (ctx.Class.ClassType is not (ClassType.Interface or ClassType.Annotation))
                        // compile method body
                        ctx = new CompilerContext(ctx, CompilerType.CodeStatement);
                    ctx.TokenIndex += 1;
                    CompilerLoop(vm, new StatementCompiler(this, false, TokenType.ParAccClose), ref ctx);
                    method.Body = ctx.ExecutableCode;
                    ctx.Parent!.TokenIndex = ctx.TokenIndex;
                    ctx = ctx.Parent!;
                    ResetData();

                    break;
                case TokenType.Terminator when !inBody:
                    _active = false;
                    break;
                case TokenType.Terminator:
                    if (method != null || property != null)
                    {
                        if (method != null && method.Body == null)
                            if (method.Name == Method.ConstructorName)
                                method.Body = new ExecutableCode();
                            else if (!method.IsAbstract() && !method.IsNative() && ctx.Class.ClassType is not ClassType.Interface or ClassType.Annotation)
                                throw new CompilerException(ctx.Token.SourcefilePosition,
                                    $"Invalid method {method.Name}: Not abstract and no body defined");
                        ResetData();
                    }

                    break;
                case TokenType.ParAccClose:
                    if (inBody)
                    {
                        _active = false;
                        // validate class
                        var context = ctx;
                        string[] unimplemented = (ctx.Class as IClass).InheritedMembers
                            .Where(x => x.IsAbstract())
                            .Where(x => !context.Class.DeclaredMembers.ContainsKey(x.Name))
                            .Select(x => x.FullName)
                            .ToArray();
                        if (unimplemented.Length > 0)
                            throw new CompilerException(ctx.Token.SourcefilePosition,
                                $"Class {ctx.Class} does not implement the following abstract members:\n\t-\t{string.Join("\n\t-\t", unimplemented)}");
                    }

                    break;
                default:
                    if (!inBody)
                        break;
                    var mod = ctx.Token.Type.Modifier();
                    if (mod == null)
                        break;
                    if (modifier == null)
                        modifier = mod;
                    else modifier |= mod;
                    break;
            }

            return this;
        }
    }
}