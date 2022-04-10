using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using KScr.Core.Model;
using KScr.Core.Std;
using KScr.Core.Util;

namespace KScr.Core.Bytecode;

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
            var kls = vm.Load<Class>(vm, strings, vm.WrappedFileStream(cls, FileMode.Open), it,
                null); //Class.Read(vm, strings, cls, it);
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