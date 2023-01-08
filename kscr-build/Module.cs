using comroid.csapi.common;
using KScr.Runtime;

namespace KScr.Build;

public class Module
{
    public readonly ModuleInfo? ModulesInfo;
    public readonly ModuleInfo ModuleInfo;
    public readonly DirectoryInfo RootDir;

    public Module(ModuleInfo? modulesInfo, ModuleInfo moduleInfo, DirectoryInfo rootDir)
    {
        ModulesInfo = modulesInfo;
        ModuleInfo = moduleInfo;
        RootDir = rootDir;
    }

    public ProjectInfo Project => ModulesInfo != null ? ModulesInfo.Project + ModuleInfo.Project : ModuleInfo.Project;
    public BuildInfo Build => ModulesInfo != null ? ModulesInfo.Build + ModuleInfo.Build : ModuleInfo.Build;
    public IEnumerable<RepositoryInfo> Repositories => Concat(ModulesInfo?.Repositories ?? ArraySegment<RepositoryInfo>.Empty, ModuleInfo.Repositories);
    public IEnumerable<DependencyInfo> Dependencies => Concat(ModulesInfo?.Dependencies ?? ArraySegment<DependencyInfo>.Empty, ModuleInfo.Dependencies);

    private IEnumerable<T> Concat<T>(IEnumerable<T>? first, IEnumerable<T>? second) =>
        (first ?? ArraySegment<T>.Empty).Concat(second ?? ArraySegment<T>.Empty);

    public string Notation => Project.ToString();

    public void RunBuild()
    {
        Log<Module>.WithExceptionLogger(() =>
        {
            var oldwkdir = Environment.CurrentDirectory;
            Log<KScrBuild>.At(LogLevel.Debug, $"Set working Directory for module: {Environment.CurrentDirectory = RootDir.FullName}");
            var cmd = new CmdCompile()
            {
                Args = ArraySegment<string>.Empty,
                Classpath = Dependencies.Select(dep =>
                    DependencyManager.Resolve(this, dep) ?? throw new Exception("Unable to continue with build")),
                Output = new DirectoryInfo(ModulesInfo?.Build.Output ?? Build.Output ?? "build/classes/"),
                PkgBase = ModulesInfo?.Build.BasePackage ?? Build.BasePackage,
                Source = ModulesInfo?.Build.Sources ?? Build.Sources ?? "src/main/",
                System = (ModulesInfo?.Build.BasePackage ?? Build.BasePackage) == "org.comroid.kscr"
            };
            KScrStarter.CopyProps(cmd);
            if (!cmd.System)
            {
                KScrStarter.LoadStdPackage();
                KScrStarter.LoadClasspath(cmd);
            }

            Log<Module>.At(LogLevel.Config, $"Compiling source '{cmd.Source}' into '{cmd.Output}'...");
            var compileTime = KScrStarter.CompileSource(cmd, cmd.PkgBase);
            if (KScrStarter.VM.CompilerErrors.Count > 0)
                foreach (var error in KScrStarter.VM.CompilerErrors)
                    Log<Module>.At(LogLevel.Error, "Compiler Error:\n" + error);
            var ioTime = KScrStarter.WriteClasses(cmd);
            Log<Module>.At(LogLevel.Info, $"Build {Notation} succeeded; {KScrStarter.IOTimeString(compileTime, ioTime: ioTime)}");
            Environment.CurrentDirectory = oldwkdir;
        }, $"Build {Notation} failed with exception");
    }
    
    public override string ToString() =>
        $"Module {Project} ({Repositories.Count()} repositories; {Dependencies.Count()} dependencies)";
}