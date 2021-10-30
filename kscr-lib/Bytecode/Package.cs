using System.Linq;

namespace KScr.Lib.Bytecode
{
    public sealed class Package : AbstractPackageMember
    {
        public static readonly string RootPackageName = "<root>";
        public static readonly Package RootPackage = new Package();

        private Package()
        {
        }

        internal Package(Package parent, string name) : base(parent, name,
            MemberModifier.Public | MemberModifier.Static)
        {
        }

        public bool IsRoot => Name == RootPackageName;

        public IRuntimeSite FindEntrypoint()
        {
            return All().Where(it => it is Class).Cast<Class>()
                .Where(it => it.Members.ContainsKey("main"))
                .Select(it => (it.DeclaredMembers["main"] as Method)!)
                .First(it => it.IsStatic());
        }
    }
}