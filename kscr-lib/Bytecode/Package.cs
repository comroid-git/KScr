using System.Collections.Concurrent;
using System.Collections.Generic;

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

        internal Package(Package parent, string name) : base(parent, name)
        {
        }
    }
}