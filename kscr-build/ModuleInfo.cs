using Newtonsoft.Json;

namespace KScr.Build;

public sealed class ModuleInfo
{
    [JsonProperty]
    public string? Inherit { get; set; }
    [JsonProperty]
    public ProjectInfo Project { get; set; }
    [JsonProperty]
    public BuildInfo Build { get; set; }
    [JsonProperty]
    public IEnumerable<RepositoryInfo>? Repositories { get; set; }
    [JsonProperty]
    public IEnumerable<DependencyInfo>? Dependencies { get; set; }
}

public sealed class ProjectInfo
{
    [JsonProperty]
    public string? Domain { get; set; }
    [JsonProperty]
    public string? Group { get; set; }
    [JsonProperty]
    public string? Id { get; set; }
}

public sealed class BuildInfo
{
    [JsonProperty]
    public string? Sources { get; set; }
    [JsonProperty]
    public string? Resources { get; set; }
    [JsonProperty]
    public string? Output { get; set; }
    [JsonProperty]
    public ExecutionInfo? Pre { get; set; }
    [JsonProperty]
    public ExecutionInfo? Post { get; set; }
}

public sealed class RepositoryInfo
{
    [JsonProperty]
    public string Name { get; set; }
    [JsonProperty]
    public string Url { get; set; }
}

public sealed class DependencyInfo
{
    [JsonProperty]
    public string? Domain { get; set; }
    [JsonProperty]
    public string? Group { get; set; }
    [JsonProperty]
    public string? Id { get; set; }
    [JsonProperty]
    public string? Scope { get; set; }
    [JsonProperty]
    public IEnumerable<DependencyInfo>? Exclude { get; set; }
}

public sealed class ExecutionInfo
{
    [JsonProperty]
    public string Name { get; set; }
    [JsonProperty]
    public string EntryPoint { get; set; }
}