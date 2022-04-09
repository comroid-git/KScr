using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KScr.Core.Model;
using KScr.Core.Util;

namespace KScr.Core.Bytecode
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

        public void Write(RuntimeBase vm, DirectoryInfo dir, ClassInfo[]? names = null, StringCache? strings = null,
            bool rec = false)
        {
            names ??= Array.Empty<ClassInfo>();
            strings ??= new StringCache();
            foreach (var member in PackageMembers.Values)
                if (!names.Any(name => name.FullName.StartsWith(member.FullName.Contains("<")
                        ? member.FullName.Substring(0, member.FullName.IndexOf('<'))
                        : member.FullName)))
                    // ReSharper disable once RedundantJumpStatement
                    continue;
                else if (member is Package pkg)
                    pkg.Write(vm, dir.CreateSubdirectory(member.Name), names, strings, true);
                else if (member is Class cls)
                {
                    var file = new FileInfo(Path.Combine(dir.FullName, (member.Name.Contains("<")
                        ? member.Name.Substring(0, member.Name.IndexOf("<", StringComparison.Ordinal))
                        : member.Name) + ".kbin"));
                    vm.Write(strings, new FileStream(file.FullName, FileMode.Create), cls);
                }
                else throw new NotSupportedException("Member is of unsupported type: " + member.GetType());
            if (!rec)
                strings.Write(dir);
        }

        public static void ReadAll(RuntimeBase vm, DirectoryInfo dir, StringCache? strings = null)
        {
            strings ??= StringCache.Read(dir);
            foreach (var sub in dir.EnumerateDirectories())
                Read(vm, strings, sub);
        }

        public static Package Read(RuntimeBase vm, StringCache strings, DirectoryInfo dir, Package? parent = null)
        {
            if (!dir.Exists)
                throw new DirectoryNotFoundException(dir.FullName);
            var it = new Package(parent ?? RootPackage, dir.Name);
            foreach (var sub in dir.EnumerateDirectories())
            {
                var pkg = Read(vm, strings, sub, it);
                //it.Members[pkg.Name] = pkg;
            }

            foreach (var cls in dir.EnumerateFiles("*.kbin"))
            {
                var kls = vm.Load<Class>(vm, strings, new FileStream(cls.FullName, FileMode.Open), it, null); //Class.Read(vm, strings, cls, it);
                //it.Members[kls.Name] = kls;
            }

            return it;
        }

        public override string ToString()
        {
            return $"Package<{FullName}>";
        }

        public Package GetOrCreatePackage(string name)
        {
            if (name.Contains('.'))
            {
                var rec = this;
                foreach (var sub in name.Split('.'))
                    rec = rec.GetOrCreatePackage(sub);
                return rec;
            }
            if (PackageMembers.TryGetValue(name, out var pm) && pm is Package pkg)
                return pkg;
            Add(pkg = new Package(this, name));
            return pkg;
        }

        public Class? GetOrCreateClass(RuntimeBase vm, string name, MemberModifier mod = MemberModifier.None,
            ClassType type = ClassType.Class)
        {
            if (PackageMembers.TryGetValue(name, out var pm) && pm is Class cls)
                return cls;
            if (mod == MemberModifier.None)
                return null;
            Add(cls = new Class(this, name, false, mod, type));
            cls.Initialize(vm);
            return cls;
        }

        public Class? GetClass(RuntimeBase vm, string[] names)
        {
            var pkg = names.Length > 1 ? RootPackage : this;
            for (var i = 0; i < names.Length - 1; i++)
                pkg = pkg.GetOrCreatePackage(names[i]);
            return pkg.GetOrCreateClass(vm, names[^1]);
        }
    }
}