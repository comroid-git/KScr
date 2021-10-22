using KScr.Lib.Model;
using KScr.Lib.Store;

namespace KScr.Lib.Bytecode
{
    public sealed class Method : AbstractClassMember
    {
        public Method(Class parent, string name, MemberModifier modifier) : base(parent, name, modifier)
        {
        }

        public override IRuntimeSite Evaluate(RuntimeBase vm, ref State state, ref ObjectRef rev)
        {
            throw new System.NotImplementedException();
        }
    }
}