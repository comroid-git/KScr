using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using KScr.Lib.Exception;

namespace KScr.Lib.Bytecode
{
    public interface IPackageMember : IModifierContainer
    {
        public IDictionary<string, IPackageMember> PackageMembers { get; }
        public bool IsRoot { get; }
        public Package? Package { get; }
        public string Name { get; }
        public string FullName { get; }
        public IPackageMember GetMember(string name);
        public IPackageMember Add(IPackageMember member);
    }

    public abstract class AbstractPackageMember : AbstractBytecode, IPackageMember
    {
        private protected string _name;

        protected AbstractPackageMember() : this(null!, Package.RootPackageName,
            MemberModifier.Public | MemberModifier.Static)
        {
        }

        public AbstractPackageMember(Package parent, string name, MemberModifier modifier)
        {
            Package = parent;
            _name = name;
            Modifier = modifier;

            if (Package != null)
                Package.PackageMembers[name] = this;
        }

        public IDictionary<string, IPackageMember> PackageMembers { get; } =
            new ConcurrentDictionary<string, IPackageMember>();

        public bool IsRoot => Name == Package.RootPackageName;

        public Package? Package { get; }

        public virtual string Name => _name;

        public MemberModifier Modifier { get; protected set; }

        public string FullName => Package?.FullName +
                                  (IsRoot ? string.Empty : (Package?.IsRoot ?? true ? string.Empty : '.') + Name);

        public IPackageMember GetMember(string name)
        {
            return name.Contains('.') ? GetAbsoluteMember(name) : PackageMembers[name];
        }

        public IPackageMember Add(IPackageMember member)
        {
            return PackageMembers[member.Name] = member;
        }

        public IPackageMember GetAbsoluteMember(string name)
        {
            return Package.RootPackage.GetAbsoluteMember(name.Split('.'), 0);
        }

        private IPackageMember GetAbsoluteMember(string[] names, int i)
        {
            if (i == names.Length && names[i] == Name)
                return this;
            if (i < names.Length)
                return (GetMember(names[i]) as AbstractPackageMember)!.GetAbsoluteMember(names, i + 1);
            throw new FatalException("Member not found: " + string.Join('.', names));
        }

        protected IEnumerable<IPackageMember> All()
        {
            return new IPackageMember[] { this }.Concat(PackageMembers.Values
                .SelectMany(it => (it as AbstractPackageMember)!.All())).Distinct();
        }
    }
}