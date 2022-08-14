using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using KScr.Core.Bytecode;
using KScr.Core.Exception;
using KScr.Core.Model;
using KScr.Core.Std;

namespace KScr.Core.Store;

public class ClassRef : IObjectRef
{
    private readonly IClassInstance _instance;

    public ClassRef(IClassInstance instance)
    {
        _instance = instance;
    }

    public int Length => 1;
    public bool IsPipe => false;

    public Stack ReadValue(RuntimeBase vm, Stack stack, IObject from)
    {
        stack[StackOutput.Default]!.Value = Value;
        return stack;
    }

    public Stack WriteValue(RuntimeBase vm, Stack stack, IObject to)
    {
        throw new NotSupportedException("Cannot change ClassRef");
    }

    public IEvaluable? ReadAccessor
    {
        get => throw new NotSupportedException("Must use ClassRef#Value");
        set => throw new NotSupportedException("Cannot change ClassRef");
    }

    public IEvaluable? WriteAccessor
    {
        get => null;
        set => throw new NotSupportedException("Cannot change ClassRef");
    }

    public IObject Value
    {
        get => _instance;
        set => throw new NotSupportedException("Cannot change ClassRef");
    }

    public IObject? DummyObject => Value;
    public IClassInstance Type => Class.TypeType.DefaultInstance;

    public IObject this[RuntimeBase vm, Stack stack, int i]
    {
        get => Value;
        set => throw new NotSupportedException("Cannot change ClassRef");
    }
}

public sealed class ClassStore
{
    private readonly IDictionary<string, Class> Classes = new ConcurrentDictionary<string, Class>();
    public readonly IDictionary<string, ClassRef> Instances = new ConcurrentDictionary<string, ClassRef>();

    public ClassRef? Add(IClass cls)
    {
        if (cls is Class kls)
            Classes[cls.CanonicalName] = kls;
        if (cls is Class.Instance inst)
            if (Instances.ContainsKey(inst.DetailedName))
                throw new FatalException($"Attempted to add already added class instance {inst} to cache");
            else return Instances[inst.FullDetailedName] = new ClassRef(inst);
        return null;
    }

    public IClass? FindType(RuntimeBase vm, Package package, string name)
    {
        if (name.Contains('<'))
        {
            if (Instances.FirstOrDefault(x => x.Key.EndsWith(name)) is var entry)
                return entry.Value.Value as IClassInstance;
            throw new FatalException("Cannot instantiate class instance here (invalid state)");
        }

        // todo actually we SHOULD handle instances here
        if (Classes.ContainsKey(name))
            return Classes[name];
        if (package.GetClass(vm, name.Split('.')) is { } x)
            return x;
        ;
        return package.GetOrCreateClass(vm, name);
    }
}