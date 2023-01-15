using System;
using KScr.Core.Exception;
using KScr.Core.Model;
using KScr.Core.Store;

namespace KScr.Core.System;

public interface IObject
{
    [Obsolete] public const int ToString_ShortName = ToString_ParseableName;

    [Obsolete] public const int ToString_LongName = ToString_FullDetailedName;

    public const short ToString_ParseableName = 0;
    public const short ToString_TypeName = 1;
    public const short ToString_Name = 2;
    public const short ToString_FullName = 3;
    public const short ToString_DetailedName = 4;
    public const short ToString_FullDetailedName = 5;
    public static readonly IObject Null = new VoidValue();

    long ObjectId { get; }

    IClassInstance Type { get; }

    string ToString(short variant);

    public Stack InvokeNative(RuntimeBase vm, Stack stack, string member, params IObject?[] args);

    public string GetKey();
}

internal sealed class VoidValue : IObject
{
    public long ObjectId => 0;
    public IClassInstance Type => Class.VoidType.DefaultInstance;

    public string ToString(short variant)
    {
        return "null";
    }

    public Stack InvokeNative(RuntimeBase vm, Stack stack, string member, params IObject?[] args)
    {
        switch (member)
        {
            case "toString":
                stack[StackOutput.Default] = String.Instance(vm, "null");
                break;
            case "equals":
                stack[StackOutput.Default] = args[0] is VoidValue || (args[0] is Numeric num && num.ByteValue != 0)
                    ? vm.ConstantTrue
                    : vm.ConstantFalse;
                break;
            case "getType":
                stack[StackOutput.Default] = Type.SelfRef;
                break;
            default:
                throw new FatalException("NullPointerException");
        }

        return stack;
    }

    public string GetKey()
    {
        return "void:null";
    }

    public override string ToString()
    {
        return ToString(0);
    }
}