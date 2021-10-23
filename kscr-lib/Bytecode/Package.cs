using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace KScr.Lib.Bytecode
{
    public sealed class Package : AbstractPackageMember
    {
        public static readonly string RootPackageName = "<root>";
        public static readonly Package RootPackage = new Package();
        public bool IsRoot => Name == RootPackageName;

        private Package()
        {
        }

        internal Package(Package parent, string name) : base(parent, name, MemberModifier.Public | MemberModifier.Static)
        {
        }

        public IRuntimeSite FindEntrypoint() => All().Where(it => it is Class).Cast<Class>()
            .Where(it => it.Members.ContainsKey("main"))
            .Select(it => (it.DeclaredMembers["main"] as Method)!)
            .First(it => it.IsStatic());
    }
}