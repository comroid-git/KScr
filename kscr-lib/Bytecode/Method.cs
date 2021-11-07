﻿using System.Collections.Generic;
using KScr.Lib.Model;
using KScr.Lib.Store;

namespace KScr.Lib.Bytecode
{
    public sealed class Method : AbstractClassMember
    {
        public ExecutableCode Body = null!;

        public Method(Class parent, string name, MemberModifier modifier) : base(parent, name, modifier)
        {
        }

        public override IRuntimeSite? Evaluate(RuntimeBase vm, ref State state, ref ObjectRef? rev, byte alt = 0)
        {
            state = Body.Evaluate(vm, null, ref rev);
            return null;
        }

        protected override IEnumerable<AbstractBytecode> BytecodeMembers => new []{ Body };
        public override ClassMemberType Type => ClassMemberType.Method;

        public override void Load(RuntimeBase vm, byte[] data, ref int i)
        {
            Body.Load(vm, data, ref i);
        }

        public static Method Read(RuntimeBase vm, Class parent, byte[] data, ref int i) => (AbstractClassMember.Read(vm, parent, data, ref i) as Method)!;
    }
}