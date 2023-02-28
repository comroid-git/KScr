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

    public void RunBuild(string? libDir = null)
    {
        log.RunWithExceptionLogger(() =>
        {
            (var oldwkdir, Environment.CurrentDirectory) = (Environment.CurrentDirectory, RootDir.FullName);

            var cmd = new CmdCompile()
            {
                Args = ArraySegment<string>.Empty,
                Classpath = Dependencies.Select(dep => DependencyManager.Resolve(this, dep)).Where(x => x != null)!,
                Output = new DirectoryInfo(Path.Combine(ModulesInfo?.Build.Output ?? Build.Output ?? KScrBuild.BuildDir, "classes")),
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
                KScrStarter.VM.PrintCompilerErrors(log);
                log.At(LogLevel.Warning, $"Build {Notation} could not finish due to unresolved compiler errors");
                return;
            }

            ioTime = KScrStarter.WriteClasses(cmd);
            
            skipBuild:
            ioTime += DebugUtil.Measure(() => BuildLibAndDesc(libDir));
            cmd.Output.UpdateMd5(KScrBuild.Md5Path);
            log.At(LogLevel.Info, $"Build {Notation} succeeded; {KScrStarter.IOTimeString(compileTime, ioTime: ioTime)}");
            Environment.CurrentDirectory = oldwkdir;
        }, "Build failed with exception", LogLevel.Error);
    }

    public void BuildLibAndDesc(string? dir = null!)
    {
        dir ??= Path.Combine(Build.Output ?? KScrBuild.BuildDir, "classes");
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

    public void BuildDist(CmdDist cmd)
    {
        var dist = Path.Combine(KScrBuild.BuildDir,
            "dist" + (cmd.Type == CmdDist.DistType.dir ? string.Empty : "." + cmd.Type));
        if (!KScrBuild.Rebuild && (cmd.Type == CmdDist.DistType.dir ? (FileSystemInfo) new FileInfo(dist) : new DirectoryInfo(dist)).IsUpToDate(KScrBuild.Md5Path))
        {
            log.At(LogLevel.Config, $"Build {cmd.Type} distribution not necessary, output is up to date");
            return;
        }

        // build lib
        RunBuild(dist);

        // copy windows starter
        if (new FileInfo(Path.Combine(RuntimeBase.SdkHome.FullName, "kscr-starter.exe")) is { Exists: true } exeWin)
            File.Copy(exeWin.FullName, Path.Combine(dist, Project.Id + ".exe"));
        else Log<KScrBuild>.At(LogLevel.Config, $"Could not include Windows starter in distribution");
        // copy linux starter
        if (new FileInfo(Path.Combine(RuntimeBase.SdkHome.FullName, "kscr-starter")) is { Exists: true } exeNix) 
            File.Copy(exeNix.FullName, Path.Combine(dist, Project.Id!));
        else Log<KScrBuild>.At(LogLevel.Config, $"Could not include Linux starter in distribution");

        switch (cmd.Type)
        {
            case CmdDist.DistType.dir:
                break;
            case CmdDist.DistType.zip:
            case CmdDist.DistType.gz:
                var distFile = new FileInfo(Path.Combine(KScrBuild.BuildDir, "dist." + cmd.Type));
                
                var zipFile = distFile.OpenWrite(); 
                var archive = new Archive();
                var close = new Container(){archive,zipFile};
                var moduleFile = new FileInfo(Path.Combine(dist, RuntimeBase.ModuleFile));
                Project.SaveToFile(moduleFile.FullName);
                foreach (var file in Directory
                             .EnumerateFiles(dist, "*" + RuntimeBase.BinaryFileExt, SearchOption.AllDirectories)
                             .Select(file => (FileSystemInfo)new FileInfo(file))
                             .Append(moduleFile))
                {
                    var isModuleFile = file.Name == RuntimeBase.ModuleFile;
                    var data = !isModuleFile ? File.Open(file.FullName, FileMode.Open, FileAccess.Read) : moduleFile.OpenRead();
                    close.Add(data);

                    // Add file to the archive
                    var relativePath = isModuleFile
                        ? Path.GetRelativePath(dist, file.FullName)
                        : Path.GetRelativePath(Build.Output ?? dist, file.FullName);
                    Log<DependencyManager>.At(LogLevel.Trace, $"Adding {file.FullName} to archive at path {relativePath}");
                    archive.CreateEntry(relativePath, data);
                }
                // ZIP file + cleanup
                archive.Save(zipFile);
                close.Dispose();
                distFile.UpdateMd5(KScrBuild.Md5Path);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(cmd.Type), cmd.Type, "Invalid Type");
        }
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