using KScr.Lib;
using KScr.Lib.Core;
using KScr.Lib.Store;
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

namespace KScr.Native.System.Impl;

[NativeImpl(Package = "org.comroid.kscr.core", ClassName = "Object")]
public class Object
{
    [NativeImpl]
    public static IObjectRef InternalID(RuntimeBase vm, Stack stack, params IObject[] args) => throw new NotImplementedException();
    [NativeImpl]
    public static IObjectRef Type(RuntimeBase vm, Stack stack, params IObject[] args) => throw new NotImplementedException();
    [NativeImpl]
    public static IObjectRef toString(RuntimeBase vm, Stack stack, params IObject[] args) => throw new NotImplementedException();
    [NativeImpl]
    public static IObjectRef equals(RuntimeBase vm, Stack stack, params IObject[] args) => throw new NotImplementedException();
}

