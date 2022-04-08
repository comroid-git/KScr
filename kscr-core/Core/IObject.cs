using KScr.Core.Bytecode;
using KScr.Core.Exception;
using KScr.Core.Model;
using KScr.Core.Store;

namespace KScr.Core.Core
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
        
        public Stack Invoke(RuntimeBase vm, Stack stack, string member, params IObject?[] args);

        public string GetKey();
    }

    internal sealed class VoidValue : IObject
    {
        public long ObjectId => 0;
        public IClassInstance Type => Class.VoidType.DefaultInstance;

        public string ToString(short variant) => "null";
        public override string ToString() => ToString(0);

        public Stack Invoke(RuntimeBase vm, Stack stack, string member, params IObject?[] args)
        {
            switch (member)
            {
                case "toString":
                    stack[StackOutput.Default] = String.Instance(vm, "null");
                    break;
                case "equals":
                    stack[StackOutput.Default] = args[0] is VoidValue || args[0] is Numeric num && num.ByteValue != 0
                        ? vm.ConstantTrue
                        : vm.ConstantFalse;
                    break;
                case "getType":
                    stack[StackOutput.Default] = Type.SelfRef;
                    break;
                default:
                    throw new FatalException("NullPointerException");
            }

            return stack;
        }

        public string GetKey()
        {
            return "void:null";
        }
    }
}