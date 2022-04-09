using System;
using KScr.Core.Exception;
using KScr.Core.Model;
using KScr.Core.Store;

namespace KScr.Core.Std;

public sealed class String : IObject
{
    private String(RuntimeBase vm, string str)
    {
        Str = str;
        ObjectId = vm.NextObjId(GetKey());
    }

    public string Str { get; }

    public bool Primitive => true;
    public long ObjectId { get; }
    public IClassInstance Type => Class.StringType.DefaultInstance;

    public string ToString(short variant)
    {
        return variant switch
        {
            0 => Str,
            -1 => Type.FullName,
            _ => $"String<{Str}>"
        };
    }

    public Stack Invoke(RuntimeBase vm, Stack stack, string member, params IObject?[] args)
    {
        switch (member)
        {
            case "toString":
                stack[StackOutput.Default] = Instance(vm, Str);
                break;
            case "equals":
                stack[StackOutput.Default] =
                    args[0] is String other1 && Str == other1.Str ? vm.ConstantTrue : vm.ConstantFalse;
                break;
            case "getType":
                stack[StackOutput.Default] = Type.SelfRef;
                break;
            case "opPlus":
                var other2 = args[0]?.Invoke(vm, stack.Output(), "toString").Copy(StackOutput.Alp, StackOutput.Bet);
                stack[StackOutput.Default] = OpPlus(vm, stack, (other2?.Value as String)?.Str ?? "null");
                break;
            case "length":
                stack[StackOutput.Default] = Numeric.Constant(vm, Str.Length);
                break;
            default:
                throw new NotImplementedException();
        }

        return stack;
    }

    public string GetKey()
    {
        return CreateKey(Str);
    }

    private static string CreateKey(string str)
    {
        return $"str:\"{str}\"";
    }

    private IObjectRef OpPlus(RuntimeBase vm, Stack stack, string other)
    {
        return Instance(vm, Str + other);
    }

    public static IObjectRef Instance(RuntimeBase vm, string str)
    {
        var key = CreateKey(str);
        var rev = vm[RuntimeBase.MainStack, VariableContext.Absolute, key];
        var obj = rev?.Value;
        if (obj is String strObj && strObj.Str == str)
            return rev!;
        if (obj != null)
            throw new FatalException("Unexpected object at key " + key);
        if (rev == null)
            rev = vm.ComputeObject(RuntimeBase.MainStack, VariableContext.Absolute, key, () => new String(vm, str));
        return rev;
    }

    public override string ToString()
    {
        return ToString(0);
    }
}