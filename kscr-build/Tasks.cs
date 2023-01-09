using System.Collections;

namespace KScr.Build;

public abstract class ProjTask
{
    public string Name { get; }
    public TaskCategory Category { get; }
    public HashSet<string> DependsOn { get; } = new();
    public virtual HashSet<string> Inputs { get; } = new();
    public virtual HashSet<string> Outputs { get; } = new();

    protected ProjTask(string name, TaskCategory category = TaskCategory.None)
    {
        Name = name;
        Category = category;
    }

    public abstract void Execute(TaskManager manager, Module module);
}

public abstract class TaskContainer : HashSet<ProjTask>
{
    public HashSet<TaskContainer> Parents { get; } = new();

    public IEnumerable<ProjTask> Tasks(TaskCategory? category = null, TaskContainer? secondary = null) =>
        TaskManager.Instance.Concat(this)
            .Concat(Parents.SelectMany(it => it))
            .Concat((IEnumerable<ProjTask>?)secondary ?? ArraySegment<ProjTask>.Empty)
            .Where(task => category == null || task.Category == category);

    public ProjTask? Task(string name, TaskContainer? secondary = null) =>
        Tasks(secondary: secondary).FirstOrDefault(task => task.Name == name);
    
    protected TaskContainer()
    {
        Add(new InfoTask());
        Add(new DependenciesTask());
    }

    private class InfoTask : ProjTask
    {
        public InfoTask() : base("info", TaskCategory.Insight)
        {
        }

        public override void Execute(TaskManager manager, Module module)
        {
            throw new NotImplementedException();
        }
    }
    
    private class DependenciesTask : ProjTask
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

    private class TasksTask : ProjTask
    {
        public TasksTask() : base("tasks", TaskCategory.Insight)
        {
        }

        public override void Execute(TaskManager manager, Module module)
        {
            Console.WriteLine($"All tasks in {module}:");
            foreach (var task in manager.Tasks(secondary: module))
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