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

    public IClassInstance Type => Class.TypeType.DefaultInstance;

    public IObject this[RuntimeBase vm, Stack stack, int i]
    {
        get => Value;
        set => throw new NotSupportedException("Cannot change ClassRef");
    }
}