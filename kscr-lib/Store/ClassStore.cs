using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using KScr.Lib.Bytecode;
using KScr.Lib.Core;
using KScr.Lib.Exception;
using KScr.Lib.Model;

namespace KScr.Lib.Store
{
    public class ClassRef : IObjectRef
    {
        private IClassInstance _instance;

        public ClassRef(IClassInstance instance)
        {
            _instance = instance;
        }

        public int Length => 1;
        public bool IsPipe => false;
        public IEvaluable? ReadAccessor
        {
            get => null;
            set => throw new NotSupportedException("Cannot change ClassRef");
        }

        public IEvaluable? WriteAccessor
        {
            get => null;
            set => throw new NotSupportedException("Cannot change ClassRef");
        }

        public IObject Value
        {
            get => _instance;
            set => throw new NotSupportedException("Cannot change ClassRef");
        }

        public IClassInstance Type => Class.TypeType.DefaultInstance;

        public IObject this[RuntimeBase vm, Stack stack, int i]
        {
            get => Value;
            set => throw new NotSupportedException("Cannot change ClassRef");
        }
    }

    public sealed class ClassStore
    {
        private readonly IDictionary<string, Class> Classes = new ConcurrentDictionary<string, Class>();
        public readonly IDictionary<string, ClassRef> Instances = new ConcurrentDictionary<string, ClassRef>();

        public ClassRef? Add(IClass cls)
        {
            if (cls is Class kls)
                Classes[cls.CanonicalName] = kls;
            if (cls is Class.Instance inst)
                if (Instances.ContainsKey(inst.DetailedName))
                    throw new FatalException($"Attempted to add already added class instance {inst} to cache");
                else return Instances[cls.CanonicalName] = new ClassRef(inst);
            return null;
        }

        public IClass? FindType(RuntimeBase vm, Package package, string name)
        {
            if (name.Contains('<'))
                throw new FatalException("Cannot instantiate class instance here (invalid state)");
            if (package.GetClass(vm, name.Split('.')) is { } x)
                return x;
            else
                ;
            return package.GetOrCreateClass(vm, name);
        }

        public void Clear()
        {
        }
    }
}