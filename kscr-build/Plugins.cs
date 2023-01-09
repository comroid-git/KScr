namespace KScr.Build;

public abstract class Plugin : TaskContainer
{
    public string Name { get; }
    public HashSet<string> DependsOn { get; }

    public Plugin(string name, params string[] dependsOn)
    {
        Name = name;
        DependsOn = new HashSet<string>(dependsOn);
    }

    public void Apply(Module module) => module.Parents.Add(this);
}

public class BuildPlugin : Plugin
{
    private class BuildTask : ProjTask {}

    public BuildPlugin() : base("build")
    {
        Add(new BuildTask());
    }
}

public class DistPlugin : Plugin
{
    private class DistTask : ProjTask {}

    public DistPlugin() : base("dist", "build")
    {
        Add(new DistTask());
    }
}

public class LibraryPlugin : Plugin
{
    private class PublishTask : ProjTask {}

    public LibraryPlugin() : base("library", "dist")
    {
        Add(new PublishTask());
    }
}

public class ApplicationPlugin : Plugin
{
    private class RunTask : ProjTask {}

    public ApplicationPlugin() : base("application", "dist")
    {
        Add(new RunTask());
    }
}