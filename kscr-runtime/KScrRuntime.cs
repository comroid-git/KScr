using KScr.Compiler;
using KScr.Lib;
using KScr.Lib.Model;
using KScr.Lib.Store;

namespace KScr.Runtime
{
    public sealed class KScrRuntime : CompilerRuntime
    {
        public override ObjectStore ObjectStore { get; } = new ObjectStore();
    }
}