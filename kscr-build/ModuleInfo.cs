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

    public string Notation => Project.ToString();
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
    public string? BasePackage { get; set; }
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
        BasePackage = @override.BasePackage?.StartsWith("+") ?? false
            ? inherit.BasePackage + '.' + @override.BasePackage.Substring(1)
            : @override.BasePackage ?? inherit.BasePackage,
        Sources = @override.Sources ?? inherit.Sources ?? "src/main/",
        Resources = @override.Resources ?? inherit.Resources ?? "src/resources/",
        Output = @override.Output ?? inherit.Output ?? "build/",
        Pre = @override.Pre ?? inherit.Pre,
        Post = @override.Post ?? inherit.Post,
    };
}

public sealed class RepositoryInfo
{
    [JsonProperty(Required = Required.Always)]
    public string Name { get; set; } = null!;
    [JsonProperty(Required = Required.Always)]
    public string Url { get; set; } = null!;

    public override string ToString() => $"{Name} ({Url})";
}

public sealed class DependencyInfo
{
    [Flags]
    public enum DepScope : byte
    {
        Compile = 0x1,
        Runtime = 0x2,
        Implementation = Compile | Runtime,
        Api = 0x4
    }
    
    [JsonProperty]
    public string? Domain { get; set; }
    [JsonProperty]
    public string? Group { get; set; }
    [JsonProperty]
    public string? Id { get; set; }
    [JsonProperty]
    public string Version { get; set; } = "+";
    [JsonProperty]
    public DepScope? Scope { get; set; } = DepScope.Implementation;
    [JsonProperty]
    public IEnumerable<DependencyInfo>? Exclude { get; set; }

    public string Notation => new ProjectInfo()
    {
        Domain = Domain,
        Group = Group,
        Id = Id,
        Version = Version
    }.ToString();

    public override string ToString() => new ProjectInfo()
    {
        Domain = Domain,
        Group = Group,
        Id = Id,
        Version = Version
    }.ToString();
}
