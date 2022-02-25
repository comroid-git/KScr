using System;
using KScr.Lib.Bytecode;
using KScr.Lib.Exception;
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
        public Class Type => Class.StringType;

        public string ToString(short variant)
        {
            return variant switch
            {
                0 => Str,
                -1 => Type.FullName,
                _ => $"String<{Str}>"
            };
        }

        public ObjectRef? Invoke(RuntimeBase vm, string member, params IObject?[] args)
        {
            if (member.StartsWith("op") && args[0] != null)
                switch (member.Substring("op".Length))
                {
                    case "Plus":
                        return OpPlus(vm, args[0]!.ToString(0));
                }

            switch (member)
            {
                case "toString":
                    return Instance(vm, Str);
                case "length":
                    return Numeric.Constant(vm, Str.Length);
                default:
                    throw new NotImplementedException();
            }
        }

        private ObjectRef OpPlus(RuntimeBase vm, string other) => Instance(vm, Str + other);

        public static ObjectRef Instance(RuntimeBase vm, string str)
        {
            string key = ObjPrefix + '#' + str.GetHashCode();
            string ptr = "str-literal:" + str;
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