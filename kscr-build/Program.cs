using CommandLine;
using Newtonsoft.Json;

namespace KScr.Build;

public sealed class Program
{
    public const string ModulesFile = "modules.kmod.json";
    public const string ModuleFile = "module.kmod.json";

    public static void Main(string[] args) => Parser.Default.ParseArguments<CmdBuild, CmdDependencies, CmdInfo>(args)
        .WithParsed<CmdBuild>(RunBuild)
        .WithParsed<CmdDependencies>(PrintDependencies)
        .WithParsed<CmdInfo>(PrintInfo);

    private static (ModuleInfo baseModule, List<Module> exported) ExtractModules(CmdBase cmd)
    {
        var dir = cmd.Dir ?? new DirectoryInfo(Directory.GetCurrentDirectory());
        var modulesFile = dir.GetFiles(ModulesFile)[0];
        var modulesInfo = JsonConvert.DeserializeObject<ModuleInfo>(File.ReadAllText(modulesFile.FullName)) ??
                      throw new Exception("Unable to parse " + ModulesFile);
        Console.WriteLine($"Found Module root {dir.FullName} as Project {modulesInfo.Project}");
        List<Module> exported = new();
        foreach (var moduleFile in dir.EnumerateFiles(ModuleFile, SearchOption.AllDirectories))
        {
            var moduleInfo = JsonConvert.DeserializeObject<ModuleInfo>(File.ReadAllText(moduleFile.FullName)) ??
                         throw new Exception("Unable to parse " + ModuleFile + " for module " + moduleFile.Directory!.Name);
            var module = new Module(modulesInfo, moduleInfo, dir);
            exported.Add(module);
            Console.WriteLine($"Found {module}");
        }
        return (modulesInfo, exported);
    }

    private static void RunBuild(CmdBuild cmd)
    {
        var (baseModule, exported) = ExtractModules(cmd);

        SortModulesByCoDependencies(exported);
    }
    
    private class CoDepNode
    { 
        internal readonly Module Module;
        internal readonly List<CoDepNode> Children = new();
        internal CoDepNode? Parent { get; set; }

        public CoDepNode(Module module)
        {
            Module = module;
        }

        internal CoDepNode FindAnywhere(Module dep)
        {
            if (Parent?.Module.Notation == dep.Notation)
                return Parent;
            if (Children.FirstOrDefault(x => x.Module.Notation == dep.Notation) is { } child)
                return child;
            return new CoDepNode(dep);
        }
    }

    private static void SortModulesByCoDependencies(List<Module> exported)
    {
        CoDepNode? parent = null;
        foreach (var module in exported)
        {
            var buf = new CoDepNode(module);
            if (parent == null)
                parent = buf;
        }

        // todo fixme
        CheckCircular_Rec(ArraySegment<string>.Empty, parent);
        
        while (parent.Parent != null)
            parent = parent.Parent;
        exported.Clear();
        exported.Add(parent.Module);
        AddCoDeps_Rec(exported, parent);
    }

    private static void CheckCircular_Rec(IEnumerable<string> above, CoDepNode node)
    {
        if (node.Children.Any(x => above.Contains(x.Module.Notation)))
            throw new Exception("Circular dependency detected");
        var above_ = above.Append(node.Module.Notation).ToArray();
        foreach (var child in node.Children) 
            CheckCircular_Rec(above_, child);
    }

    private static void AddCoDeps_Rec(List<Module> exported, CoDepNode node)
    {
        foreach (var coDepNode in node.Children)
        {
            exported.Add(coDepNode.Module);
            AddCoDeps_Rec(exported, coDepNode);
        }
    }

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

        SortModulesByCoDependencies(exported);
        PrintModuleInfo(baseModule, true);
        foreach (var module in exported)
        {
            Console.WriteLine();
            PrintModuleInfo(module.ModuleInfo);
        }
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
}
