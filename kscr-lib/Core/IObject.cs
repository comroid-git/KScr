using System;
using KScr.Lib.VM;

namespace KScr.Lib.Core
{
    public interface IObject
    {
        public const int ToString_TypeName = -1;
        public const int ToString_ShortName = 0;
        public const int ToString_LongName = 1;
        public static readonly IObject Nil = new ConstantValue(Numeric.Zero);

        long ObjectId { get; }
        bool Primitive { get; }
        
        TypeRef Type { get; }

        string ToString(short variant);
    }

    internal sealed class ConstantValue : IObject
    {
        public ConstantValue(IObject? value)
        {
            Value = value;
        }

        public IObject? Value { get; }

        public long ObjectId => Value?.ObjectId ?? UInt32.MinValue;
        public bool Primitive => Value?.Primitive ?? true;
        public TypeRef Type => Value?.Type ?? TypeRef.VoidType;

        public string ToString(short variant) => Value?.ToString(variant) ?? "null";
    }

    public sealed class ReturnValue : IObject
    {
        public ReturnValue(IObject? value)
        {
            Value = value;
        }

        public IObject? Value { get; }

        public long ObjectId => Value?.ObjectId ?? UInt32.MinValue;
        public bool Primitive => Value?.Primitive ?? true;
        public TypeRef Type => Value?.Type ?? TypeRef.VoidType;

        public string ToString(short variant) => Value?.ToString(variant) ?? "null";
    }

    public sealed class ThrownValue : Exception, IObject
    {
        public ThrownValue(IObject? value)
        {
            Value = value;
        }

        public IObject? Value { get; }

        public long ObjectId => Value?.ObjectId ?? UInt32.MinValue;
        public bool Primitive => Value?.Primitive ?? true;
        public TypeRef Type => Value?.Type ?? TypeRef.VoidType;

        public string ToString(short variant) => Value?.ToString(variant) ?? "null";
    }
}