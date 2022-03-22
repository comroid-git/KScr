using System;
using System.Linq;
using KScr.Lib.Bytecode;
using KScr.Lib.Model;
using KScr.Lib.Store;

namespace KScr.Lib.Core
{
    [Obsolete]
    public sealed class Tuple : IObject
    {
        public Tuple(RuntimeBase vm, int len) : this(vm, new ObjectRef[len])
        {
        }

        public Tuple(RuntimeBase vm, ObjectRef[] arr)
        {
            Arr = arr;
            ObjectId = vm.NextObjId(GetKey());
        }

        public ObjectRef[] Arr { get; }
        public bool Primitive => true;
        public long ObjectId { get; }
        public IClassInstance Type => Class.TupleType.DefaultInstance;

        public string ToString(short variant)
        {
            return string.Join(", ", Arr.Select(it => it.Value?.ToString(variant)));
        }

        public IObjectRef? Invoke(RuntimeBase vm, Stack stack, string member, params IObject?[] args)
        {
            switch (member)
            {
                case "length":
                    return Numeric.Constant(vm, Arr.Length);
                case "get":
                    if (args[0] is Numeric num)
                        return Arr[num.IntValue];
                    break;
            }

            return null;
        }

        public string GetKey()
        {
            return $"tuple<{Type.FullName}>[{Arr.Length}]";
        }
    }
}