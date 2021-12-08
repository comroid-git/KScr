using KScr.Compiler.Code;
using KScr.Lib;
using KScr.Lib.Bytecode;
using KScr.Lib.Exception;
using KScr.Lib.Model;

namespace KScr.Compiler.Class
{
    public class ClassCompiler : AbstractCompiler
    {
        private MemberModifier? modifier;
        private string? memberName;
        private int memberType = 0;
        private IClassRef targetType = null!;
        private Method method = null!;
        private Field field = null!;

        public override ICompiler? AcceptToken(RuntimeBase vm, ref CompilerContext ctx)
        {
            if (ctx.Type != CompilerType.Class)
                return Parent;

            ICompiler sub;
            switch (ctx.Token.Type)
            {
                // parse type parameter names 
                case TokenType.ParDiamondOpen:
                case TokenType.ParDiamondClose:
                    // todo
                    break; 
                case TokenType.Public:
                case TokenType.Protected:
                case TokenType.Internal:
                case TokenType.Private:
                case TokenType.Abstract:
                case TokenType.Final:
                case TokenType.Static:
                    var mod = ctx.Token.Type.Modifier();
                    if (modifier == null)
                        modifier = mod;
                    else modifier |= mod;
                    break;
                case TokenType.Word:
                    if (targetType == null)
                    {
                        string targetTypeIdentifier = FindCompoundWord(ctx);
                        targetType = vm.FindType(targetTypeIdentifier) ?? throw new CompilerException("Could not find type: " + targetTypeIdentifier);
                    }
                    else if (memberName == null) 
                        memberName = ctx.Token.Arg!;
                    memberType = 2; // field
                    break;
                // into field
                case TokenType.OperatorEquals:
                    break;
                // into method
                case TokenType.ParRoundOpen:
                    if (memberType != 2)
                        break; // todo
                    memberType = 1; // method

                    // compile parameter definition
                    method = new Method(ctx.Class, memberName!, modifier!.Value);
                    sub = new ParameterDefinitionCompiler(this, method);
                    ctx = new CompilerContext(ctx, CompilerType.ParameterDefintion);
                    CompilerLoop(vm, ref sub, ref ctx);
                    method.Body = ctx.ExecutableCode;
                    ctx = ctx.Parent!;

                    break;
                case TokenType.ParAccOpen:
                    if (method == null)
                        break;
                    
                    // compile method body
                    sub = new StatementCompiler(this);
                    ctx = new CompilerContext(ctx, CompilerType.CodeStatement);
                    CompilerLoop(vm, ref sub, ref ctx);
                    method.Body = ctx.ExecutableCode;
                    ctx = ctx.Parent!;
                    
                    break;
            }

            return this;
        }
    }
}