using System;
using System.Collections.Concurrent;
using KScr.Lib.Core;

namespace KScr.Lib.Store
{
    public sealed class TypeStore
    {
        private readonly ConcurrentDictionary<string, TypeRef> _cache = new ConcurrentDictionary<string, TypeRef>();

        public void Clear()
        {
        }
    }

    public sealed class Package
    {
    }

    public sealed class TypeRef
    {
        public static readonly TypeRef VoidType = new TypeRef("void", null);
        public static readonly TypeRef StringType = new TypeRef("str", "");
        public static readonly TypeRef NumericByteType = new TypeRef("num<byte>", (byte)0);
        public static readonly TypeRef NumericShortType = new TypeRef("num<short>", (short)0);
        public static readonly TypeRef NumericIntegerType = new TypeRef("num<int>", 0);
        public static readonly TypeRef NumericLongType = new TypeRef("num<long>", (long)0);
        public static readonly TypeRef NumericFloatType = new TypeRef("num<float>", (float)0);
        public static readonly TypeRef NumericDoubleType = new TypeRef("num<double>", (double)0);

        private TypeRef(string fullName, object? constDefault)
        {
            FullName = fullName;
            Default = constDefault;
        } // todo fixme !!!!!!!!

        public string FullName { get; }
        public long TypeId => RuntimeBase.GetHashCode64(FullName);
        public object? Default { get; }

        public static TypeRef NumericType(NumericMode mode)
        {
            return mode switch
            {
                NumericMode.Byte => NumericByteType,
                NumericMode.Short => NumericShortType,
                NumericMode.Int => NumericIntegerType,
                NumericMode.Long => NumericLongType,
                NumericMode.Float => NumericFloatType,
                NumericMode.Double => NumericDoubleType,
                _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
            };
        }

        public bool CanHold(TypeRef? type)
        {
            return FullName == "void" || Equals(type);
        }

        public override string ToString()
        {
            return FullName;
        }

        #region Equality Overrides

        public override bool Equals(object? obj)
        {
            return obj is TypeRef other && FullName == other.FullName;
        }

        public override int GetHashCode()
        {
            return FullName.GetHashCode();
        }

        #endregion
    }
}