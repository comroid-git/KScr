using System;
using System.Collections.Generic;
using System.Linq;
using KScr.Lib.Model;
using KScr.Lib.Store;

namespace KScr.Lib.Bytecode
{
    public class MethodParameter
    {
        public IClass Type { get; set; }
        public string Name { get; set; }
    }
    
    public sealed class Method : AbstractClassMember
    {
        public ExecutableCode Body = null!;

        public List<MethodParameter> Parameters { get; } = new List<MethodParameter>();

        public Method(Class parent, string name, MemberModifier modifier) : base(parent, name, modifier)
        {
        }

        public override IRuntimeSite? Evaluate(RuntimeBase vm, ref State state, ref ObjectRef? rev, byte alt = 0)
        {
            if (Modifier.IsStatic())
                vm.Stack.StepDown(Parent, Name);
            else vm.Stack.StepDown(rev!, Name);
            state = Body.Evaluate(vm, null, ref rev);
            return null;
        }

        public override string Name => base.Name + '(' + string.Join(", ", Parameters.Select(it => it.Type)) + ')';

        protected override IEnumerable<AbstractBytecode> BytecodeMembers => new []{ Body };
        public override ClassMemberType Type => ClassMemberType.Method;

        public override void Load(RuntimeBase vm, byte[] data, ref int i)
        {
            Body.Load(vm, data, ref i);
        }

        public new static Method Read(RuntimeBase vm, Class parent, byte[] data, ref int i) => (AbstractClassMember.Read(vm, parent, data, ref i) as Method)!;
    }
}