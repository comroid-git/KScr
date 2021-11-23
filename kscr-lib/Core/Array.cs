using System;
using System.Linq;
using KScr.Lib.Bytecode;
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
            ObjectId = vm.NextObjId("array");
        }

        public ObjectRef[] List { get; }
        public long ObjectId { get; }
        public bool Primitive => true;
        public Class Type => Class.ArrayType;
        public string ToString(short variant) => string.Join(", ", List.Select(it => it.Value?.ToString(variant)));

        public ObjectRef? Invoke(RuntimeBase vm, string member, params IObject?[] args)
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
    }
}