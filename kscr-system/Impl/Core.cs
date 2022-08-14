using KScr.Core;
using KScr.Core.Model;
using KScr.Core.Std;
using KScr.Core.Store;
using String = KScr.Core.Std.String;

namespace KScr.Native.System.Impl;

[NativeImpl(Package = "org.comroid.kscr.core", ClassName = "type")]
public class Type
{
    [NativeImpl]
    public static IObjectRef getCanonicalName(RuntimeBase vm, Stack stack, IObject target, params IObject[] args) =>
        String.Instance(vm, (target as IClassInstance)!.CanonicalName);
    
    [NativeImpl]
    public static IObjectRef getName(RuntimeBase vm, Stack stack, IObject target, params IObject[] args) =>
        String.Instance(vm, (target as IClassInstance)!.Name);
    
    [NativeImpl]
    public static IObjectRef getDetailedName(RuntimeBase vm, Stack stack, IObject target, params IObject[] args) =>
        String.Instance(vm, (target as IClassInstance)!.DetailedName);

    [NativeImpl]
    public static IObjectRef getFullName(RuntimeBase vm, Stack stack, IObject target, params IObject[] args) => 
        String.Instance(vm, (target as IClassInstance)!.FullName);

    [NativeImpl]
    public static IObjectRef getFullDetailedName(RuntimeBase vm, Stack stack, IObject target, params IObject[] args) => 
        String.Instance(vm, (target as IClassInstance)!.FullDetailedName);
}