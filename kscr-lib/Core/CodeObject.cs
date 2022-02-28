﻿using System;
using System.Collections.Generic;
using System.Linq;
using KScr.Lib.Bytecode;
using KScr.Lib.Exception;
using KScr.Lib.Model;
using KScr.Lib.Store;

namespace KScr.Lib.Core
{
    public sealed class CodeObject : IObject
    {
        public CodeObject(RuntimeBase vm, IClassInstance type)
        {
            Type = type;
            ObjectId = vm.NextObjId("obj:" + type.FullName);
        }

        public long ObjectId { get; }
        public bool Primitive => false;
        public IClassInstance Type { get; }
        public string ToString(short variant) => Type.Name + "#" + ObjectId.ToString("X");

        public ObjectRef? Invoke(RuntimeBase vm, string member, ref ObjectRef? rev, params IObject?[] args)
        {
            // try use overrides first
            if (Type.DeclaredMembers.TryGetValue(member, out var icm))
            {
                if (icm.IsStatic())
                    throw new InternalException("Static method invoked on object instance");
                IRuntimeSite? site = icm;
                List<MethodParameter>? param = (icm as IMethod)?.Parameters;
                State state = State.Normal;
                vm.Stack.StepDown(vm, rev, "toString", ref rev, _rev =>
                {
                    for (var i = 0; i < args.Length; i++)
                        vm.PutObject(VariableContext.Local, param?[i].Name ?? throw new NullReferenceException(), args[i]);
                    do
                    {
                        // todo: step inside context of REV
                        site = site.Evaluate(vm, ref state, ref _rev!);
                    } while (state == State.Normal && site != null);

                    return _rev;
                });

                return rev;
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