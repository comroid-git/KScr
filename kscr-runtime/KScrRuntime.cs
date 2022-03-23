using KScr.Compiler;
using KScr.Lib.Model;
using KScr.Lib.Store;
using KScr.Native;

namespace KScr.Runtime
{
    public sealed class KScrRuntime : CompilerRuntime
    {
        public override ObjectStore ObjectStore { get; } = new();
        public override INativeRunner? NativeRunner { get; } = new NativeRunner();
    }
}