using KScr.Lib;
using KScr.Core.Core;
using KScr.Core.Exception;
using KScr.Core.Store;
using static KScr.Core.Store.StackOutput;
using String = KScr.Core.Core.String;
// ReSharper disable InconsistentNaming,UnusedMember.Global

namespace KScr.Native.System.Impl;

[NativeImpl(Package = "org.comroid.kscr.io", ClassName = "File")]
public class File
{
    [NativeImpl]
    public static IObjectRef ctor(RuntimeBase vm, Stack stack, IObject target, params IObject[] args) => throw new NotImplementedException();
    [NativeImpl]
    public static IObjectRef read(RuntimeBase vm, Stack stack, IObject target, params IObject[] args) => throw new NotImplementedException();
    [NativeImpl]
    public static IObjectRef write(RuntimeBase vm, Stack stack, IObject target, params IObject[] args) => throw new NotImplementedException();
    [NativeImpl]
    public static IObjectRef absPath(RuntimeBase vm, Stack stack, IObject target, params IObject[] args)
    {
        if (args[0] is String intern && intern.Str is {} str)
            return stack[Default] = String.Instance(vm, str.EndsWith("/") 
                ? new DirectoryInfo(str).FullName : new FileInfo(str).FullName);
        throw new FatalException("Invalid arguments: " + string.Join(", ", args.Select(x => x.ToString(0))));
    }

    [NativeImpl]
    public static IObjectRef name(RuntimeBase vm, Stack stack, IObject target, params IObject[] args)
    {
        if (args[0] is String intern && intern.Str is {} str)
            return stack[Default] = String.Instance(vm, str.EndsWith("/") 
                ? new DirectoryInfo(str).Name : new FileInfo(str).Name);
        throw new FatalException("Invalid arguments: " + string.Join(", ", args.Select(x => x.ToString(0))));
    }

    [NativeImpl]
    public static IObjectRef exists(RuntimeBase vm, Stack stack, IObject target, params IObject[] args)
    {
        if (args[0] is String intern && intern.Str is {} str)
            return stack[Default] = (str.EndsWith("/") 
                ? new DirectoryInfo(str).Exists : new FileInfo(str).Exists) 
                ? vm.ConstantTrue : vm.ConstantFalse;
        throw new FatalException("Invalid arguments: " + string.Join(", ", args.Select(x => x.ToString(0))));
    }

    [NativeImpl]
    public static IObjectRef isDir(RuntimeBase vm, Stack stack, IObject target, params IObject[] args)
    {
        if (args[0] is String intern && intern.Str is {} str)
            return stack[Default] = str.EndsWith("/") ? vm.ConstantTrue : vm.ConstantFalse;
        throw new FatalException("Invalid arguments: " + string.Join(", ", args.Select(x => x.ToString(0))));
    }
}