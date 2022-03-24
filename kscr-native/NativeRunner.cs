﻿using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using KScr.Lib;
using KScr.Lib.Bytecode;
using KScr.Lib.Exception;
using KScr.Lib.Model;
using KScr.Lib.Store;
using static KScr.Lib.Store.StackOutput;

namespace KScr.Native;

public class NativeRunner : INativeRunner
{
    private readonly IDictionary<string, NativeImpl> _types = new ConcurrentDictionary<string, NativeImpl>();

    public NativeRunner()
    {
        // load all NativeImpl annotated members
        Debug.WriteLine("[NativeRunner] Loading native implementations...");

        // load system assembly
        LoadNativeAssembly(Path.Combine(RuntimeBase.SdkHome.FullName, "kscr-system.dll"));
        
        var dir = new DirectoryInfo(Path.Combine(RuntimeBase.SdkHome.FullName, "native"));
        if (!dir.Exists)
            dir.Create();
        foreach (var assemblyPath in dir.EnumerateFiles("*.dll", SearchOption.AllDirectories))
        {
            var loaded = LoadNativeAssembly(assemblyPath.FullName);
            Debug.WriteLine($"[NativeRunner] Processed {loaded} types from assembly {assemblyPath}");
        }
    }

    private int LoadNativeAssembly(string file)
    {
        var assembly = Assembly.LoadFile(file);
        return assembly.GetTypes()
            .Where(x => x.GetCustomAttribute<NativeImplAttribute>() != null)
            .Count(ProcessType);
    }

    private bool ProcessType(Type type)
    {
        var attr = type.GetCustomAttribute<NativeImplAttribute>()!;
        var name =
            (attr.Package ?? throw new NullReferenceException("Missing package name for NativeImpl attribute in type: " + type))
            + '.' 
            + attr.ClassName ?? throw new NullReferenceException("Missing class name for NativeImpl attribute in type: " + type);
        var impl = _types[name] = new NativeImpl(type, name);
        foreach (var method in type.GetMethods()
                     .Where(x => x.IsStatic && x.GetCustomAttribute<NativeImplAttribute>() != null))
            impl.Members[method.Name] = impl.WrapMethod(method);

        return true;
    }

    public Stack Invoke(RuntimeBase vm, Stack stack, IClassMember member)
    {
        if (!member.IsNative())
            throw new FatalException("Member is not native: " + member);
        stack.StepInto(vm, RuntimeBase.BlankInvocPos, member, stack =>
        {
            stack[Alp | Omg] = _types[member.Parent.FullName].Members[member.Name](vm, stack, (stack.Del as ObjectRef)?.Refs!); 
        }, Alp | Omg);
        return stack;
    }
}

public sealed class NativeImpl
{
    public readonly IDictionary<string, NativeImplMember> Members = new ConcurrentDictionary<string, NativeImplMember>();
    public string Name { get; }
    public Type Type { get; }

    public NativeImpl(Type type, string name)
    {
        Name = name;
        Type = type;
    }
    internal NativeImplMember WrapMethod(MethodInfo method) => (vm, stack, args) =>
    {
        return method.Invoke(null, new object[]{vm, stack, args}) as IObjectRef;
    };
}