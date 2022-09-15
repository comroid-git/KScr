using KScr.Core.Exception;
using KScr.Core.Model;
using KScr.Core.Store;

namespace KScr.Core.Std;

public class DummySequence : NativeObj
{
    private readonly IObject[] _values;
    private int _cursor = -1;

    public DummySequence(RuntimeBase vm, ITypeInfo t, params IObject[] values) : base(vm)
    {
        _values = values;
        Type = Class.Sequence.CreateInstance(vm, Class.Sequence, t);
    }

    public override IClassInstance Type { get; }
    public override string GetKey() => "dummy-" + Type.FullDetailedName + $"[{_values.Length}]";

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