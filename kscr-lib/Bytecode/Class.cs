using System.Collections.Concurrent;
using System.Collections.Generic;
using KScr.Lib.Store;

namespace KScr.Lib.Bytecode
{
    public sealed class Class : AbstractPackageMember
    {
        public IDictionary<string, IClassMember> DeclaredMembers { get; } = new ConcurrentDictionary<string, IClassMember>();

        public Class(Package parent, string name, MemberModifier modifier) : base(parent, name, modifier)
        {
        }
    }
}