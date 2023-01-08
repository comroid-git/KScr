using Aspose.Zip;
using comroid.csapi.common;
using KScr.Core;
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
                Classpath = Dependencies.Select(dep => DependencyManager.Resolve(this, dep)).Where(x => x != null)!,
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
                    Log<Module>.At(LogLevel.Error, "Compiler Error:\r\n" + error);
            var ioTime = KScrStarter.WriteClasses(cmd);
            Log<Module>.At(LogLevel.Info, $"Build {Notation} succeeded; {KScrStarter.IOTimeString(compileTime, ioTime: ioTime)}");
            Environment.CurrentDirectory = oldwkdir;
        }, $"Build {Notation} failed with exception");
    }

    public void SaveToFile(string lib)
    {
        // Create FileStream for output ZIP archive
        using var zipFile = File.Open(lib, FileMode.Create);
        using var archive = new Archive();
        var close = new Container();
        foreach (var file in Directory
                     .EnumerateFiles(Build.Output!, "*" + RuntimeBase.BinaryFileExt,
                         SearchOption.AllDirectories).Select(file => new FileInfo(file)))
        {
            var src = File.Open(file.FullName, FileMode.Open, FileAccess.Read);
            close.Add(src);
            // Add file to the archive
            var relativePath = Path.GetRelativePath(Build.Output!, file.FullName);
            Log<DependencyManager>.At(LogLevel.Trace, $"Adding {file.FullName} to archive at path {relativePath}");
            archive.CreateEntry(relativePath, src);
        }
        // ZIP file
        archive.Save(zipFile);
        close.Dispose();
    }

    public override string ToString() =>
        $"Module {Project} ({Repositories.Count()} repositories; {Dependencies.Count()} dependencies)";
}