using System;
using System.Collections.Generic;
using KScr.Lib.VM;

namespace KScr.Lib.Core
{
    public sealed class String : IObject
    {
        private long _objId;
        private const string ObjPrefix = "kscr.string";

        private String(VirtualMachine vm, string str)
        {
            _objId = vm.NextObjId("str:" + str);
            Str = str;
        }

        public string Str { get; }
        public long ObjectId => _objId;
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

        public static String Instance(VirtualMachine vm, string str)
        {
            string key = ObjPrefix + '#' + str.GetHashCode();
            string ptr = "str-literal:" + str;
            var rev = vm[VariableContext.Absolute, ptr];
            var obj = rev?.Value;
            if (obj is String strObj && strObj.Str == str)
                return strObj;
            if (obj != null)
                throw new Exception("Unexpected object at key " + key);
            if (rev != null)
                return ((rev.Value = new String(vm, str)) as String)!;
            throw new NullReferenceException("Pointer not found: " + ptr);
        }

        public override string ToString()
        {
            return ToString(0);
        }
    }
}