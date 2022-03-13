using System;
using System.Linq;
using KScr.Lib.Bytecode;
using KScr.Lib.Model;
using KScr.Lib.Store;

namespace KScr.Lib.Core
{
    [Obsolete]
    public sealed class Array : IObject
    {
        public Array(RuntimeBase vm, int len) : this(vm, new ObjectRef[len])
        {
        }

        public Array(RuntimeBase vm, ObjectRef[] list)
        {
            List = list;
            ObjectId = vm.NextObjId(GetKey());
        }

        public ObjectRef[] List { get; }
        public bool Primitive => true;
        public long ObjectId { get; }
        public IClassInstance Type => Class.ArrayType.DefaultInstance;

        public string ToString(short variant)
        {
            return string.Join(", ", List.Select(it => it.Value?.ToString(variant)));
        }

        public ObjectRef? Invoke(RuntimeBase vm, string member, ref ObjectRef? rev, params IObject?[] args)
        {
            switch (member)
            {
                case "length":
                    return Numeric.Constant(vm, List.Length);
                case "get":
                    if (args[0] is Numeric num)
                        return List[num.IntValue];
                    break;
            }

            return null;
        }

        public string GetKey()
        {
            return $"{Type.Name}[{List.Length}]";
        }
    }
}