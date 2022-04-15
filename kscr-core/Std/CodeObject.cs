using System.Linq;
using KScr.Core.Bytecode;
using KScr.Core.Exception;
using KScr.Core.Model;
using KScr.Core.Store;

namespace KScr.Core.Std;

public sealed class CodeObject : IObject
{
    public CodeObject(RuntimeBase vm, IClassInstance type)
    {
        Type = type;
        ObjectId = vm.NextObjId(GetKey());
    }

    public bool Primitive => false;

    public long ObjectId { get; }
    public IClassInstance Type { get; }

    public string ToString(short variant)
    {
        return Type.Name + "#" + ObjectId.ToString("X");
    }

    public Stack InvokeNative(RuntimeBase vm, Stack stack, string member, params IObject?[] args)
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
                vm.NativeRunner!.Invoke(vm, stack.Channel(StackOutput.Del), this, icm)
                    .Copy(StackOutput.Omg, StackOutput.Alp);
            }
            else
            {
                stack.StepInto(vm, icm.SourceLocation, stack.Alp!, icm, stack =>
                {
                    for (var i = 0; i < (param?.Count ?? 0); i++)
                        vm.PutLocal(stack, param![i].Name, args.Length - 1 < i ? IObject.Null : args[i]);
                    icm.Evaluate(vm, stack.Output()).Copy(StackOutput.Omg, StackOutput.Alp);
                }, StackOutput.Alp);
            }

            return stack;
        }
        // then inherited members

        if ((Type.BaseClass as IClass).InheritedMembers
            .FirstOrDefault(x => x.Name == member && !x.IsAbstract()) is { } superMember
            && !(superMember.IsNative() && superMember.Parent.IsNative()))
        {
            superMember.Evaluate(vm, stack.Output());
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

    public string GetKey()
    {
        return $"obj:{Type.FullName}-{ObjectId:X}";
    }

    public override string ToString()
    {
        return ToString(0);
    }
}