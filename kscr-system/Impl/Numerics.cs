using KScr.Lib;
using KScr.Lib.Core;
using KScr.Lib.Store;

namespace KScr.Native.System.Impl;

[NativeImpl(Package = "org.comroid.kscr.numerics", ClassName = "Math")]
public static class Math
{
    [NativeImpl]
    public static IObjectRef sqrt(RuntimeBase vm, Stack stack, params IObject[] args)
    {
        if (args[0] is not Numeric num)
            throw new ArgumentException("Invalid Argument; expected num");
        return num.Sqrt(vm);
    }
    [NativeImpl]
    public static IObjectRef sin(RuntimeBase vm, Stack stack, params IObject[] args)
    {
        if (args[0] is not Numeric num)
            throw new ArgumentException("Invalid Argument; expected num");
        return num.Sin(vm);
    }
    [NativeImpl]
    public static IObjectRef cos(RuntimeBase vm, Stack stack, params IObject[] args)
    {
        if (args[0] is not Numeric num)
            throw new ArgumentException("Invalid Argument; expected num");
        return num.Cos(vm);
    }
    [NativeImpl]
    public static IObjectRef tan(RuntimeBase vm, Stack stack, params IObject[] args)
    {
        if (args[0] is not Numeric num)
            throw new ArgumentException("Invalid Argument; expected num");
        return num.Tan(vm);
    }
}