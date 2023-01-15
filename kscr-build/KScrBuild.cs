using System.Text;
using System.Text.Json;
using CommandLine;
using comroid.csapi.common;
using KScr.Core;
using KScr.Core.Bytecode;
using KScr.Core.System;
using KScr.Runtime;

namespace KScr.Build;

public sealed class KScrBuild
{
    static KScrBuild()
    {
#if RELEASE
        ILog.BaseLogger.Level = LogLevel.Config;
#endif
        ILog.BaseLogger.FullNames = false;
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public static void Main(string[] args)
    {
        Parser.Default.ParseArguments<CmdInfo, CmdDependencies, RebuildCmdBuild, CmdPublish, RebuildCmdRun>(args)
            .WithParsed<CmdInfo>(cmd => Log<KScrBuild>.Get()
                .WrapWithExceptionLogger<CmdInfo>(PrintInfo, "Info failed with unhandled Exception")(cmd))
            .WithParsed<CmdDependencies>(cmd => Log<KScrBuild>.Get()
                .WrapWithExceptionLogger<CmdDependencies>(PrintDependencies, "Dependencies failed with unhandled Exception")(cmd))
            .WithParsed<RebuildCmdBuild>(cmd => Log<KScrBuild>.Get()
                .WrapWithExceptionLogger<RebuildCmdBuild>(RunBuild, "Build failed with unhandled Exception")(cmd))
            .WithParsed<CmdPublish>(cmd => Log<KScrBuild>.Get()
                .WrapWithExceptionLogger<CmdPublish>(RunPublish, "Publish failed with unhandled Exception")(cmd))
            .WithParsed<RebuildCmdRun>(cmd => Log<KScrBuild>.Get()
                .WrapWithExceptionLogger<RebuildCmdRun>(RunRun, "Run failed with unhandled Exception")(cmd));
    }

    private static void RunRun(RebuildCmdRun rebuildCmd)
    {
        ApplyCmd(rebuildCmd);
        var (baseModule, exported) = ExtractModules(rebuildCmd);

        foreach (var module in exported) 
            module.RunBuild();

        if (new[] { baseModule }.Concat(exported.Select(mod => mod.ModuleInfo))
                .FirstOrDefault(mod => mod?.MainClassName != null) is { } mod)
        {
            Log<Module>.At(LogLevel.Info, $"Executing module {mod.Notation}...");
            KScrStarter.Execute(out _, mod.MainClassName);
        }
        else
        {
            var findEntrypoint = Package.FindEntrypoint(baseModule?.MainClassName);
            Log<Module>.At(LogLevel.Info, $"Executing module {baseModule!.Notation}...");
            findEntrypoint.Invoke(KScrStarter.VM, RuntimeBase.MainStack);
        }
    }

    private static void RunPublish(CmdPublish cmd)
    {
        var (baseModule, exported) = ExtractModules(cmd);
        foreach (var module in exported)
        {
            var desc = baseModule?.Publishing ?? module.ModuleInfo.Publishing;
            foreach (var repo in desc.Repositories ?? ArraySegment<RepositoryInfo>.Empty)
            {
                if (repo.Url is not "local" and not "localhost")
                    throw new NotImplementedException("External Publishing is not supported yet");

                module.RunBuild();
                DependencyManager.PublishToLocalRepository(module);
            }
        }
    }

    private static (ModuleInfo? baseModule, List<Module> exported) ExtractModules(CmdBase cmd)
    {
        var dir = cmd.Dir ?? new DirectoryInfo(Environment.CurrentDirectory);
        var modulesFile = dir.GetFiles(RuntimeBase.ModulesFile).FirstOrDefault();
        var modulesInfo = modulesFile == null
            ? Log<KScrBuild>.At<ModuleInfo>(LogLevel.Config, $"No {RuntimeBase.ModulesFile} was found in {dir.FullName}")
            : JsonSerializer.Deserialize<ModuleInfo>(File.ReadAllText(modulesFile.FullName)) ??
              throw new Exception($"Unable to parse {RuntimeBase.ModulesFile} in {dir.FullName}");
        if (modulesInfo != null)
            Log<KScrBuild>.At(LogLevel.Config, $"Found Module root {dir.FullName} as Project {modulesInfo.Project}");
        List<Module> exported = new();
        foreach (var moduleFile in dir.EnumerateFiles(RuntimeBase.ModuleFile, SearchOption.AllDirectories)
                     .Where(file => !file.FullName.EndsWith(Path.Combine("build", RuntimeBase.ModuleFile)))) 
        {
            var moduleInfo = JsonSerializer.Deserialize<ModuleInfo>(File.ReadAllText(moduleFile.FullName)) ??
                             throw new Exception($"Unable to parse {RuntimeBase.ModuleFile} in module {moduleFile.Directory!.FullName}");
            var module = new Module(modulesInfo, moduleInfo, moduleFile.Directory!);
            exported.Add(module);
            Log<KScrBuild>.At(LogLevel.Config, $"Found {module} at {moduleFile.FullName}");
        }
        return (modulesInfo, exported);
    }

    private static void RunBuild(RebuildCmdBuild rebuildCmd)
    {
        ApplyCmd(rebuildCmd);
        var (baseModule, exported) = ExtractModules(rebuildCmd);

        SortModules(ref exported);
        
        foreach (var module in exported) 
            module.RunBuild();
    }

    private static void ApplyCmd(IRebuildCmd cmd) => Rebuild = cmd.Rebuild;

    public static bool Rebuild { get; private set; }

    private static void PrintDependencies(CmdDependencies cmd)
    {
        var exported = ExtractModules(cmd).exported;
        foreach (var module in exported)
        {
            Console.WriteLine();
            Console.WriteLine($"Dependencies of Module {module.Notation}:");
            foreach (var dependency in module.Dependencies) 
                Console.WriteLine($" - {dependency} ({dependency.Scope})");
            Console.WriteLine("Repositories:");
            foreach (var repository in module.Repositories)
                Console.WriteLine($" - {repository}");
        }
    }

    private static void PrintInfo(CmdInfo cmd)
    {
        var (baseModule, exported) = ExtractModules(cmd);

        SortModules(ref exported);
        PrintModuleInfo(baseModule, true);
        foreach (var module in exported)
        {
            Console.WriteLine();
            PrintModuleInfo(module.ModuleInfo);
        }
    }

    private static void SortModules(ref List<Module> exported)
    {
        exported.Sort((left, right) => left.DependsOn(right) ? 1 : -1);
    }

    private static void PrintModuleInfo(ModuleInfo module, bool isBaseModule = false)
    {
        Console.WriteLine();
        Console.WriteLine($"Information about {(isBaseModule ? "Project" : "Module")} {module.Notation}");
        
        Console.WriteLine();
        Console.WriteLine("Project information");
        if (module.Project.Domain != null)
            Console.WriteLine($" - Domain: {module.Project.Domain}");
        if (module.Project.Group != null)
            Console.WriteLine($" - Group: {module.Project.Group}");
        if (module.Project.Id != null)
            Console.WriteLine($" - Id: {module.Project.Id}");
        if (module.Project.Version != null)
            Console.WriteLine($" - Version: {module.Project.Version}");
        
        Console.WriteLine();
        Console.WriteLine("Build information");
        if (module.Build.BasePackage != null)
            Console.WriteLine($" - Base Package: {module.Build.BasePackage}");
        if (module.Build.Sources != null)
            Console.WriteLine($" - Source Directory: {module.Build.Sources}");
        if (module.Build.Resources != null)
            Console.WriteLine($" - Resources Directory: {module.Build.Resources}");
        if (module.Build.Output != null)
            Console.WriteLine($" - Output Directory: {module.Build.Output}");
        if (module.Build.Pre != null)
            Console.WriteLine($" - PreBuild Command: {module.Build.Pre}");
        if (module.Build.Post != null)
            Console.WriteLine($" - PostBuild Command: {module.Build.Post}");
        
        var dependencies = (module.Dependencies ?? ArraySegment<DependencyInfo>.Empty).ToArray();
        if (dependencies.Length != 0)
        {
            Console.WriteLine();
            Console.WriteLine($"Dependencies ({dependencies.Length})");
            foreach (var dependency in dependencies)
                Console.WriteLine($" - {dependency} ({dependency.Scope})");
        }
        
        var repositories = (module.Repositories ?? ArraySegment<RepositoryInfo>.Empty).ToArray();
        if (repositories.Length != 0)
        {
            Console.WriteLine();
            Console.WriteLine($"Repositories ({repositories.Length})");
            foreach (var repository in repositories)
                Console.WriteLine($" - {repository}");
        }
    }
        
    public static string Md5Path(FileSystemInfo path) => Path.Combine("build", "checksums",
        (path.FullName.StartsWith("build" + Path.DirectorySeparatorChar)
            ? Path.GetRelativePath(Path.Combine(Environment.CurrentDirectory, "build"), path.FullName)
            : path.Name).TrimEnd(Path.DirectorySeparatorChar) + ".md5");
}
