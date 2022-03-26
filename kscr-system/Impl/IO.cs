using KScr.Lib;
using KScr.Lib.Core;
using KScr.Lib.Store;

namespace KScr.Native.System.Impl;

[NativeImpl(Package = "org.comroid.kscr.io", ClassName = "File")]
public class File
{
    [NativeImpl]
    public static IObjectRef read(RuntimeBase vm, Stack stack, params IObject[] args)
    {
        throw new NotImplementedException();
    }

    [NativeImpl]
    public static IObjectRef write(RuntimeBase vm, Stack stack, params IObject[] args)
    {
        throw new NotImplementedException();
    }
}