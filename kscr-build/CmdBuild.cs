using CommandLine;

namespace KScr.Build;

public class CmdBuild
{
    [Option]
    public string? Dir { get; set; }
}