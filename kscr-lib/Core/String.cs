using System;
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
        public TypeRef Type => TypeRef.StringType;

        public string ToString(short variant)
        {
            return variant switch
            {
                0 => Str,
                -1 => Type.FullName,
                _ => $"String<{Str}>"
            };
        }

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