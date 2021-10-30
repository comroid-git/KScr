using System.Collections.Concurrent;
using System.Collections.Generic;
using KScr.Lib.Model;
using KScr.Lib.Store;

namespace KScr.Lib.Bytecode
{
    public sealed class Class : AbstractPackageMember, IClassRef
    {
        public Class(Package parent, string name, MemberModifier modifier) : base(parent, name, modifier)
        {
        }

        public IDictionary<string, IClassMember> DeclaredMembers { get; } =
            new ConcurrentDictionary<string, IClassMember>();

        public TokenType Modifier { get; }
        public long TypeId { get; }
        public object? Default { get; }
    }
}