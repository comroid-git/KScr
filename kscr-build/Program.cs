using CommandLine;
using Newtonsoft.Json;

namespace KScr.Build;

public sealed class Program
{
    public static void Main(string[] args) => Parser.Default.ParseArguments<CmdBuild>(args).WithParsed(RunBuild);

    private static void RunBuild(CmdBuild cmd)
    {
        ModuleInfo modules = JsonConvert.DeserializeObject<ModuleInfo>(json);
        throw new NotImplementedException();
    }
}
