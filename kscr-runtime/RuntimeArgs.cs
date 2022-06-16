using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using CommandLine;
using KScr.Core;

namespace KScr.Runtime;

public interface IGenericCmd
{
    [Option(HelpText = "The Encoding to use")]
    public string? Encoding { get; set; }

    [Option(HelpText = "Whether to keep the program open until a key is pressed after the action")]
    public bool Confirm { get; set; }

    [Option(HelpText = "Whether to run in Debug mode")]
    public bool Debug { get; set; }

    [Option(HelpText = "Extra arguments to forward to psvm()", Separator = ' ')]
    public IEnumerable<string> Args { get; set; }
}

public interface IOutputCmd : IGenericCmd
{
    [Option(HelpText = "The path of the output directory. Defaults to ./build/compile")]
    public DirectoryInfo? Output { get; set; }

    [Option(HelpText = "The compression method to use. Defaults to 'none'; options are 'None', 'GZip' and 'ZLib'",
        Default = CompressionType.None)]
    public CompressionType Compression { get; set; }

    [Option(HelpText = "The compression level to use. Defaults to 'optimal'", Default = CompressionLevel.Optimal)]
    public CompressionLevel CompressionLevel { get; set; }
}

public interface IClasspathCmd : IGenericCmd
{
    [Option(HelpText = "The compile classpath to load before compilation")]
    public IEnumerable<DirectoryInfo> Classpath { get; set; }
}

public interface ISourcesCmd : IGenericCmd
{
    [Option(HelpText = "The source path to compile", Required = true)]
    public string Source { get; set; }
}

public interface IConfigCmd
{
    [Option(HelpText = "Whether to add the executable to the PATH environment variable")]
    public bool Install { get; set; }
}

[Verb("compile", HelpText = "Compile and Write one or more .kscr Files to .kbin Files")]
public sealed class CmdCompile : IOutputCmd, IClasspathCmd, ISourcesCmd
{
    [Option(Hidden = true)] public bool System { get; set; }

    public IEnumerable<DirectoryInfo> Classpath { get; set; }

    public DirectoryInfo? Output { get; set; }
    public CompressionType Compression { get; set; }
    public CompressionLevel CompressionLevel { get; set; }
    public string? Encoding { get; set; }
    public bool Confirm { get; set; }
    public bool Debug { get; set; }
    public IEnumerable<string> Args { get; set; }
    public string Source { get; set; }
}

[Verb("execute", HelpText = "Compile and Execute one or more .kscr Files")]
public sealed class CmdExecute : IClasspathCmd, ISourcesCmd, IOutputCmd
{
    public IEnumerable<DirectoryInfo> Classpath { get; set; }
    public string? Encoding { get; set; }
    public bool Confirm { get; set; }
    public bool Debug { get; set; }
    public IEnumerable<string> Args { get; set; }
    public DirectoryInfo? Output { get; set; }
    public CompressionType Compression { get; set; }
    public CompressionLevel CompressionLevel { get; set; }
    public string Source { get; set; }
}

[Verb("run", HelpText = "Load and Execute one or more .kbin Files")]
public sealed class CmdRun : IClasspathCmd
{
    public IEnumerable<DirectoryInfo> Classpath { get; set; }
    public string? Encoding { get; set; }
    public bool Confirm { get; set; }
    public bool Debug { get; set; }
    public IEnumerable<string> Args { get; set; }
}

[Verb("config", HelpText = "Configure your KScr Installation")]
public sealed class CmdConfig : IConfigCmd
{
    public bool Install { get; set; }
}
