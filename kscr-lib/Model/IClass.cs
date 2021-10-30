using KScr.Lib.Bytecode;

namespace KScr.Lib.Model
{
    public interface IClassRef
    {
        MemberModifier Modifier { get; }
        string FullName { get; }
        long TypeId { get; }

        bool CanHold(IClassRef? type)
        {
            return FullName == "void" || Equals(type);
        }
    }
}