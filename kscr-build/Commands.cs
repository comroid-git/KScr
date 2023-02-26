using CommandLine;
using KScr.Core;
using KScr.Runtime;

namespace KScr.Build;

public interface CmdBase
{
    [Value(0, Required = false)]
    public DirectoryInfo? Dir { get; set; }
    [Option('q', "quiet", Default = false)]
    public bool Quiet { get; set; }
}

public interface IRebuildCmd
{
    [Option('r', "rebuild", Default = false)]
    public bool Rebuild { get; set; }
}

[Verb("info")]
public class CmdInfo : CmdBase
{
    public DirectoryInfo? Dir { get; set; }
    public bool Quiet { get; set; }
}

[Verb("dependencies")]
public class CmdDependencies : CmdBase
{
    public DirectoryInfo? Dir { get; set; }
    public bool Quiet { get; set; }
}

[Verb("build")]
public class RebuildCmdBuild : CmdBase, IRebuildCmd
{
    public DirectoryInfo? Dir { get; set; }
    public bool Quiet { get; set; }
    public bool Rebuild { get; set; }
}

[Verb("publish")]
public class CmdPublish : CmdBase
{
    public DirectoryInfo? Dir { get; set; }
    public bool Quiet { get; set; }
}

[Verb("dist")]
public class CmdDist : CmdBase, IRebuildCmd
{
    public DirectoryInfo? Dir { get; set; }
    public bool Quiet { get; set; }
    public bool Rebuild { get; set; }
    [Option('t', "type")]
    public DistType Type { get; set; }
    [Option('o', "output")]
    public FileInfo Output { get; set; }

    public enum DistType
    {
        dir,
        zip,
        gz
    }
}

[Verb("run")]
public class RebuildCmdRun : CmdBase, IRebuildCmd
{
    public DirectoryInfo? Dir { get; set; }
    public bool Quiet { get; set; }
    public bool Rebuild { get; set; }
}