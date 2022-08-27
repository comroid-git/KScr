using Newtonsoft.Json;

namespace KScr.Build;

public sealed class ModuleInfo
{
    [JsonProperty]
    public ProjectInfo Project { get; set; } = new();
    [JsonProperty]
    public BuildInfo Build { get; set; } = new();
    [JsonProperty]
    public IEnumerable<RepositoryInfo>? Repositories { get; set; }
    [JsonProperty]
    public IEnumerable<DependencyInfo>? Dependencies { get; set; }

    public override string ToString() =>
        $"Module {Project} ({Repositories?.Count() ?? 0} repositories; {Dependencies?.Count() ?? 0} dependencies)";
}

public sealed class ProjectInfo
{
    [JsonProperty]
    public string? Domain { get; set; }
    [JsonProperty]
    public string? Group { get; set; }
    [JsonProperty]
    public string? Id { get; set; }
    [JsonProperty]
    public string? Version { get; set; }

    public static ProjectInfo operator +(ProjectInfo inherit, ProjectInfo @override) => new()
    {
        Domain = @override.Domain ?? inherit.Domain,
        Group = @override.Group ?? inherit.Group,
        Id = @override.Id ?? inherit.Id,
        Version = @override.Version ?? inherit.Version
    };

    public override string ToString()
    {
        var str = string.Empty;
        if (Domain != null)
            str += Domain;
        if (Group != null)
        {
            if (str.Length != 0)
                str += ':';
            str += Group;
        }
        if (Id != null)
        {
            if (str.Length != 0)
                str += ':';
            str += Id;
        }
        if (Version != null)
        {
            if (str.Length != 0)
                str += ':';
            str += Version;
        }
        return str;
    }
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
    public string? Pre { get; set; }
    [JsonProperty]
    public string? Post { get; set; }

    public static BuildInfo operator +(BuildInfo inherit, BuildInfo @override) => new()
    {
        Sources = @override.Sources ?? inherit.Sources,
        Resources = @override.Resources ?? inherit.Resources,
        Output = @override.Output ?? inherit.Output,
        Pre = @override.Pre ?? inherit.Pre,
        Post = @override.Post ?? inherit.Post,
    };
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
