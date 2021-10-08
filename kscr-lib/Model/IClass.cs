using KScr.Lib.Model;

namespace KScr.Lib.Store
{
    public interface IClassRef
    {
        ClassTokenType Modifier { get; }
        string FullName { get; }
        long TypeId { get; }
        object? Default { get; }

        bool CanHold(IClassRef? type)
        {
            return FullName == "void" || Equals(type);
        }
    }
}