using CommandLine;
using Newtonsoft.Json;

namespace KScr.Build;

public sealed class Program
{
    public const string ModulesFile = "modules.kmod.json";
    public const string ModuleFile = "module.kmod.json";

    public static void Main(string[] args) => Parser.Default.ParseArguments<CmdBuild, CmdDependencies>(args)
        .WithParsed<CmdBuild>(RunBuild)
        .WithParsed<CmdDependencies>(PrintDependencies);

    private static (ModuleInfo baseModule, List<Module> exported) ExtractModules(CmdBase cmd)
    {
        var dir = cmd.Dir ?? new DirectoryInfo(Directory.GetCurrentDirectory());
        var modulesFile = dir.GetFiles(ModulesFile)[0];
        var modules = JsonConvert.DeserializeObject<ModuleInfo>(File.ReadAllText(modulesFile.FullName)) ??
                      throw new Exception("Unable to parse " + ModulesFile);
        Console.WriteLine($"Exporting module root {dir.FullName} as Project {modules.Project}");
        List<Module> exported = new();
        foreach (var moduleFile in dir.EnumerateFiles(ModuleFile, SearchOption.AllDirectories))
        {
            var module = JsonConvert.DeserializeObject<ModuleInfo>(File.ReadAllText(moduleFile.FullName)) ??
                         throw new Exception("Unable to parse " + ModuleFile + " for module " + moduleFile.Directory!.Name);
            exported.Add(new Module(modules, module, dir));
            Console.WriteLine($"Exporting {module}");
        }
        return (modules, exported);
    }

    private static void RunBuild(CmdBuild cmd)
    {
        var (baseModule, exported) = ExtractModules(cmd);
    }

    private static void PrintDependencies(CmdDependencies cmd)
    {
        var (baseModule, exported) = ExtractModules(cmd);
    }
}
