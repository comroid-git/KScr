using System;
using System.Linq;
using KScr.Lib;
using KScr.Lib.Bytecode;
using KScr.Lib.Exception;
using KScr.Lib.Model;
using static KScr.Lib.Exception.CompilerError;
using static KScr.Lib.Model.TokenType;

namespace KScr.Compiler.Class
{
    public class TypeParameterDefinitionCompiler : AbstractCompiler
    {
        private bool _active = true;
        private int pIndex = -1, pState;

        public TypeParameterDefinitionCompiler(ClassCompiler parent) : base(parent) { }

        public override bool Active => _active;

        public override ICompiler? AcceptToken(RuntimeBase vm, ref CompilerContext ctx)
        {
            switch (ctx.Token.Type)
            {
                case Word:
                    if (pState == 0)
                    {
                        // parse name
                        if (pIndex >= ctx.Class.BaseClass.TypeParameters.Count)
                            throw new FatalException("Invalid TypeParameter index during compilation");

                        var context = ctx;
                        if (ctx.Class.TypeParameters.Any(x => x.Name == context.Token.Arg!))
                            break;
                        ctx.Class.TypeParameters.Add(new TypeParameter(ctx.Token.Arg!));
                        pIndex += 1;
                        pState += 1;
                    }
                    else
                    {
                        // parse spec type name
                        // todo
                        throw new NotImplementedException();
                    }

                    break;
                case Comma:
                    pState = 0;
                    break;
                case Extends:
                    // default state
                    break;
                case Super:
                    (ctx.Class.TypeParameters[pIndex] as TypeParameter)!.Specialization =
                        TypeParameterSpecializationType.Super;
                    break;
                case Dot:
                    ctx.TokenIndex += 1;
                    if (ctx.PrevToken!.Type != ctx.Token.Type
                        || ctx.Token.Type != ctx.NextToken!.Type
                        || ctx.NextToken!.Type != Dot)
                        throw new CompilerException(ctx.Token.SourcefilePosition, InvalidToken, 
                            ctx.Class.FullName, ".", "");
                    if ((ctx.Class.TypeParameters[pIndex] as TypeParameter)!.Specialization ==
                        TypeParameterSpecializationType.N)
                        throw new CompilerException(ctx.Token.SourcefilePosition, InvalidToken, 
                            ctx.Class.FullName, "...", "Type Parameter n cannot be varargs!");
                    (ctx.Class.TypeParameters[pIndex] as TypeParameter)!.Specialization =
                        TypeParameterSpecializationType.List;
                    ctx.TokenIndex += 1;
                    return this;
                case ParDiamondClose:
                    _active = false;
                    return this;
                default: throw new CompilerException(ctx.Token.SourcefilePosition, UnexpectedToken,  ctx.Class.FullName, ctx.Token.String());
            }

            return this;
        }
    }
}