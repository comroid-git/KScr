using System;
using System.Collections.Generic;
using KScr.Lib;
using KScr.Lib.Bytecode;
using KScr.Lib.Model;
using KScr.Lib.Store;

namespace KScr.Eval
{
    public sealed class ClassCompiler : IClassCompiler
    {
        public IClassCompiler AcceptToken(RuntimeBase vm, IList<ClassToken> tokens, ref int i)
        {
            throw new NotImplementedException();
        }

        public Package Compile(RuntimeBase vm)
        {
            throw new NotImplementedException();
        }
    }
}