using System.Collections.Generic;
using KScr.Lib;
using KScr.Lib.Bytecode;
using KScr.Lib.Exception;
using KScr.Lib.Model;

namespace KScr.Compiler.Class
{
    public class ParameterDefinitionCompiler : AbstractCompiler
    {
        private readonly Method _method;
        private int pIndex = -1, pState = 0;
        private bool _active = true;

        public ParameterDefinitionCompiler(ClassCompiler parent, Method method) : base(parent)
        {
            _method = method;
        }

        public override bool Active => _active;

        public override ICompiler? AcceptToken(RuntimeBase vm, ref CompilerContext ctx)
        {
            switch (ctx.Token.Type)
            {
                case TokenType.Word:
                    if (pState == 0)
                    { // parse type
                        if (pIndex >= _method.Parameters.Count)
                            throw new CompilerException("Invalid parameter index during compilation");
                        
                        _method.Parameters.Add(new MethodParameter());
                        _method.Parameters[++pIndex].Type = vm.FindType(ctx.Token.Arg!)!;
                        pState = 1;
                    } else if (pState == 1)
                    { // parse name
                        _method.Parameters[pIndex].Name = ctx.Token.Arg!;
                        pState = 0;
                    }
                    break;
                case TokenType.Comma:
                    pIndex += 1;
                    break;
                case TokenType.ParRoundClose:
                    _active = false;
                    return Parent;
            }
            return this;
        }
    }
}