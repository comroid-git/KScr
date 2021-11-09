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
}