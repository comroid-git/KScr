using CommandLine;

namespace KScr.Build;

public sealed class Program
{
    public static void Main(string[] args) => Parser.Default.ParseArguments<CmdBuild>(args).WithParsed(RunBuild);

    private static void RunBuild(CmdBuild cmd)
    {
        throw new NotImplementedException();
    }
}
