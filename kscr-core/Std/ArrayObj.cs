using System;
using System.Linq;
using KScr.Core.Model;
using KScr.Core.Store;

namespace KScr.Core.Std;

public sealed class ArrayObj : NativeObj
{
    public ArrayObj(RuntimeBase vm, ITypeInfo t, int len) : base(vm)
    {
        Type = Class.ArrayType.CreateInstance(vm, typeParameters: t);
        Arr = new ObjectRef[len];
    }

    public ObjectRef[] Arr { get; }
    public override IClassInstance Type { get; }

    public override string ToString(short variant)
    {
        return string.Join(", ", Arr.Select(it => it.Value?.ToString(variant)));
    }

    public override Stack InvokeNative(RuntimeBase vm, Stack stack, string member, params IObject?[] args)
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

    public override string GetKey()
    {
        return $"array<{Type.FullName}>[{Arr.Length}]";
    }
}