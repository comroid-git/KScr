using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using KScr.Lib.Exception;

namespace KScr.Lib.Bytecode
{
    public interface IPackageMember : IModifierContainer
    {
        public IDictionary<string, IPackageMember> Members { get; }
        public bool IsRoot { get; }
        public Package? Parent { get; }
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
            Parent = parent;
            _name = name;
            Modifier = modifier;

            if (Parent != null)
                Parent.Members[name] = this;
        }

        public IDictionary<string, IPackageMember> Members { get; } =
            new ConcurrentDictionary<string, IPackageMember>();

        public bool IsRoot => Name == Package.RootPackageName;

        public Package? Parent { get; }

        public virtual string Name => _name;

        public MemberModifier Modifier { get; protected set; }

        public string FullName => Parent?.FullName +
                                  (IsRoot ? string.Empty : (Parent?.IsRoot ?? true ? string.Empty : '.') + Name);

        public IPackageMember GetMember(string name)
        {
            return name.Contains('.') ? GetAbsoluteMember(name) : Members[name];
        }

        public IPackageMember Add(IPackageMember member)
        {
            return Members[member.Name] = member;
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
            throw new InternalException("Member not found: " + string.Join('.', names));
        }

        protected IEnumerable<IPackageMember> All()
        {
            return new IPackageMember[] { this }.Concat(Members.Values
                .SelectMany(it => (it as AbstractPackageMember)!.All())).Distinct();
        }
    }
}