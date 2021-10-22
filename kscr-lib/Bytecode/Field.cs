namespace KScr.Lib.Bytecode
{
    public sealed class Field : AbstractClassMember
    {
        public Field(Class parent, string name, MemberModifier modifier) : base(parent, name, modifier)
        {
        }
    }
}