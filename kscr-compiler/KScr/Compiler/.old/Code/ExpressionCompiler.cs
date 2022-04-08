using System;
using KScr.Core.Bytecode;
using KScr.Core.Core;
using KScr.Core.Exception;
using KScr.Core.Model;

namespace KScr.Compiler.Code
{
    public class ExpressionCompiler : AbstractCodeCompiler
    {
        public ExpressionCompiler(ICompiler parent, bool endBeforeTerminator = false, params TokenType[] terminators) :
            base(parent, endBeforeTerminator, terminators)
        {
        }

        public ExpressionCompiler(ICompiler parent, bool endBeforeTerminator = false,
            TokenType terminator = TokenType.Terminator)
            : this(parent, endBeforeTerminator, new[] { terminator })
        {
        }
    }
}