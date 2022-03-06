using System.Collections.Generic;
using System.IO;
using CommandLine;

namespace KScr.Runtime
{
    public interface IGenericCmd
    {
        [Option(HelpText = "Whether to keep the program open until a key is pressed after the action")]
        public bool Confirm { get; set; }
        [Option(HelpText = "Whether to run in Debug mode")]
        public bool Debug { get; set; }
    }
    public interface IOutputCmd : IGenericCmd
    {
        [Option(HelpText = "The path of the output directory. Defaults to ./build/compile")]
        public DirectoryInfo? Output{ get; set; }
    }
    public interface IClasspathCmd : IGenericCmd
    {
        [Option(HelpText = "The compile classpath to load before compilation")]
        public IEnumerable<DirectoryInfo> Classpath { get; set; }
    }
    public interface ISourcesCmd : IGenericCmd
    {
        [Option(HelpText = "The source paths to compile", Required = true)]
        public IEnumerable<string> Sources { get; set; }
    }
    
    [Verb("compile", HelpText = "Compile and Write one or more .kscr Files to .kbin Files")]
    public sealed class CmdCompile : IOutputCmd, IClasspathCmd, ISourcesCmd
    {
        [Option(HelpText = "Whether this operation is compiling the system package")]
        public bool System { get; set; }

        public DirectoryInfo? Output { get; set; }
        public IEnumerable<DirectoryInfo> Classpath { get; set; }
        public IEnumerable<string> Sources { get; set; }
        public bool Confirm { get; set; }
        public bool Debug { get; set; }
    }
    
    [Verb("execute", HelpText = "Compile and Execute one or more .kscr Files")]
    public sealed class CmdExecute : IClasspathCmd, ISourcesCmd, IOutputCmd
    {
        public DirectoryInfo? Output { get; set; }
        public IEnumerable<DirectoryInfo> Classpath { get; set; }
        public IEnumerable<string> Sources { get; set; }
        public bool Confirm { get; set; }
        public bool Debug { get; set; }
    }
    
    [Verb("run", HelpText = "Load and Execute one or more .kbin Files")]
    public sealed class CmdRun : IClasspathCmd
    {
        public IEnumerable<DirectoryInfo> Classpath { get; set; }
        public bool Confirm { get; set; }
        public bool Debug { get; set; }
    }
}