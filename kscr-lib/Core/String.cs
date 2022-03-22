using System;
using KScr.Lib.Bytecode;
using KScr.Lib.Exception;
using KScr.Lib.Model;
using KScr.Lib.Store;

namespace KScr.Lib.Core
{
    public sealed class String : IObject
    {
        private String(RuntimeBase vm, string str)
        {
            Str = str;
            ObjectId = vm.NextObjId(GetKey());
        }

        public string Str { get; }

        public bool Primitive => true;
        public long ObjectId { get; }
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

        public IObjectRef? Invoke(RuntimeBase vm, Stack stack, string member, params IObject?[] args)
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
                    return OpPlus(vm, stack, (args[0]?.Invoke(vm, stack, "toString")?.Value as String)?.Str ?? "null");
                case "length":
                    return Numeric.Constant(vm, Str.Length);
                default:
                    throw new NotImplementedException();
            }
        }

        public string GetKey()
        {
            return CreateKey(Str);
        }

        private static string CreateKey(string str)
        {
            return $"str:\"{str}\"";
        }

        private IObjectRef OpPlus(RuntimeBase vm, Stack stack, string other)
        {
            return Instance(vm, Str + other);
        }

        public static IObjectRef Instance(RuntimeBase vm, string str)
        {
            string key = CreateKey(str);
            var rev = vm[RuntimeBase.MainStack.KeyGen, VariableContext.Absolute, key];
            var obj = rev?.Value;
            if (obj is String strObj && strObj.Str == str)
                return rev!;
            if (obj != null)
                throw new FatalException("Unexpected object at key " + key);
            if (rev == null)
                rev = vm.ComputeObject(RuntimeBase.MainStack, VariableContext.Absolute, key, () => new String(vm, str));
            return rev;
        }

        public override string ToString()
        {
            return ToString(0);
        }
    }
}