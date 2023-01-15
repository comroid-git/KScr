using Aspose.Zip;
using comroid.csapi.common;
using KScr.Core;
using KScr.Runtime;

namespace KScr.Build;

public class Module
{
    private readonly Log log;
    public readonly ModuleInfo? ModulesInfo;
    public readonly ModuleInfo ModuleInfo;
    public readonly DirectoryInfo RootDir;

    public Module(ModuleInfo? modulesInfo, ModuleInfo moduleInfo, DirectoryInfo rootDir)
    {
        ModulesInfo = modulesInfo;
        ModuleInfo = moduleInfo;
        RootDir = rootDir;
        log = new Log(Notation);
    }

    public ProjectInfo Project => ModulesInfo != null ? ModulesInfo.Project + ModuleInfo.Project : ModuleInfo.Project;
    public BuildInfo Build => ModulesInfo != null ? ModulesInfo.Build + ModuleInfo.Build : ModuleInfo.Build ;
    public IEnumerable<RepositoryInfo> Repositories => Concat(ModulesInfo?.Repositories ?? ArraySegment<RepositoryInfo>.Empty, ModuleInfo.Repositories);
    public IEnumerable<DependencyInfo> Dependencies => Concat(ModulesInfo?.Dependencies ?? ArraySegment<DependencyInfo>.Empty, ModuleInfo.Dependencies);

    private IEnumerable<T> Concat<T>(IEnumerable<T>? first, IEnumerable<T>? second) =>
        (first ?? ArraySegment<T>.Empty).Concat(second ?? ArraySegment<T>.Empty);

    public string Notation => Project.ToString();

    public void RunBuild()
    {
        log.RunWithExceptionLogger(() =>
        {
            (var oldwkdir, Environment.CurrentDirectory) = (Environment.CurrentDirectory, RootDir.FullName);

            var cmd = new CmdCompile()
            {
                Args = ArraySegment<string>.Empty,
                Classpath = Dependencies.Select(dep => DependencyManager.Resolve(this, dep)).Where(x => x != null)!,
                Output = new DirectoryInfo(Path.Combine(ModulesInfo?.Build.Output ?? Build.Output ?? Path.Combine(Environment.CurrentDirectory, "build"), "classes")),
                PkgBase = ModulesInfo?.Build.BasePackage ?? Build.BasePackage,
                Source = ModulesInfo?.Build.Sources ?? Build.Sources ?? "src/main/",
                System = (ModulesInfo?.Build.BasePackage ?? Build.BasePackage) == "org.comroid.kscr"
            };

            long compileTime = -1, ioTime = -1;
            if (!KScrBuild.Rebuild && cmd.Output.IsUpToDate(KScrBuild.Md5Path))
            {
                log.At(LogLevel.Config, $"Build not necessary; output dir is up-to-date");
                goto skipBuild;
            }
            else log.At(LogLevel.Info, $"Building module...");

            KScrStarter.CopyProps(cmd);
            if (!cmd.System)
            {
                KScrStarter.LoadSystemPackage();
                KScrStarter.LoadClasspath(cmd);
            }

            log.At(LogLevel.Config, $"Compiling source '{cmd.Source}' into '{cmd.Output}'...");
            compileTime = KScrStarter.CompileSource(cmd, cmd.PkgBase);
            if (KScrStarter.VM.CompilerErrors.Count > 0)
            {
                foreach (var error in KScrStarter.VM.CompilerErrors)
                    log.At(LogLevel.Error, "Compiler Error:\r\n" + error);
                throw new Exception("There were Compiler Errors");
            }

            ioTime = KScrStarter.WriteClasses(cmd);
            
            skipBuild:
            ioTime += DebugUtil.Measure(() => SaveToFiles());
            cmd.Output.UpdateMd5(KScrBuild.Md5Path);
            log.At(LogLevel.Info, $"Build {Notation} succeeded; {KScrStarter.IOTimeString(compileTime, ioTime: ioTime)}");
            Environment.CurrentDirectory = oldwkdir;
        }, $"Build failed with exception", LogLevel.Error);
    }

    public void SaveToFiles(string dir = null!)
    {
        dir ??= Path.Combine(Build.Output ?? Path.Combine(Environment.CurrentDirectory, "build"), "classes");
        // Create FileStream for output ZIP archive
        var lib = new FileInfo(Build.OutputLib ?? Path.Combine(dir, RuntimeBase.ModuleLibFile));
        if (!KScrBuild.Rebuild && lib.IsUpToDate(KScrBuild.Md5Path))
        {
            log.At(LogLevel.Config, $"Create Package {Notation} not necessary; output library is up-to-date");
            return;
        }
        log.At(LogLevel.Debug, $"Writing module {Notation} to file {lib.FullName}");
        if (lib.Exists)
            lib.Delete();
        var zipFile = lib.OpenWrite(); 
        var archive = new Archive();
        var close = new Container(){archive,zipFile};
        var moduleFile = new FileInfo(Path.Combine(dir, RuntimeBase.ModuleFile));
        Project.SaveToFile(moduleFile.FullName);
        foreach (var file in Directory
                     .EnumerateFiles(dir, "*" + RuntimeBase.BinaryFileExt, SearchOption.AllDirectories)
                     .Select(file => (FileSystemInfo)new FileInfo(file))
                     .Append(moduleFile))
        {
            var isModuleFile = file.Name == RuntimeBase.ModuleFile;
            var data = !isModuleFile ? File.Open(file.FullName, FileMode.Open, FileAccess.Read) : moduleFile.OpenRead();
            close.Add(data);

            // Add file to the archive
            var relativePath = isModuleFile
                ? Path.GetRelativePath(dir, file.FullName)
                : Path.GetRelativePath(Build.Output ?? dir, file.FullName);
            Log<DependencyManager>.At(LogLevel.Trace, $"Adding {file.FullName} to archive at path {relativePath}");
            archive.CreateEntry(relativePath, data);
        }
        // ZIP file + cleanup
        archive.Save(zipFile);
        close.Dispose();
        lib.UpdateMd5(KScrBuild.Md5Path);
    }

    public override string ToString() =>
        $"Module {Project} ({Repositories.Count()} repositories; {Dependencies.Count()} dependencies)";

    public bool DependsOn(Module other) => Dependencies.Any(dep
        => dep.Domain == other.Project.Domain
           // and group equal
           && dep.Group == other.Project.Group
           // and id equal
           && dep.Id == other.Project.Id
           // and versions compatible
           && dep.GetVersion() >= Project.GetVersion());
}