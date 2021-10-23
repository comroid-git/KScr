using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using KScr.Lib.Model;
using KScr.Lib.Store;

namespace KScr.Lib.Bytecode
{
    public interface IClassMember : IRuntimeSite, IModifierContainer
    {
        public Class Parent { get; }
        public string Name { get; }
        public string FullName { get; }
    }
    
    public abstract class AbstractClassMember : IClassMember
    {
        protected AbstractClassMember(Class parent, string name, MemberModifier modifier)
        {
            Parent = parent;
            Name = name;
            Modifier = modifier;
        }
        
        public Class Parent { get; }
        public string Name { get; }
        public MemberModifier Modifier { get; }
        public string FullName => Parent.FullName + '.' + Name;

        public abstract IRuntimeSite? Evaluate(RuntimeBase vm, ref State state, ref ObjectRef? rev, byte alt = 0);
    }
}