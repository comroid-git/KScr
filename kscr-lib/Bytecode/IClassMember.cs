using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using KScr.Lib.Model;
using KScr.Lib.Store;

namespace KScr.Lib.Bytecode
{
    [Flags]
    public enum MemberModifier {
        // accessibility keywords
        Public = 0x0000_1000,
        Internal = 0x0000_2000,
        Protected = 0x0000_4000,
        Private = 0x0000_8000,
        Abstract = 0x0040_0000,
        Final = 0x0080_0000,
        Static = 0x0010_0000
    }
    
    public interface IClassMember : IRuntimeSite
    {
        public IDictionary<string, IClassMember> Members { get; }
        public Class Parent { get; }
        public string Name { get; }
        public MemberModifier Modifier { get; }
        public string FullName { get; }
        public IClassMember GetMember(string name);
        public IClassMember Add(IClassMember member);
    }
    
    public abstract class AbstractClassMember : IClassMember
    {
        protected AbstractClassMember(Class parent, string name, MemberModifier modifier)
        {
            Parent = parent;
            Name = name;
            Modifier = modifier;
        }

        public IDictionary<string, IClassMember> Members { get; } = new ConcurrentDictionary<string, IClassMember>();
        public Class Parent { get; }
        public string Name { get; }
        public MemberModifier Modifier { get; }
        public string FullName => Parent.FullName + '.' + Name;

        public IClassMember GetMember(string name) => Members[name];

        public IClassMember Add(IClassMember member) => Members[member.Name] = member;
        public abstract IRuntimeSite Evaluate(RuntimeBase vm, ref State state, ref ObjectRef rev);
    }
}