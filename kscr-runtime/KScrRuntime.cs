using KScr.Compiler;
using KScr.Core.Model;
using KScr.Core.Store;
using KScr.Native;

namespace KScr.Runtime;

public sealed class KScrRuntime : CompilerRuntime
{
    public override ObjectStore ObjectStore { get; } = new();
    public override INativeRunner? NativeRunner { get; } = new NativeRunner();
}