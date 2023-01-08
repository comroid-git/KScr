using System.IO.Compression;
using System.Net;
using System.Text.Json;
using comroid.csapi.common;
using KScr.Core;
using KScr.Runtime;

namespace KScr.Build;

public sealed class BuildManager
{
    public static readonly string CacheDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".kbuild");
}

public sealed class DependencyManager
{
    public static readonly string ProjectsDir = Path.Combine(BuildManager.CacheDir, "projects");
    public static readonly string LibrariesDir = Path.Combine(BuildManager.CacheDir, "libs");
    private static readonly HttpClient http = new();

    public static DirectoryInfo? Resolve(Module module, DependencyInfo dep)
    { // todo: this code needs testing
        var urlPath = string.Join("/", dep.Strings()) + '/';

        if (Directory.Exists(Path.Combine(ProjectsDir, urlPath)) &&
            new FileInfo(Path.Combine(ProjectsDir, urlPath, "location")) is { Exists: true } link &&
            new DirectoryInfo(File.ReadAllText(link.FullName).Trim()) is { Exists: true } dir)
            return dir; // handle as project link
        else
        {
            if (Directory.Exists(Path.Combine(LibrariesDir, urlPath)) &&
                new FileInfo(Path.Combine(LibrariesDir, urlPath, KScrBuild.ModuleFile)) is { Exists: true } desc &&
                JsonSerializer.Deserialize<ModuleInfo>(File.OpenRead(desc.FullName)) is { } mod)
                return ResolveModule(module, mod, urlPath) ??
                       Log<DependencyManager>.At<DirectoryInfo?>(LogLevel.Warning,
                           $"Could not resolve dependency {dep}");
            else
                foreach (var repo in module.Repositories)
                {
                    var moduleUrl = CombineUrl(repo.Url, urlPath);
                    var response =
                        http.Send(new HttpRequestMessage(HttpMethod.Get, CombineUrl(moduleUrl, KScrBuild.ModuleFile)));
                    switch (response.StatusCode)
                    {
                        case HttpStatusCode.OK:
                            var uncached = JsonSerializer.Deserialize<ModuleInfo>(response.Content.ReadAsStream())!;
                            return ResolveModule(module, uncached, urlPath, moduleUrl) ??
                                   Log<DependencyManager>.At<DirectoryInfo?>(LogLevel.Warning,
                                       $"Could not resolve dependency {dep}");
                        case HttpStatusCode.NotFound:
                            Log<DependencyManager>.At(LogLevel.Trace,
                                $"Could not resolve dependency {dep} in repository {repo}");
                            continue;
                        default:
                            Log<DependencyManager>.At(LogLevel.Error, $"Unexpected response: {response.StatusCode}");
                            break;
                    }
                }
        }
        
        Log<DependencyManager>.At(LogLevel.Warning, $"Could not resolve dependency {dep}");
        return null;
    }

    private static DirectoryInfo? ResolveModule(Module project, ModuleInfo mod, string? urlPath = null, string? url = null)
    {
        foreach (var dependency in mod.Dependencies ?? ArraySegment<DependencyInfo>.Empty)
            Resolve(project, dependency);
        if (urlPath != null && url != null)
        {
            // need to cache the module
            var response = http.Send(new HttpRequestMessage(HttpMethod.Get, CombineUrl(url, urlPath, "library" + RuntimeBase.ModuleFileExt)));
            if (response.StatusCode == HttpStatusCode.OK)
            {
                using var za = new ZipArchive(response.Content.ReadAsStream(), ZipArchiveMode.Read);
                var outputPath = Path.Combine(LibrariesDir, urlPath);
                za.ExtractToDirectory(outputPath);
                return new DirectoryInfo(outputPath);
            }
            else Log<DependencyManager>.At(LogLevel.Error, $"Unexpected response when resolving dependency {mod.Project}: {response.StatusCode}");
        }
        return null;
    }

    private static string CombineUrl(params string[] arr) => arr.Aggregate(string.Empty, (left, right) => left.EndsWith('/') ? left + right : left + '/' + right);
}