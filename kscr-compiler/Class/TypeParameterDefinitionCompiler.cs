using KScr.Lib;
using KScr.Lib.Bytecode;
using KScr.Lib.Exception;
using KScr.Lib.Model;

namespace KScr.Compiler.Class
{
    public class TypeParameterDefinitionCompiler : AbstractCompiler
    {
        private readonly KScr.Lib.Bytecode.Class _class;
        private bool _active = true;
        private int pIndex = -1, pState;

        public TypeParameterDefinitionCompiler(ClassCompiler parent, IClass @class) : base(parent)
        {
            _class = @class.BaseClass;
        }

        public override bool Active => _active;

        public override ICompiler? AcceptToken(RuntimeBase vm, ref CompilerContext ctx)
        {
            switch (ctx.Token.Type)
            {
                case TokenType.Word:
                    if (pState == 0)
                    {
                        // parse name
                        if (pIndex >= _class.BaseClass.TypeParameters.Count)
                            throw new CompilerException(ctx.Token.SourcefilePosition, "Invalid TypeParameter index during compilation");

                        _class.TypeParameters.Add(new TypeParameter(ctx.Token.Arg!));
                        pIndex += 1;
                        pState += 1;
                    }
                    else
                    {
                        // parse spec type name
                        // todo
                    }

                    break;
                case TokenType.Comma:
                    pState = 0;
                    break;
                case TokenType.Extends:
                    // default state
                    break;
                case TokenType.Super:
                    _class.TypeParameters[pIndex].Specialization = TypeParameterSpecializationType.Super;
                    break;
                case TokenType.Dot:
                    ctx.TokenIndex += 1;
                    if (ctx.PrevToken!.Type != ctx.Token.Type
                        || ctx.Token.Type != ctx.NextToken!.Type
                        || ctx.NextToken!.Type != TokenType.Dot)
                        throw new CompilerException(ctx.Token.SourcefilePosition, "Invalid Dot token");
                    if (_class.TypeParameters[pIndex].Specialization == TypeParameterSpecializationType.N)
                        throw new CompilerException(ctx.Token.SourcefilePosition, "Cannot list N");
                    _class.TypeParameters[pIndex].Specialization = TypeParameterSpecializationType.List;
                    ctx.TokenIndex += 1;
                    return this;
                case TokenType.ParDiamondClose:
                    _active = false;
                    return Parent;
                default: throw new CompilerException(ctx.Token.SourcefilePosition, "Unexpected token: " + ctx.Token);
            }

            return this;
        }
    }
}