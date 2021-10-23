using System;
using System.Collections.Concurrent;
using System.Linq;
using KScr.Lib.Bytecode;
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
            ClassTokenType.Public | ClassTokenType.Static | ClassTokenType.Final | ClassTokenType.Class, null);

        public static readonly ClassRef StringType = new ClassRef("str",
            ClassTokenType.Public | ClassTokenType.Static | ClassTokenType.Final | ClassTokenType.Class, "");

        public static readonly ClassRef NumericByteType = new ClassRef("num<byte>",
            ClassTokenType.Public | ClassTokenType.Static | ClassTokenType.Final | ClassTokenType.Class, (byte)0);

        public static readonly ClassRef NumericShortType = new ClassRef("num<short>",
            ClassTokenType.Public | ClassTokenType.Static | ClassTokenType.Final | ClassTokenType.Class, (short)0);

        public static readonly ClassRef NumericIntegerType = new ClassRef("num<int>",
            ClassTokenType.Public | ClassTokenType.Static | ClassTokenType.Final | ClassTokenType.Class, 0);

        public static readonly ClassRef NumericLongType = new ClassRef("num<long>",
            ClassTokenType.Public | ClassTokenType.Static | ClassTokenType.Final | ClassTokenType.Class, (long)0);

        public static readonly ClassRef NumericFloatType = new ClassRef("num<float>",
            ClassTokenType.Public | ClassTokenType.Static | ClassTokenType.Final | ClassTokenType.Class, (float)0);

        public static readonly ClassRef NumericDoubleType = new ClassRef("num<double>",
            ClassTokenType.Public | ClassTokenType.Static | ClassTokenType.Final | ClassTokenType.Class, (double)0);

        private ClassRef(string fullName, ClassTokenType modifier, object? constDefault)
        {
            FullName = fullName;
            Modifier = modifier;
            Default = constDefault;
        }

        public ClassTokenType Modifier { get; }
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