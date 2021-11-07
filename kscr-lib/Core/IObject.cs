using KScr.Lib.Bytecode;
using KScr.Lib.Store;

namespace KScr.Lib.Core
{
    public interface IObject
    {
        public const int ToString_TypeName = -1;
        public const int ToString_ShortName = 0;
        public const int ToString_LongName = 1;
        public static readonly IObject Null = new ConstantValue(Numeric.Zero);

        long ObjectId { get; }
        bool Primitive { get; }

        Class Type { get; }

        string ToString(short variant);

        public ObjectRef? Invoke(RuntimeBase vm, string member, params IObject?[] args);
    }

    internal sealed class ConstantValue : IObject
    {
        public ConstantValue(IObject? value)
        {
            Value = value;
        }

        public IObject? Value { get; }

        public long ObjectId => Value?.ObjectId ?? long.MinValue;
        public bool Primitive => Value?.Primitive ?? true;
        public Class Type => Value?.Type ?? Class.VoidType;

        public string ToString(short variant)
        {
            return Value?.ToString(variant) ?? "null";
        }

        public ObjectRef? Invoke(RuntimeBase vm, string member, params IObject?[] args)
        {
            return Value?.Invoke(vm, member, args);
        }
    }

    public sealed class ReturnValue : IObject
    {
        public ReturnValue(IObject? value)
        {
            Value = value;
        }

        public IObject? Value { get; }

        public long ObjectId => Value?.ObjectId ?? long.MinValue;
        public bool Primitive => Value?.Primitive ?? true;
        public Class Type => Value?.Type ?? Class.VoidType;

        public string ToString(short variant)
        {
            return Value?.ToString(variant) ?? "null";
        }

        public ObjectRef? Invoke(RuntimeBase vm, string member, params IObject?[] args)
        {
            return Value?.Invoke(vm, member, args);
        }
    }

    public sealed class ThrownValue : System.Exception, IObject
    {
        public ThrownValue(IObject value)
        {
            Value = value;
        }

        public IObject Value { get; }

        public long ObjectId => Value.ObjectId;
        public bool Primitive => Value.Primitive;
        public Class Type => Value.Type;

        public string ToString(short variant)
        {
            return Value.ToString(variant);
        }

        public ObjectRef? Invoke(RuntimeBase vm, string member, params IObject?[] args)
        {
            return Value?.Invoke(vm, member, args);
        }
    }
}