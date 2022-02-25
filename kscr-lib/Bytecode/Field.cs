using System.Collections.Generic;
using KScr.Lib.Store;

namespace KScr.Lib.Bytecode
{
    public sealed class Field : AbstractClassMember
    {
        public Method Getter = null!;
        public Method Setter = null!;

        public Field(Class parent, string name, MemberModifier modifier) : base(parent, name, modifier)
        {
        }

        public override ClassMemberType Type => ClassMemberType.Field;

        protected override IEnumerable<AbstractBytecode> BytecodeMembers => new[] { Getter, Setter };

        public override IRuntimeSite? Evaluate(RuntimeBase vm, ref State state, ref ObjectRef? rev, byte alt = 0)
        {
            return (alt == 1 ? Setter : Getter).Evaluate(vm, ref state, ref rev);
        }

        public override void Load(RuntimeBase vm, byte[] data, ref int i)
        {
            Getter = Method.Read(vm, Parent, data, ref i);
            Setter = Method.Read(vm, Parent, data, ref i);
        }

        public new static Field Read(RuntimeBase vm, Class parent, byte[] data, ref int i)
        {
            return (AbstractClassMember.Read(vm, parent, data, ref i) as Field)!;
        }
    }
}