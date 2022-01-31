using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace KScr.Lib.Bytecode
{
    public sealed class Package : AbstractPackageMember
    {
        public static readonly string RootPackageName = "<root>";
        public static readonly Package RootPackage = new Package();

        private Package()
        {
        }

        internal Package(Package parent, string name) : base(parent, name,
            MemberModifier.Public | MemberModifier.Static)
        {
        }

        public bool IsRoot => Name == RootPackageName;

        public IRuntimeSite FindEntrypoint()
        {
            return All().Where(it => it is Class).Cast<Class>()
                .Where(it => it.Members.ContainsKey("main"))
                .Select(it => (it.DeclaredMembers["main"] as Method)!)
                .First(it => it.IsStatic());
        }

        public void Write(DirectoryInfo dir)
        {
            foreach (var member in Members.Values)
            {
                if (member is Package pkg)
                    pkg.Write(dir.CreateSubdirectory(member.Name));
                else if (member is Class cls)
                    cls.Write(new FileInfo(Path.Combine(dir.FullName, member.Name, ".kbin")));
                else throw new NotSupportedException("Member is of unsupported type: " + member.GetType());
            }
        }

        public override void Write(Stream stream) => Write(new FileInfo((stream as FileStream)!.Name).Directory!);

        public static Package Read(RuntimeBase vm, DirectoryInfo dir, Package? parent = null)
        {
            if (!dir.Exists)
                throw new DirectoryNotFoundException(dir.FullName);
            Package it = new Package(parent ?? RootPackage, dir.Name);
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
    }
}