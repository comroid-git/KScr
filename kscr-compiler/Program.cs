using System.IO;
using System.Linq;
using CommandLine;

namespace KScr.Compiler
{

    public static class Program
    {
        public static void Main(string[] args)
        {
            if (Directory.Exists(args[0]))
                MainCompiler.Run(new CompilerArgs{SourceDir = args[0]});
            else Parser.Default.ParseArguments<CompilerArgs>(args).WithParsed(MainCompiler.Run);
        }

        public static void CreateAll(this DirectoryInfo dir)
        {
            if (!dir.Parent?.Exists ?? false)
                dir.Parent?.CreateAll();
            if (!dir.Exists)
                dir.Create();
        }
    }
}