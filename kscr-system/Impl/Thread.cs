using KScr.Lib;
using KScr.Lib.Core;
using KScr.Lib.Store;

// ReSharper disable InconsistentNaming

namespace KScr.Native.System.Impl;

[NativeImpl(Package = "org.comroid.kscr.async", ClassName = "Thread")]
public sealed class Thread
{
    [NativeImpl]
    public static IObject ctor(RuntimeBase vm, Stack stack, params IObject[] args) => throw new NotImplementedException();
}