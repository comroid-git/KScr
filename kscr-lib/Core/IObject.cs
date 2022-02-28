using KScr.Lib.Bytecode;
using KScr.Lib.Model;
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

        IClassInstance Type { get; }

        string ToString(short variant);

        public ObjectRef? Invoke(RuntimeBase vm, string member, ref ObjectRef? rev, params IObject?[] args);
    }

    internal sealed class ConstantValue : IObject
    {
        public ConstantValue(IObject? value)
        {
            Value = value;
        }

        public IObject? Value { get; }

        public long ObjectId => Value?.ObjectId ?? long.MinValue;
        public IClassInstance Type => Value?.Type ?? Class.VoidType.DefaultInstance;

        public string ToString(short variant)
        {
            return Value?.ToString(variant) ?? "null";
        }

        public ObjectRef? Invoke(RuntimeBase vm, string member, ref ObjectRef? rev, params IObject?[] args)
        {
            return Value?.Invoke(vm, member, ref rev, args);
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
        public IClassInstance Type => Value?.Type ?? Class.VoidType.DefaultInstance;

        public string ToString(short variant)
        {
            return Value?.ToString(variant) ?? "null";
        }

        public ObjectRef? Invoke(RuntimeBase vm, string member, ref ObjectRef? rev, params IObject?[] args)
        {
            return Value?.Invoke(vm, member, ref rev, args);
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
        public IClassInstance Type => Value.Type;

        public string ToString(short variant)
        {
            return Value.ToString(variant);
        }

        public ObjectRef? Invoke(RuntimeBase vm, string member, ref ObjectRef? rev, params IObject?[] args)
        {
            return Value?.Invoke(vm, member, ref rev, args);
        }
    }
}