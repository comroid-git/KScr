using KScr.Core;
using KScr.Core.Std;
using KScr.Core.Store;

// ReSharper disable InconsistentNaming

namespace KScr.Native.System.Impl;

[NativeImpl(Package = "org.comroid.kscr.async", ClassName = "Thread")]
public static class Thread
{
    [NativeImpl]
    public static IObjectRef ctor(RuntimeBase vm, Stack stack, IObject target, params IObject[] args)
    {
        throw new NotImplementedException();
    }
}