﻿using Aspose.Zip;
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
    public BuildInfo Build => ModulesInfo != null ? ModulesInfo.Build + ModuleInfo.Build : ModuleInfo.Build ;
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

            long compileTime = -1, ioTime = -1;
            if (cmd.Output.IsUpToDate(KScrBuild.Md5Path))
            {
                Log<Module>.At(LogLevel.Config, $"Build {Notation} not necessary; output dir is up-to-date");
                goto skipBuild;
            }

            KScrStarter.CopyProps(cmd);
            if (!cmd.System)
            {
                KScrStarter.LoadStdPackage();
                KScrStarter.LoadClasspath(cmd);
            }

            Log<Module>.At(LogLevel.Config, $"Compiling source '{cmd.Source}' into '{cmd.Output}'...");
            compileTime = KScrStarter.CompileSource(cmd, cmd.PkgBase);
            if (KScrStarter.VM.CompilerErrors.Count > 0)
                foreach (var error in KScrStarter.VM.CompilerErrors)
                    Log<Module>.At(LogLevel.Error, "Compiler Error:\r\n" + error);
            ioTime = KScrStarter.WriteClasses(cmd);
            
            skipBuild:
            ioTime += DebugUtil.Measure(() => SaveToFiles());
            cmd.Output.UpdateMd5(KScrBuild.Md5Path);
            Log<Module>.At(LogLevel.Info, $"Build {Notation} succeeded; {KScrStarter.IOTimeString(compileTime, ioTime: ioTime)}");
            Environment.CurrentDirectory = oldwkdir;
        }, $"Build {Notation} failed with exception");
    }

    public void SaveToFiles(string dir = null!)
    {
        dir ??= Build.Output ?? Path.Combine(Environment.CurrentDirectory, "build/classes/");
        // Create FileStream for output ZIP archive
        var lib = new FileInfo(Build.OutputLib ?? Path.Combine(dir, RuntimeBase.ModuleLibFile));
        if (lib.IsUpToDate(KScrBuild.Md5Path))
        {
            Log<Module>.At(LogLevel.Config, $"Create Package {Notation} not necessary; output library is up-to-date");
            return;
        }
        Log<Module>.At(LogLevel.Debug, $"Writing module {Notation} to file {lib.FullName}");
        if (lib.Exists)
            lib.Delete();
        using var zipFile = lib.OpenWrite();
        using var archive = new Archive();
        using var close = new Container();
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
        lib.UpdateMd5(KScrBuild.Md5Path);
        close.Dispose();
    }

    public override string ToString() =>
        $"Module {Project} ({Repositories.Count()} repositories; {Dependencies.Count()} dependencies)";
}