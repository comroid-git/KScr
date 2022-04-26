using System.Linq;
using KScr.Core.Bytecode;
using KScr.Core.Exception;
using KScr.Core.Model;
using KScr.Core.Store;

namespace KScr.Core.Std;

public sealed class CodeObject : NativeObj
{
    public CodeObject(RuntimeBase vm, IClassInstance type) : base(vm)
    {
        Type = type;
    }

    public bool Primitive => false;

    public override IClassInstance Type { get; }

    public override string ToString(short variant)
    {
        return $"{Type.Name}#{ObjectId:X16}";
    }

    public override Stack InvokeNative(RuntimeBase vm, Stack stack, string member, params IObject?[] args)
    {
        // try use overrides first
        if (Type.ClassMembers.FirstOrDefault(x => x.Name == member) is { } icm
            && !(icm.IsNative() && icm.Parent.IsNative()))
        {
            if (icm.IsStatic())
                throw new FatalException("Static method invoked on object instance");
            var param = (icm as IMethod)?.Parameters;
            // todo: use correct callLocation
            if (icm.IsNative())
            {
                stack[StackOutput.Del] = new ObjectRef(Class.VoidType.DefaultInstance, args.Length);
                for (var i = 0; i < args.Length; i++)
                    stack[StackOutput.Del]![vm, stack, i] = args[i];
                vm.NativeRunner!.InvokeMember(vm, stack.Channel(StackOutput.Del), this, icm).Copy(StackOutput.Omg, StackOutput.Alp);
            }
            else
            {
                stack[StackOutput.Default] = icm.Invoke(vm, stack.Output(), this, args: args).Copy(StackOutput.Omg, StackOutput.Alp);
            }

            return stack;
        }
        // then inherited members

        if ((Type.BaseClass as IClass).InheritedMembers
            .FirstOrDefault(x => x.Name == member && !x.IsAbstract()) is { } superMember
            && !(superMember.IsNative() && superMember.Parent.IsNative()))
        {
            superMember.Invoke(vm, stack.Output(), stack[StackOutput.Default]![vm,stack,0], args: args);
            return stack;
        }
        // then primitive implementations

        switch (member)
        {
            case "InternalID":
                stack[StackOutput.Default] = Numeric.Constant(vm, ObjectId);
                break;
            case "toString":
                short variant;
                if (args.Length > 0 && args[0] is Numeric num)
                    variant = num.ShortValue;
                else throw new FatalException("Invalid argument: " + args[0]);
                stack[StackOutput.Default] = String.Instance(vm, ToString(variant));
                break;
            case "equals":
                stack[StackOutput.Default] = args[0]!.ObjectId == ObjectId ? vm.ConstantTrue : vm.ConstantFalse;
                break;
            case "getType":
                stack[StackOutput.Default] = Type.SelfRef;
                break;
            default: throw new FatalException("Method not implemented: " + member);
        }

        return stack;
    }

    public override string GetKey()
    {
        return $"obj:{Type.FullName}-{ObjectId:X}";
    }

    public override string ToString()
    {
        return ToString(0);
    }
}