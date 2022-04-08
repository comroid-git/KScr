using System.Linq;
using KScr.Lib;
using KScr.Core.Bytecode;
using KScr.Core.Exception;
using KScr.Core.Model;

namespace KScr.Compiler.Class
{
    public class ParameterDefinitionCompiler : AbstractCompiler
    {
        private readonly Method _method;
        private bool _active = true;
        private int pIndex = -1, pState;

        public ParameterDefinitionCompiler(ClassCompiler parent, Method method) : base(parent)
        {
            _method = method;
        }

        public override bool Active => _active;

        public override ICompiler? AcceptToken(RuntimeBase vm, ref CompilerContext ctx)
        {
            switch (ctx.Token.Type)
            {
                case TokenType.IdentNum:
                case TokenType.IdentStr:
                case TokenType.IdentNumByte:
                case TokenType.IdentNumShort:
                case TokenType.IdentNumInt:
                case TokenType.IdentNumLong:
                case TokenType.IdentNumFloat:
                case TokenType.IdentNumDouble:
                case TokenType.Word:
                    if (pState == 0)
                    {
                        // parse type
                        if (pIndex > _method.Parameters.Count)
                            throw new FatalException("Invalid parameter index during compilation");

                        _method.Parameters.Add(new MethodParameter());
                        var name = ctx.Token.String();
                        if (ctx.FindType(vm, name) is { }  cls)
                            _method.Parameters[++pIndex].Type = cls;
                        else if (ctx.Class.TypeParameters.FirstOrDefault(x => x.Name == name) is {}  tp)
                            _method.Parameters[++pIndex].Type = tp;
                        pState = 1;
                    }
                    else if (pState == 1)
                    {
                        // parse name
                        _method.Parameters[pIndex].Name = ctx.Token.Arg!;
                        pState = 0;
                    }

                    break;
                case TokenType.Comma:
                    pState = 0;
                    break;
                case TokenType.ParRoundClose:
                    _active = false;
                    break;
            }

            return this;
        }
    }
}