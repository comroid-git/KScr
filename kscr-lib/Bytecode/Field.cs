using KScr.Lib.Model;
using KScr.Lib.Store;

namespace KScr.Lib.Bytecode
{
    public sealed class Field : AbstractClassMember
    {
        public IEvaluable Getter = null!;
        public IEvaluable Setter = null!;
        
        public Field(Class parent, string name, MemberModifier modifier) : base(parent, name, modifier)
        {
        }

        public override IRuntimeSite? Evaluate(RuntimeBase vm, ref State state, ref ObjectRef? rev, byte alt = 0)
        {
            state = (alt == 1 ? Setter : Getter).Evaluate(vm, null, ref rev);
            return null;
        }
    }
}