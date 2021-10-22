using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using KScr.Lib.Exception;

namespace KScr.Lib.Bytecode
{
    public interface IPackageMember
    {
        public IDictionary<string, IPackageMember> Members { get; }
        public Package? Parent { get; }
        public string Name { get; }
        public string FullName { get; }
        public IPackageMember GetMember(string name);
        public IPackageMember Add(IPackageMember member);
    }
    
    public abstract class AbstractPackageMember : IPackageMember
    {
        public IDictionary<string, IPackageMember> Members { get; } = new ConcurrentDictionary<string, IPackageMember>();
        public Package? Parent { get; }
        public string Name { get; }
        public string FullName => (Parent != null ? Parent?.FullName + '.' : string.Empty) + Name;

        protected AbstractPackageMember() : this(null!, Package.RootPackageName)
        {
        }

        public AbstractPackageMember(Package parent, string name)
        {
            Parent = parent;
            Name = name;
        }

        public IPackageMember GetMember(string name) => name.Contains('.') ? GetAbsoluteMember(name) : Members[name];
        
        public IPackageMember Add(IPackageMember member) => Members[member.Name] = member;

        public Package GetOrCreatePackage(string name, Package? parent = null)
        {
            if (Members.TryGetValue(name, out var pm) && pm is Package pkg)
                return pkg;
            Add(pkg = new Package(parent ?? Package.RootPackage, name));
            return pkg;
        }
        
        public Class GetOrCreateClass(string name, Package? parent = null)
        {
            if (Members.TryGetValue(name, out var pm) && pm is Class cls)
                return cls;
            Add(cls = new Class(parent ?? Package.RootPackage, name));
            return cls;
        }

        public IPackageMember GetAbsoluteMember(string name) => Package.RootPackage.GetAbsoluteMember(name.Split('.'), 0);

        private IPackageMember GetAbsoluteMember(string[] names, int i)
        {
            if (i == names.Length && names[i] == Name)
                return this;
            if (i < names.Length)
                return (GetMember(names[i]) as AbstractPackageMember)!.GetAbsoluteMember(names, i + 1);
            throw new InternalException("Member not found: " + string.Join('.', names));
        }
    }
}