using System.Linq;
using KScr.Lib.Bytecode;
using KScr.Lib.Exception;
using KScr.Lib.Model;
using KScr.Lib.Store;

namespace KScr.Lib.Core
{
    public sealed class CodeObject : IObject
    {
        public static readonly DummyMethod ToStringInvoc =
            new(Class.VoidType, "toString", MemberModifier.Public, Class.StringType);

        public static readonly SourcefilePosition ToStringInvocPos = new()
            { SourcefilePath = "<native>org/comroid/kscr/core/Object.kscr" };

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
        public override string ToString() => ToString(0);

        public IObjectRef? Invoke(RuntimeBase vm, Stack stack, string member, params IObject?[] args)
        {
            // try use overrides first
            if (Type.ClassMembers.FirstOrDefault(x => x.Name == member) is { } icm)
            {
                if (icm.IsStatic())
                    throw new FatalException("Static method invoked on object instance");
                var param = (icm as IMethod)?.Parameters;
                var state = State.Normal;
                // todo: use correct callLocation
                stack.StepInto(vm, ToStringInvocPos, stack.Alp!, icm, stack =>
                {
                    for (var i = 0; i < param.Count; i++)
                        vm.PutLocal(stack, param[i].Name, args.Length - 1 < i ? IObject.Null : args[i]);
                    icm.Evaluate(vm, stack.Output());
                }, StackOutput.Alp);

                return stack.Alp;
            }
            // then inherited members
            else if ((Type.BaseClass as IClass).InheritedMembers
                     .FirstOrDefault(x => x.Name == member && !x.IsAbstract()) is { } superMember)
            {
                superMember.Evaluate(vm, stack.Output(StackOutput.Alp));
                return stack.Alp;
            }
            // then primitive implementations
            else
            {
                switch (member)
                {
                    case "InternalID":
                        return Numeric.Constant(vm, ObjectId);
                    case "toString":
                        short variant;
                        if (args.Length > 0 && args[0] is Numeric num)
                            variant = num.ShortValue;
                        else throw new FatalException("Invalid argument: " + args[0]);
                        return String.Instance(vm, ToString(variant));
                    case "equals":
                        return args[0]!.ObjectId == ObjectId ? vm.ConstantTrue : vm.ConstantFalse;
                    case "getType":
                        return Type.SelfRef;
                }
            }

            throw new FatalException("Method not implemented: " + member);
        }

        public string GetKey()
        {
            return $"obj:{Type.FullName}-{ObjectId:X}";
        }
    }
}