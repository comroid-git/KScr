namespace KScr.Build;

public abstract class Plugin : TaskContainer
{
    public string Name { get; }

    public Plugin(string name)
    {
        Name = name;
    }

    public void Apply(Module module) => module.Parents.Add(this);
}

public class BuildPlugin : Plugin
{
    private class BuildTask : ProjTask
    {
        public BuildTask() : base("build", TaskCategory.Build)
        {
        }

        public override void Execute(TaskManager manager, Module module)
        {
            module.RunBuild();
            module.SaveToFiles();
        }
    }

    public BuildPlugin(string? name = null) : base(name ?? "build")
    {
        Add(new BuildTask());
    }
}

public class DistPlugin : BuildPlugin
{
    private class DistTask : ProjTask {}

    public DistPlugin(string? name = null) : base(name ?? "dist")
    {
        Add(new DistTask());
    }
}

public class LibraryPlugin : DistPlugin
{
    private class PublishTask : ProjTask {}

    public LibraryPlugin(string? name = null) : base(name ?? "library")
    {
        Add(new PublishTask());
    }
}

public class ApplicationPlugin : DistPlugin
{
    private class RunTask : ProjTask {}

    public ApplicationPlugin(string? name = null) : base(name ?? "application")
    {
        Add(new RunTask());
    }
}