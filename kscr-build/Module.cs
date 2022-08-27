namespace KScr.Build;

public class Module
{
    public readonly ModuleInfo ModulesInfo;
    public readonly ModuleInfo ModuleInfo;
    public readonly DirectoryInfo RootDir;

    public Module(ModuleInfo modulesInfo, ModuleInfo moduleInfo, DirectoryInfo rootDir)
    {
        ModulesInfo = modulesInfo;
        ModuleInfo = moduleInfo;
        RootDir = rootDir;
    }

    public ProjectInfo Project => ModulesInfo.Project + ModuleInfo.Project;
    public BuildInfo Build => ModulesInfo.Build + ModuleInfo.Build;
    public IEnumerable<RepositoryInfo> Repositories => Concat(ModulesInfo.Repositories, ModuleInfo.Repositories);
    public IEnumerable<DependencyInfo> Dependencies => Concat(ModulesInfo.Dependencies, ModuleInfo.Dependencies);

    private IEnumerable<T> Concat<T>(IEnumerable<T>? first, IEnumerable<T>? second) =>
        (first ?? Array.Empty<T>()).Concat(second ?? Array.Empty<T>());
}