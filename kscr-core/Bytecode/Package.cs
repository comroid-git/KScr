using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using comroid.csapi.common;
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
            {
                continue;
            }
            else if (member is Package pkg)
            {
                pkg.Write(vm, dir.CreateSubdirectory(member.Name), names, strings, true);
            }
            else if (member is Class cls)
            {
                var file = new FileInfo(Path.Combine(dir.FullName, (member.Name.Contains("<")
                    ? member.Name.Substring(0, member.Name.IndexOf("<", StringComparison.Ordinal))
                    : member.Name) + ".kbin"));
                var fStream = FStream(vm, file, FileMode.Create);
                vm.Write(fStream, strings, cls);
                fStream.Dispose();
            }
            else
            {
                throw new NotSupportedException("Member is of unsupported type: " + member.GetType());
            }

        if (!rec)
            strings.Write(dir);
    }

    public static void ReadAll(RuntimeBase vm, DirectoryInfo dir)
    {
        StringCache strings;
        if (new FileInfo(Path.Combine(dir.FullName, RuntimeBase.ModuleLibFile)) is { Exists: true } lib)
        {
            using var zip = new ZipArchive(lib.OpenRead());
            if (zip.GetEntry(StringCache.FileName)?.Open() is { } stream)
            {
                strings = StringCache.Read(stream);
                stream.Dispose();
            }
            else throw new System.Exception("Library contains no String cache");
            foreach (var entry in zip.Entries)
            {
                if (entry.Name == StringCache.FileName)
                    continue;
                var names = entry.FullName.StripExtension(RuntimeBase.BinaryFileExt).Replace('\\', '/').Split("/");
                var pkg = RootPackage.GetPackage(vm, names[..^1])!;
                using var fStream = entry.Open();
                var kls = vm.Load<Class>(vm, strings, fStream, pkg, null);
                Log<Package>.At(LogLevel.Trace, $"Loaded class {kls.CanonicalName} from library {lib.FullName}");
            }
        }
        else
        {
            strings = StringCache.Read(dir);
            foreach (var sub in dir.EnumerateDirectories())
                Read(vm, strings, sub);
        }
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
            using var fStream = FStream(vm, cls, FileMode.Open);
            var kls = vm.Load<Class>(vm, strings, fStream, it, null);
            Log<Package>.At(LogLevel.Trace, $"Loaded class {kls.CanonicalName} from file {cls.FullName}");
        }

        return it;
    }

    private static Stream FStream(RuntimeBase vm, FileInfo file, FileMode mode, bool rec = false)
    {
        return !rec
            ? vm.WrapStream(FStream(vm, file, mode, true), mode switch
            {
                FileMode.Create => CompressionMode.Compress,
                FileMode.Open => CompressionMode.Decompress,
                _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "invalid state")
            })
            : new FileStream(file.FullName, mode);
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

    public Package? GetPackage(RuntimeBase vm, params string[] names)
    {
        var pkg = names.Length > 1 ? RootPackage : this;
        for (var i = 0; i < names.Length; i++)
            pkg = pkg.GetOrCreatePackage(names[i]);
        return pkg;
    }

    public Class? GetClass(RuntimeBase vm, params string[] names)
    {
        return GetPackage(vm, names[..^1])?.GetOrCreateClass(vm, names[^1]);
    }
}