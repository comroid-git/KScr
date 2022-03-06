using System;
using KScr.Lib.Bytecode;
using KScr.Lib.Exception;
using KScr.Lib.Model;
using KScr.Lib.Store;

namespace KScr.Lib.Core
{
    public sealed class String : IObject
    {
        private const string ObjPrefix = "kscr.string";

        private String(RuntimeBase vm, string str)
        {
            ObjectId = vm.NextObjId("str:" + str);
            Str = str;
        }

        public string Str { get; }
        public long ObjectId { get; }

        public bool Primitive => true;
        public IClassInstance Type => Class.StringType.DefaultInstance;

        public string ToString(short variant)
        {
            return variant switch
            {
                0 => Str,
                -1 => Type.FullName,
                _ => $"String<{Str}>"
            };
        }

        public ObjectRef? Invoke(RuntimeBase vm, string member, ref ObjectRef? rev, params IObject?[] args)
        {
            switch (member)
            {
                case "toString":
                    return Instance(vm, Str);
                case "equals":
                    return args[0] is String other && Str == other.Str ? vm.ConstantTrue : vm.ConstantFalse;
                case "getType":
                    return Type.SelfRef;
                case "opPlus":
                    return OpPlus(vm, (args[0]?.Invoke(vm, "toString", ref rev)?.Value as String)?.Str ?? "null");
                case "length":
                    return Numeric.Constant(vm, Str.Length);
                default:
                    throw new NotImplementedException();
            }
        }

        private ObjectRef OpPlus(RuntimeBase vm, string other)
        {
            return Instance(vm, Str + other);
        }

        public static ObjectRef Instance(RuntimeBase vm, string str)
        {
            string key = ObjPrefix + '#' + str.GetHashCode();
            string ptr = "static-str:" + str;
            var rev = vm[VariableContext.Absolute, ptr];
            var obj = rev?.Value;
            if (obj is String strObj && strObj.Str == str)
                return rev!;
            if (obj != null)
                throw new InternalException("Unexpected object at key " + key);
            if (rev == null)
                rev = vm.ComputeObject(VariableContext.Absolute, ptr, () => new String(vm, str));
            return rev;
        }

        public override string ToString()
        {
            return ToString(0);
        }
    }
}