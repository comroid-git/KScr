using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace KScr.Lib.Bytecode
{
    public sealed class Package : AbstractPackageMember
    {
        public static readonly string RootPackageName = "<root>";
        public static readonly Package RootPackage = new();

        private Package() : this(null!, RootPackageName)
        {
        }

        public Package(Package parent, string name) : base(parent, name,
            MemberModifier.Public | MemberModifier.Static)
        {
        }

        public Method FindEntrypoint()
        {
            return All().Where(it => it is Class).Cast<Class>()
                .Where(it => it.DeclaredMembers.ContainsKey("main"))
                .Select(it => (it.DeclaredMembers["main"] as Method)!)
                .First(it => it.IsStatic());
        }

        public void Write(DirectoryInfo dir)
        {
            foreach (var member in Members.Values)
                if (member is Package pkg)
                    pkg.Write(dir.CreateSubdirectory(member.Name));
                else if (member is Class cls)
                    cls.Write(new FileInfo(Path.Combine(dir.FullName, member.Name + ".kbin")));
                else throw new NotSupportedException("Member is of unsupported type: " + member.GetType());
        }

        protected override IEnumerable<AbstractBytecode> BytecodeMembers => throw new NotSupportedException();

        public override void Write(Stream stream)
        {
            Write(new FileInfo((stream as FileStream)!.Name).Directory!);
        }

        public static Package Read(RuntimeBase vm, DirectoryInfo dir, Package? parent = null)
        {
            if (!dir.Exists)
                throw new DirectoryNotFoundException(dir.FullName);
            var it = new Package(parent ?? RootPackage, dir.Name);
            foreach (var sub in dir.EnumerateDirectories())
            {
                var pkg = Read(vm, sub, it);
                it.Members[pkg.Name] = pkg;
            }

            foreach (var cls in dir.EnumerateFiles(".kbin"))
            {
                var kls = Class.Read(vm, cls, it);
                it.Members[kls.Name] = kls;
            }

            return it;
        }

        public override string ToString()
        {
            return $"Package<{FullName}>";
        }

        public Package GetOrCreatePackage(string name)
        {
            if (Members.TryGetValue(name, out var pm) && pm is Package pkg)
                return pkg;
            Add(pkg = new Package(this, name));
            return pkg;
        }

        public Class? GetOrCreateClass(RuntimeBase vm, string name, MemberModifier mod = MemberModifier.None)
        {
            if (Members.TryGetValue(name, out var pm) && pm is Class cls)
                return cls;
            if (mod == MemberModifier.None)
                return null;
            Add(cls = new Class(this, name, false, mod));
            cls.Initialize(vm);
            return cls;
        }

        public Package GetPackage(string[] names)
        {
            var pkg = RootPackage;
            foreach (string name in names)
                pkg = GetOrCreatePackage(name);
            return pkg;
        }

        public Class? GetClass(RuntimeBase vm, string[] names)
        {
            var pkg = RootPackage;
            for (var i = 0; i < names.Length - 1; i++)
                pkg = pkg.GetOrCreatePackage(names[i]);
            return pkg.GetOrCreateClass(vm, names[^1]);
        }
    }
}