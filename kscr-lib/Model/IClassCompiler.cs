using System.Collections.Generic;
using KScr.Lib.Bytecode;
using KScr.Lib.Store;

namespace KScr.Lib.Model
{
    public interface IClassCompiler
    {
        public IClassCompiler AcceptToken(RuntimeBase vm, IList<ClassToken> tokens, ref int i);

        public Package Compile(RuntimeBase vm);
    }
}