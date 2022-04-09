using System;
using System.Collections.Generic;
using System.Linq;

namespace KScr.Core;

public abstract class AbstractVersion
{
    public readonly Version Version;

    protected AbstractVersion(string vStr)
    {
        Version = new Version(vStr);
    }
}

public sealed class RuntimeVersion : AbstractVersion
{
    private static readonly ICollection<RuntimeVersion> _cache = new HashSet<RuntimeVersion>();
    public static readonly RuntimeVersion V_0_1_0 = new("0.1.0");
    public static readonly RuntimeVersion V_0_1_1 = new("0.1.1");
    public static readonly RuntimeVersion V_0_2_0 = new("0.2.0");
    public static readonly RuntimeVersion V_0_2_1 = new("0.2.1");
    public static readonly RuntimeVersion V_0_2_2 = new("0.2.2");
    public static readonly RuntimeVersion V_0_3_0 = new("0.3.0");

    private RuntimeVersion(string vStr) : base(vStr)
    {
        _cache.Add(this);
    }

    public static RuntimeVersion Current => V_0_3_0;

    public static RuntimeVersion Find(int major, int minor, int build = int.MinValue, string? msg = null)
    {
        var ver = new Version(major, minor, build);
        return _cache.FirstOrDefault(x => x.Version >= ver)
               ?? throw new NullReferenceException(msg ?? "Unable to find RuntimeVersion " + ver);
    }
}

public sealed class BytecodeVersion : AbstractVersion
{
    private static readonly ICollection<BytecodeVersion> _cache = new HashSet<BytecodeVersion>();
    public static readonly BytecodeVersion V_0_10 = new("0.10");

    private BytecodeVersion(string vStr) : base(vStr)
    {
        _cache.Add(this);
    }

    public static BytecodeVersion Current => V_0_10;

    public static BytecodeVersion Find(int major, int minor = int.MinValue, string? msg = null)
    {
        var ver = new Version(major, minor);
        return _cache.FirstOrDefault(x => x.Version >= ver)
               ?? throw new NullReferenceException(msg ?? "Unable to find BytecodeVersion " + ver);
    }
}