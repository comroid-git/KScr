﻿using System;
using System.Linq;
using KScr.Core.Model;
using KScr.Core.Store;

namespace KScr.Core.Std;

[Obsolete]
public sealed class ArrayObj : IObject
{
    public ArrayObj(RuntimeBase vm, int len) : this(vm, new ObjectRef[len])
    {
    }

    public ArrayObj(RuntimeBase vm, ObjectRef[] arr)
    {
        Arr = arr;
        ObjectId = vm.NextObjId(GetKey());
    }

    public ObjectRef[] Arr { get; }
    public bool Primitive => true;
    public long ObjectId { get; }
    public IClassInstance Type => Class.ArrayType.DefaultInstance;

    public string ToString(short variant)
    {
        return string.Join(", ", Arr.Select(it => it.Value?.ToString(variant)));
    }

    public Stack InvokeNative(RuntimeBase vm, Stack stack, string member, params IObject?[] args)
    {
        switch (member)
        {
            case "length":
                stack[StackOutput.Default] = Numeric.Constant(vm, Arr.Length);
                break;
            case "get":
                if (args[0] is Numeric num)
                    stack[StackOutput.Default] = Arr[num.IntValue];
                break;
            default:
                throw new NotImplementedException();
        }

        return stack;
    }

    public string GetKey()
    {
        return $"array<{Type.FullName}>[{Arr.Length}]";
    }
}