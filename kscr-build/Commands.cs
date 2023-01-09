using CommandLine;

namespace KScr.Build;

public interface CmdBase
{
    [Value(0, Required = false)]
    public DirectoryInfo? Dir { get; set; }
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
}

[Verb("dependencies")]
public class CmdDependencies : CmdBase
{
    public DirectoryInfo? Dir { get; set; }
}

[Verb("build")]
public class RebuildCmdBuild : CmdBase, IRebuildCmd
{
    public DirectoryInfo? Dir { get; set; }
    public bool Rebuild { get; set; }
}

[Verb("publish")]
public class CmdPublish : CmdBase
{
    public DirectoryInfo? Dir { get; set; }
}

[Verb("run")]
public class RebuildCmdRun : CmdBase, IRebuildCmd
{
    public DirectoryInfo? Dir { get; set; }
    public bool Rebuild { get; set; }
}