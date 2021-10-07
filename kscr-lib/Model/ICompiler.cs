using System;
using System.Collections.Generic;

namespace KScr.Lib.Model
{

    public interface ICompiler
    {
        public IEvaluable Compile(RuntimeBase runtime, IList<Token> tokens);

        public ICompiler AcceptToken(RuntimeBase vm, IList<Token> tokens, ref int i);
        public IStatementComponent Compose(RuntimeBase runtime);
        public IEvaluable Compile(RuntimeBase runtime);
    }
}