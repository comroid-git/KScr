using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using KScr.Lib.Bytecode;
using KScr.Lib.Core;
using KScr.Lib.Model;

namespace KScr.Lib.Store
{
    public sealed class ClassStore
    {
        private IDictionary<long, Class> _cache = new ConcurrentDictionary<long, Class>();

        public Class FindType(long typeId) => _cache[typeId];
        
        public void Clear()
        {
        }
    }

    [Obsolete]
    public sealed class ClassRef : IClassRef
    {
        public static readonly ClassRef VoidType = new ClassRef("void",
            MemberModifier.Public | MemberModifier.Static | MemberModifier.Final, null);

        public static readonly ClassRef StringType = new ClassRef("str",
            MemberModifier.Public | MemberModifier.Static | MemberModifier.Final, "");

        public static readonly ClassRef NumericByteType = new ClassRef("num<byte>",
            MemberModifier.Public | MemberModifier.Static | MemberModifier.Final, (byte)0);

        public static readonly ClassRef NumericShortType = new ClassRef("num<short>",
            MemberModifier.Public | MemberModifier.Static | MemberModifier.Final, (short)0);

        public static readonly ClassRef NumericIntegerType = new ClassRef("num<int>",
            MemberModifier.Public | MemberModifier.Static | MemberModifier.Final, 0);

        public static readonly ClassRef NumericLongType = new ClassRef("num<long>",
            MemberModifier.Public | MemberModifier.Static | MemberModifier.Final, (long)0);

        public static readonly ClassRef NumericFloatType = new ClassRef("num<float>",
            MemberModifier.Public | MemberModifier.Static | MemberModifier.Final, (float)0);

        public static readonly ClassRef NumericDoubleType = new ClassRef("num<double>",
            MemberModifier.Public | MemberModifier.Static | MemberModifier.Final, (double)0);

        private ClassRef(string fullName, MemberModifier modifier, object? constDefault)
        {
            FullName = fullName;
            Modifier = modifier;
            Default = constDefault;
        }

        public MemberModifier Modifier { get; }
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