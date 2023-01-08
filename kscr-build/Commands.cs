using CommandLine;

namespace KScr.Build;

public interface CmdBase
{
    [Option]
    public DirectoryInfo? Dir { get; set; }
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
public class CmdBuild : CmdBase
{
    public DirectoryInfo? Dir { get; set; }
}

[Verb("publish")]
public class CmdPublish : CmdBase
{
    public DirectoryInfo? Dir { get; set; }
}

[Verb("run")]
public class CmdRun : CmdBase
{
    public DirectoryInfo? Dir { get; set; }
}