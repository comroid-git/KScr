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