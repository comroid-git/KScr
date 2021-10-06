using System;
using KScr.Lib.Core;

namespace KScr.Lib.VM
{
    public sealed class TypeStore
    { // todo fixme !!!!!!!!
        public void Clear() {}
    }
    
    public sealed class TypeRef
    { // todo fixme !!!!!!!!
        public string FullName { get; }
        public object? Default { get; }

        public static readonly TypeRef VoidType = new TypeRef("void", null);
        public static readonly TypeRef StringType = new TypeRef("str", "");
        public static readonly TypeRef NumericByteType = new TypeRef("num<byte>", (byte)0);
        public static readonly TypeRef NumericShortType = new TypeRef("num<short>", (short)0);
        public static readonly TypeRef NumericIntegerType = new TypeRef("num<int>", (int)0);
        public static readonly TypeRef NumericLongType = new TypeRef("num<long>", (long)0);
        public static readonly TypeRef NumericFloatType = new TypeRef("num<float>", (float)0);
        public static readonly TypeRef NumericDoubleType = new TypeRef("num<double>", (double)0);

        private TypeRef(string fullName, object? constDefault)
        {
            FullName = fullName;
            Default = constDefault;
        }

        public static TypeRef NumericType(NumericMode mode) => mode switch
        {
            NumericMode.Byte => NumericByteType,
            NumericMode.Short => NumericShortType,
            NumericMode.Int => NumericIntegerType,
            NumericMode.Long => NumericLongType,
            NumericMode.Float => NumericFloatType,
            NumericMode.Double => NumericDoubleType,
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };

        #region Equality Overrides
        public override bool Equals(object? obj) => obj is TypeRef other && FullName == other.FullName;
        public override int GetHashCode() => FullName.GetHashCode();
        #endregion

        public bool CanHold(TypeRef? type) => FullName == "void" || Equals(type);

        public override string ToString() => FullName;
    }
}