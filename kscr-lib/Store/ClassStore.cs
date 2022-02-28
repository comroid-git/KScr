using System.Collections.Concurrent;
using System.Collections.Generic;
using KScr.Lib.Bytecode;
using KScr.Lib.Model;

namespace KScr.Lib.Store
{
    public sealed class ClassStore
    {
        private readonly IDictionary<long, Class.Instance> _cache = new ConcurrentDictionary<long, Class.Instance>();

        public IClassInstance? FindType(RuntimeBase vm, Package package, string name)
        {
            if (name.Contains(".") && package.GetClass(vm, name.Split("."))?.DefaultInstance is {} x)
                return x;
            var cls = package.GetOrCreateClass(vm, name);
            if (cls != null)
                return cls.DefaultInstance;
            return FindType(RuntimeBase.GetHashCode64(package.FullName + '.' + name));
        }

        public IClassInstance? FindType(long typeId)
        {
            if (!_cache.ContainsKey(typeId))
                return null;
            return _cache[typeId];
        }

        public long Add(Class.Instance classInstance)
        {
            long key = RuntimeBase.GetHashCode64(classInstance.FullName);
            _cache[key] = classInstance;
            return key;
        }

        public void Clear()
        {
        }
    }
}