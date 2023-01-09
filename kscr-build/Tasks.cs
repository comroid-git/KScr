namespace KScr.Build;

public abstract class BuildTask
{
    public string Name { get; }
    public TaskCategory Category { get; }
    public HashSet<string> DependsOn { get; } = new();
    public virtual HashSet<string> Inputs { get; } = new();
    public virtual HashSet<string> Outputs { get; } = new();

    protected BuildTask(string name, TaskCategory category = TaskCategory.None)
    {
        Name = name;
        Category = category;
    }

    public abstract void Execute(TaskManager manager, Module module);
}

public abstract class TaskContainer : HashSet<BuildTask>
{
    protected TaskContainer()
    {
        Add(new InfoTask());
        Add(new DependenciesTask());
    }

    private class InfoTask : BuildTask
    {
        public InfoTask() : base("info", TaskCategory.Insight)
        {
        }

        public override void Execute(TaskManager manager, Module module)
        {
            throw new NotImplementedException();
        }
    }
    
    private class DependenciesTask : BuildTask
    {
        public DependenciesTask() : base("dependencies", TaskCategory.Insight)
        {
        }

        public override void Execute(TaskManager manager, Module module)
        {
            throw new NotImplementedException();
        }
    }
}

public sealed class TaskManager : TaskContainer
{
    public static readonly TaskManager Instance = new();

    private TaskManager()
    {
        Add(new TasksTask());
    }
    
    public IEnumerable<BuildTask> Tasks(Module module) => this.Concat(module);

    private class TasksTask : BuildTask
    {
        public TasksTask() : base("tasks", TaskCategory.Insight)
        {
        }

        public override void Execute(TaskManager manager, Module module)
        {
            Console.WriteLine($"All tasks in {module}:");
            foreach (var task in manager.Tasks(module))
                Console.WriteLine($"\t- {task.Name}" +
                                  (task.Category != TaskCategory.None ? $" ({task.Category})" : string.Empty));
        }
    }
}

public enum TaskCategory
{
    None,
    Build,
    Insight
}