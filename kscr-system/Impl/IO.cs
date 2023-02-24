using System.Collections.Concurrent;
using System.Text;
using comroid.csapi.common;
using KScr.Core;
using KScr.Core.Exception;
using KScr.Core.System;
using KScr.Core.Store;
using static KScr.Core.Store.StackOutput;
using String = KScr.Core.System.String;

// ReSharper disable InconsistentNaming,UnusedMember.Global

namespace KScr.Native.System.Impl;

[NativeImpl(Package = "org.comroid.kscr.io", ClassName = "File")]
public class File
{
    private static readonly IDictionary<long, File> interns = new ConcurrentDictionary<long, File>();
    private readonly string Path;

    private File(string path)
    {
        Path = path;
    }

    private static File Get(IObject obj) => interns[obj.ObjectId];
    private static File Create(IObject obj, string path) => interns[obj.ObjectId] = new File(path);
    
    [NativeImpl]
    public static IObjectRef ctor(RuntimeBase vm, Stack stack, IObject target, params IObject[] args)
    {
        Create(target, args[0].ToString()!);
        return new ObjectRef(target);
    }

    [NativeImpl]
    public static IObjectRef read(RuntimeBase vm, Stack stack, IObject target, params IObject[] args)
    {
        var f = Get(target);
        using var read = global::System.IO.File.OpenRead(f.Path);
        var l = (args[0] as Numeric)!.IntValue;
        var b = new byte[l];
        int c;
        if ((c = read.Read(b)) != l)
            Log<File>.At(LogLevel.Debug, $"Invalid amount of bytes read by {target}; got {c} expected {l}");
        return String.Instance(vm, RuntimeBase.Encoding.GetString(b[..c]));
    }

    [NativeImpl]
    public static IObjectRef write(RuntimeBase vm, Stack stack, IObject target, params IObject[] args)
    {
        var f = Get(target);
        using var write = global::System.IO.File.OpenWrite(f.Path);
        var s = args[0].ToString()!;
        var b = RuntimeBase.Encoding.GetBytes(s);
        write.Write(b);
        return Numeric.Constant(vm, b.Length);
    }

    [NativeImpl]
    public static IObjectRef absPath(RuntimeBase vm, Stack stack, IObject target, params IObject[] args)
    {
        if (args[0] is String intern && intern.Str is { } str)
            return stack[Default] = String.Instance(vm, str.EndsWith("/")
                ? new DirectoryInfo(str).FullName
                : new FileInfo(str).FullName);
        throw new FatalException("Invalid arguments: " + string.Join(", ", args.Select(x => x.ToString(0))));
    }

    [NativeImpl]
    public static IObjectRef name(RuntimeBase vm, Stack stack, IObject target, params IObject[] args)
    {
        if (args[0] is String intern && intern.Str is { } str)
            return stack[Default] = String.Instance(vm, str.EndsWith("/")
                ? new DirectoryInfo(str).Name
                : new FileInfo(str).Name);
        throw new FatalException("Invalid arguments: " + string.Join(", ", args.Select(x => x.ToString(0))));
    }

    [NativeImpl]
    public static IObjectRef exists(RuntimeBase vm, Stack stack, IObject target, params IObject[] args)
    {
        if (args[0] is String intern && intern.Str is { } str)
            return stack[Default] = (str.EndsWith("/")
                ? new DirectoryInfo(str).Exists
                : new FileInfo(str).Exists)
                ? vm.ConstantTrue
                : vm.ConstantFalse;
        throw new FatalException("Invalid arguments: " + string.Join(", ", args.Select(x => x.ToString(0))));
    }

    [NativeImpl]
    public static IObjectRef isDir(RuntimeBase vm, Stack stack, IObject target, params IObject[] args)
    {
        if (args[0] is String intern && intern.Str is { } str)
            return stack[Default] = str.EndsWith("/") ? vm.ConstantTrue : vm.ConstantFalse;
        throw new FatalException("Invalid arguments: " + string.Join(", ", args.Select(x => x.ToString(0))));
    }
}