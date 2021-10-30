using System.Collections.Concurrent;
using System.Collections.Generic;
using KScr.Lib.Model;
using KScr.Lib.Store;

namespace KScr.Lib.Bytecode
{
    public sealed class Class : AbstractPackageMember, IClassRef, IRuntimeSite
    {
        public const string StaticInitializer = "initializer_static";
        
        public Class(Package parent, string name, MemberModifier modifier) : base(parent, name, modifier)
        {
        }

        public IDictionary<string, IClassMember> DeclaredMembers { get; } =
            new ConcurrentDictionary<string, IClassMember>();

        public MemberModifier Modifier { get; }
        public long TypeId { get; }
        public object? Default { get; }

        public IRuntimeSite? Evaluate(RuntimeBase vm, ref State state, ref ObjectRef? rev, byte alt = 0) =>
            DeclaredMembers[StaticInitializer].Evaluate(vm, ref state, ref rev, alt);
    }
}