using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using KScr.Lib.Model;
using KScr.Lib.Store;

namespace KScr.Lib.Bytecode
{
    public interface IClassMember : IRuntimeSite, IModifierContainer
    {
        public IDictionary<string, IClassMember> Members { get; }
        public Class Parent { get; }
        public string Name { get; }
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