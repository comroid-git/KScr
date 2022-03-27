using System;
using KScr.Lib.Bytecode;
using KScr.Lib.Core;
using KScr.Lib.Exception;
using KScr.Lib.Model;

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