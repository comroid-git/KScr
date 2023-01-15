using KScr.Core.Bytecode;
using KScr.Core.Exception;
using KScr.Core.Model;
using KScr.Core.Store;

namespace KScr.Core.System;

public class DummySequence_Finite : NativeObj
{
    private readonly IObject[] _values;
    private int _cursor = -1;

    public DummySequence_Finite(RuntimeBase vm, ITypeInfo t, params IObject[] values) : base(vm)
    {
        _values = values;
        Type = Class.Sequence.CreateInstance(vm, Class.Sequence, t);
    }

    public override IClassInstance Type { get; }

    public override string GetKey()
    {
        return "dummy-" + Type.FullDetailedName + $"[{_values.Length}]";
    }

    public override Stack InvokeNative(RuntimeBase vm, Stack stack, string member, params IObject?[] args)
    {
        switch (member)
        {
            case "finite":
                stack[StackOutput.Default] = vm.ConstantTrue;
                break;
            case "length":
                stack[StackOutput.Default] = Numeric.Constant(vm, _values.Length);
                break;
            case "hasNext":
                stack[StackOutput.Default] = _values.Length + 1 > _cursor ? vm.ConstantTrue : vm.ConstantFalse;
                break;
            case "next":
                if (_values.Length + 1 <= _cursor)
                    throw new FatalException("Sequence has no next element");
                var val = _values[++_cursor];
                stack[StackOutput.Default] = new ObjectRef(val.Type, val);
                break;
            default: throw new FatalException("Native member " + member + " not implemented");
        }

        return stack;
    }
}

public class DummySequence_Infinite : NativeObj
{
    private readonly StatementComponent _lambda;
    private readonly IObject _sequence;

    public DummySequence_Infinite(RuntimeBase vm, ITypeInfo t, IObject sequence, StatementComponent lambda) : base(vm)
    {
        _sequence = sequence;
        _lambda = lambda;
        Type = Class.Sequence.CreateInstance(vm, Class.Sequence, t);
    }

    public override IClassInstance Type { get; }

    public override string GetKey()
    {
        return "dummy-" + Type.FullDetailedName + "[unknown length]";
    }

    public override Stack InvokeNative(RuntimeBase vm, Stack stack, string member, params IObject?[] args)
    {
        switch (member)
        {
            case "finite":
                stack[StackOutput.Default] = vm.ConstantFalse;
                break;
            case "length":
                stack[StackOutput.Default] = Numeric.Constant(vm, -1);
                break;
            case "hasNext":
                Class.Sequence.DeclaredMembers["hasNext"].Invoke(vm, stack.Output(), _sequence)
                    .Copy(StackOutput.Omg, StackOutput.Alp | StackOutput.Omg);
                break;
            case "next":
                //todo Needs testing
                var res = Class.Sequence.DeclaredMembers["next"].Invoke(vm, stack, _sequence)
                    .Copy(StackOutput.Omg);
                stack[StackOutput.Default] = stack.StepIntoLambda(vm, stack.Output(), _lambda, res!.Value);
                break;
            default: throw new FatalException("Native member " + member + " not implemented");
        }

        return stack;
    }
}