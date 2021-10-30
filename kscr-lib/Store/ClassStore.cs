using System;
using KScr.Lib.Core;
using KScr.Lib.Model;

namespace KScr.Lib.Store
{
    public sealed class ClassStore
    {
        public void Clear()
        {
        }
    }

    [Obsolete]
    public sealed class ClassRef : IClassRef
    {
        public static readonly ClassRef VoidType = new ClassRef("void",
            TokenType.Public | TokenType.Static | TokenType.Final | TokenType.Class, null);

        public static readonly ClassRef StringType = new ClassRef("str",
            TokenType.Public | TokenType.Static | TokenType.Final | TokenType.Class, "");

        public static readonly ClassRef NumericByteType = new ClassRef("num<byte>",
            TokenType.Public | TokenType.Static | TokenType.Final | TokenType.Class, (byte)0);

        public static readonly ClassRef NumericShortType = new ClassRef("num<short>",
            TokenType.Public | TokenType.Static | TokenType.Final | TokenType.Class, (short)0);

        public static readonly ClassRef NumericIntegerType = new ClassRef("num<int>",
            TokenType.Public | TokenType.Static | TokenType.Final | TokenType.Class, 0);

        public static readonly ClassRef NumericLongType = new ClassRef("num<long>",
            TokenType.Public | TokenType.Static | TokenType.Final | TokenType.Class, (long)0);

        public static readonly ClassRef NumericFloatType = new ClassRef("num<float>",
            TokenType.Public | TokenType.Static | TokenType.Final | TokenType.Class, (float)0);

        public static readonly ClassRef NumericDoubleType = new ClassRef("num<double>",
            TokenType.Public | TokenType.Static | TokenType.Final | TokenType.Class, (double)0);

        private ClassRef(string fullName, TokenType modifier, object? constDefault)
        {
            FullName = fullName;
            Modifier = modifier;
            Default = constDefault;
        }

        public TokenType Modifier { get; }
        public string FullName { get; }
        public long TypeId => RuntimeBase.GetHashCode64(FullName);
        public object? Default { get; }

        public static ClassRef NumericType(NumericMode mode)
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

        public override string ToString()
        {
            return FullName;
        }

        #region Equality Overrides

        public override bool Equals(object? obj)
        {
            return obj is ClassRef other && FullName == other.FullName;
        }

        public override int GetHashCode()
        {
            return FullName.GetHashCode();
        }

        #endregion
    }
}