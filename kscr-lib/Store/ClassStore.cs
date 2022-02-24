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
        private IDictionary<long, Class.Instance> _cache = new ConcurrentDictionary<long, Class.Instance>();

        public IClassInstance FindType(string name)
        {
            var cls = Package.RootPackage.GetClass(name.Split("."));
            if (cls != null)
                return cls;
            return FindType(RuntimeBase.GetHashCode64(name));
        }

        public IClassInstance FindType(long typeId) => _cache[typeId];

        public void Clear()
        {
        }
    }
}