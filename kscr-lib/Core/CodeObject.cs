﻿using System.Linq;
using KScr.Lib.Bytecode;
using KScr.Lib.Exception;
using KScr.Lib.Model;
using KScr.Lib.Store;

namespace KScr.Lib.Core
{
    public sealed class CodeObject : IObject
    {
        public CodeObject(RuntimeBase vm, Class.Instance type)
        {
            Type = type;
            ObjectId = vm.NextObjId("obj:" + type.FullName);
        }

        public long ObjectId { get; }
        public bool Primitive => false;
        public IClassInstance Type { get; }
        public string ToString(short variant)
        {
            throw new System.NotImplementedException();
        }

        public ObjectRef? Invoke(RuntimeBase vm, string member, params IObject?[] args)
        {
            // try use overrides first
            if (Type.DeclaredMembers.TryGetValue(member, out var icm))
            {
                if (icm.IsStatic())
                    throw new InternalException("Static method invoked on object instance");
                IRuntimeSite? site = icm;
                State state = State.Normal;
                ObjectRef? output = new ObjectRef(Class.VoidType.DefaultInstance, args.Length);
                for (var i = 0; i < vm.Stack.MethodParams!.Count; i++)
                    vm.PutObject(VariableContext.Local, vm.Stack.MethodParams![i].Name, output[vm, i]);
                do
                {
                    site = site.Evaluate(vm, ref state, ref output);
                } while (state == State.Normal && site != null);

                return output;
            } else switch (member)
            {
                case "InternalID":
                    return Numeric.Constant(vm, ObjectId);
                case "toString":
                    short variant;
                    if (args.Length > 0 && args[0] is Numeric num)
                        variant = num.ShortValue;
                    else throw new InternalException("Invalid argument: " + args[0]);
                    return String.Instance(vm, ToString(variant));
                case "getType":
                    return Type.SelfRef;
            }
            throw new InternalException("Method not implemented: " + member);
        }
    }
}