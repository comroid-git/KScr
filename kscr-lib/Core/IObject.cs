using KScr.Lib.Bytecode;
using KScr.Lib.Exception;
using KScr.Lib.Model;
using KScr.Lib.Store;

namespace KScr.Lib.Core
{
    public interface IObject
    {
        public const int ToString_TypeName = -1;
        public const int ToString_ShortName = 0;
        public const int ToString_LongName = 1;
        public static readonly IObject Null = new VoidValue();

        long ObjectId { get; }

        IClassInstance Type { get; }

        string ToString(short variant);

        public IObjectRef? Invoke(RuntimeBase vm, Stack stack, string member, params IObject?[] args);

        public string GetKey();
    }

    internal sealed class VoidValue : IObject
    {
        public long ObjectId => 0;
        public IClassInstance Type => Class.VoidType.DefaultInstance;

        public string ToString(short variant)
        {
            return "null";
        }

        public IObjectRef? Invoke(RuntimeBase vm, Stack stack, string member, params IObject?[] args)
        {
            switch (member)
            {
                case "toString":
                    return String.Instance(vm, stack, "null");
                case "equals":
                    return args[0] is VoidValue || args[0] is Numeric num && num.ByteValue != 0
                        ? vm.ConstantTrue
                        : vm.ConstantFalse;
                case "getType":
                    return Type.SelfRef;
                default:
                    throw new FatalException("NullPointerException");
            }
        }

        public string GetKey()
        {
            return "static-void:null";
        }
    }
}