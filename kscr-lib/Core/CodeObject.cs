﻿using System.Linq;
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

        public ObjectRef? Invoke(RuntimeBase vm, string member, ref ObjectRef? rev, params IObject?[] args)
        {
            // try use overrides first
            if (Type.ClassMembers.FirstOrDefault(x => x.Name == member) is { } icm)
            {
                if (icm.IsStatic())
                    throw new InternalException("Static method invoked on object instance");
                IRuntimeSite? site = icm;
                var param = (icm as IMethod)?.Parameters!;
                var state = State.Normal;
                // todo: use correct callLocation
                vm.Stack.StepInto(vm, ToStringInvocPos, rev!, ToStringInvoc, ref rev, _rev =>
                {
                    for (var i = 0; i < args.Length; i++)
                        vm.PutLocal(param[i].Name, args[i]);
                    do
                    {
                        site = site.Evaluate(vm, ref state, ref _rev!);
                    } while (state == State.Normal && site != null);

                    return _rev;
                });

                return rev;
            }
            else if ((Type.BaseClass as IClass).InheritedMembers
                     .FirstOrDefault(x => x.Name == member && !x.IsAbstract()) is { } superMember)
            {
                var state = State.Normal;
                superMember.Evaluate(vm, ref state, ref rev);
                return rev;
            }
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
                        else throw new InternalException("Invalid argument: " + args[0]);
                        return String.Instance(vm, ToString(variant));
                    case "equals":
                        return args[0]!.ObjectId == ObjectId ? vm.ConstantTrue : vm.ConstantFalse;
                    case "getType":
                        return Type.SelfRef;
                }
            }

            throw new InternalException("Method not implemented: " + member);
        }

        public string GetKey()
        {
            return $"instance:{Type.FullName}-{ObjectId:X}";
        }
    }
}