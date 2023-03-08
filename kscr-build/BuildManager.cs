using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Aspose.Zip;
using Aspose.Zip.Saving;
using comroid.common;
using KScr.Core;

namespace KScr.Build;

public sealed class BuildManager
{
    public static readonly string CacheDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".kbuild");
}

public sealed class DependencyManager
{
    public static readonly string LocalRepositoryDir = Path.Combine(BuildManager.CacheDir, "localRepository");
    public static readonly string LibrariesDir = Path.Combine(BuildManager.CacheDir, "libs");
    private static readonly HttpClient http = new();

    #region Publishing

    public static void PublishToLocalRepository(Module module)
    {
        var dir = Path.Combine(LocalRepositoryDir, string.Join(Path.DirectorySeparatorChar, module.Project.Strings()));

        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        module.BuildLibAndDesc(dir);
        
        Log<DependencyManager>.At(LogLevel.Info, $"Published {module.Notation} to local repository");
    }

    #endregion

    private static string CombineUrl(params string[] arr)
    {
        return arr.Aggregate(string.Empty, (left, right) => left == string.Empty ? right : left.EndsWith('/') ? left + right : left + '/' + right);
    }

    #region Resolving

    public static DirectoryInfo? Resolve(Module module, DependencyInfo dep)
    {
        Exception? ex = null;
        try
        {
            var useLatest = new[] { '+' }.Any(dep.Version.Contains);
            var urlPath = string.Join("/", useLatest ? dep.Strings()[..^1] : dep.Strings()) + '/';
            foreach (var repo in new[] { LocalRepositoryDir, LibrariesDir })
            {
                if (Directory.Exists(Path.Combine(repo, urlPath)))
                { // handle as project link
                    if (useLatest)
                    {
                        var name = Directory.EnumerateDirectories(Path.Combine(repo, urlPath))
                            .Select(path => new DirectoryInfo(path))
                            .Where(dir => Regex.IsMatch(dir.Name, "\\d+\\.\\d+(\\.\\d+)?(\\.\\d+)?"))
                            .MinBy(dir => new Version(dir.Name))?.Name;
                        if (name != null)
                            urlPath = CombineUrl(urlPath, name);
                    }

                    var descFile = repo + urlPath + Path.DirectorySeparatorChar + RuntimeBase.ModuleFile;
                    if (new FileInfo(descFile) is { Exists: true } desc&&
                        JsonSerializer.Deserialize<ModuleInfo>(File.OpenRead(desc.FullName)) is { } mod)
                        return ResolveModule(module, mod, urlPath) ??
                               Log<DependencyManager>.At<DirectoryInfo?>(LogLevel.Warning,
                                   $"Could not resolve dependency {dep}");
                }
            }

            foreach (var repo in module.Repositories)
            {
                var moduleUrl = CombineUrl(repo.Url, urlPath);
                var response =
                    http.Send(new HttpRequestMessage(HttpMethod.Get, CombineUrl(moduleUrl, RuntimeBase.ModuleFile)));
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
        catch (Exception e)
        {
            ex = e;
        }
        Log<DependencyManager>.At(LogLevel.Warning, $"Could not resolve dependency {dep} {(ex == null ? string.Empty : ";\r\n" + ex)}");
        return null;
    }

    private static DirectoryInfo? ResolveModule(Module project, ModuleInfo mod, string urlPath,
        string? url = null)
    {
        foreach (var dependency in mod.Dependencies ?? ArraySegment<DependencyInfo>.Empty)
            Resolve(project, dependency);
        if (url != null)
        {
            // need to cache the module
            var response = http.Send(new HttpRequestMessage(HttpMethod.Get,
                CombineUrl(url, urlPath, "library" + RuntimeBase.ModuleFileExt)));
            if (response.StatusCode == HttpStatusCode.OK)
            {
                using var za = new ZipArchive(response.Content.ReadAsStream(), ZipArchiveMode.Read);
                var outputPath = Path.Combine(LibrariesDir, urlPath);
                za.ExtractToDirectory(outputPath);
                return new DirectoryInfo(outputPath);
            }

            Log<DependencyManager>.At(LogLevel.Error,
                $"Unexpected response when resolving dependency {mod.Project}: {response.StatusCode}");
        }
        return new[] { LocalRepositoryDir, LibrariesDir }
                .Select(repo => repo + urlPath)
                .Where(Directory.Exists)
                .Select(path => new DirectoryInfo(path))
                .FirstOrDefault();
    }

    #endregion
}