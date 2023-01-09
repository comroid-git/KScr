using System.Text.Json;
using System.Text.Json.Serialization;

namespace KScr.Build;

public sealed class ModuleInfo
{
    [JsonPropertyName("project")]
    public ProjectInfo Project { get; set; } = new();
    [JsonPropertyName("build")]
    public BuildInfo Build { get; set; } = new();
    [JsonPropertyName("repositories")]
    public IEnumerable<RepositoryInfo>? Repositories { get; set; }
    [JsonPropertyName("dependencies")]
    public IEnumerable<DependencyInfo>? Dependencies { get; set; }
    [JsonPropertyName("publishing")]
    public PublishingInfo Publishing { get; set; } = new();
    [JsonPropertyName("mainClassName")]
    public string? MainClassName { get; set; }

    public string Notation => Project.ToString();
    public override string ToString() =>
        $"Module {Project} ({Repositories?.Count() ?? 0} repositories; {Dependencies?.Count() ?? 0} dependencies)";
}

public sealed class ProjectInfo : DependencyInfo
{
    public static ProjectInfo operator +(ProjectInfo inherit, ProjectInfo @override) => new()
    {
        Domain = @override.Domain ?? inherit.Domain,
        Group = @override.Group ?? inherit.Group,
        Id = @override.Id ?? inherit.Id,
        Version = @override.Version ?? inherit.Version
    };

    public override DepScope? Scope => DepScope.api;
    public override IEnumerable<DependencyInfo>? Exclude => ArraySegment<DependencyInfo>.Empty;

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

    public void SaveToFile(string file)
    {
        File.WriteAllText(file, JsonSerializer.Serialize(this, typeof(DependencyInfo)));
    }
}

public sealed class BuildInfo
{
    [JsonPropertyName("base_package")]
    public string? BasePackage { get; set; }
    [JsonPropertyName("sources")]
    public string? Sources { get; set; }
    [JsonPropertyName("resources")]
    public string? Resources { get; set; }
    [JsonPropertyName("output")]
    public string? Output { get; set; }
    [JsonPropertyName("outputLib")]
    public string? OutputLib { get; set; }
    [JsonPropertyName("pre")]
    public string? Pre { get; set; }
    [JsonPropertyName("post")]
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

public sealed class PublishingInfo
{
    [JsonPropertyName("repositories")]
    public IEnumerable<RepositoryInfo>? Repositories { get; set; }
}

public sealed class RepositoryInfo
{
    [JsonPropertyName("name")]
    public string? Name { get; set; } = null!;
    [JsonPropertyName("url")]
    public string Url { get; set; } = null!;
    [JsonPropertyName("username")]
    public string? Username { get; set; } = null;
    [JsonPropertyName("password")]
    public string? Password { get; set; } = null;

    public override string ToString() => $"{Name} ({Url})";
}

public class DependencyInfo
{
    [Flags]
    public enum DepScope : byte
    {
        compile = 0x1,
        runtime = 0x2,
        implementation = compile | runtime,
        api = 0x4
    }
    
    [JsonPropertyName("domain")]
    public virtual string? Domain { get; set; }
    [JsonPropertyName("group")]
    public virtual string? Group { get; set; }
    [JsonPropertyName("id")]
    public virtual string? Id { get; set; }
    [JsonPropertyName("version")]
    public virtual string Version { get; set; } = "+";
    [JsonPropertyName("scope")]
    public virtual DepScope? Scope { get; set; } = DepScope.implementation;
    [JsonPropertyName("exclude")]
    public virtual IEnumerable<DependencyInfo>? Exclude { get; set; }

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

    public string[] Strings() => 
        (Domain?.Split('.') ?? ArraySegment<string>.Empty)
        .Concat(Group?.Split('.') ?? ArraySegment<string>.Empty)
        .Concat(Id?.Split('.') ?? ArraySegment<string>.Empty)
        .Append(Version)
        .ToArray();
}
