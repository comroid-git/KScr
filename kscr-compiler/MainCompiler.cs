using System;
using System.IO;
using CommandLine;
using KScr.Lib;
using KScr.Lib.Bytecode;
using KScr.Lib.Model;

namespace KScr.Compiler
{
    public sealed class CompilerArgs
    {
        [Option("source", Required = true, HelpText = "The source directory")]
        public string SourceDir { get; set; } = null!;
        [Option("output", Required = false, HelpText = "The output directory; defaults to ./output/")]
        public string OutputDir { get; set; } = string.Empty;
    }
    
    public sealed class MainCompiler
    {
        public static void Compile(RuntimeBase vm, DirectoryInfo sourceDir, DirectoryInfo outputDir)
        {
            vm.Compiler.Compile(vm, sourceDir);
            Package.RootPackage.Write(outputDir);
        }

        public static void Run(CompilerArgs args)
        {
            var vm = new CompilerRuntime();
            var src = new DirectoryInfo(args.SourceDir);
            var output = new DirectoryInfo(args.OutputDir == string.Empty
                ? Path.Combine(Directory.GetCurrentDirectory(), "output")
                : args.OutputDir);
            if (!src.Exists)
                throw new ArgumentException("Source Directory does not exist: " + src);
            if (!output.Exists)
                output.Create();
            Compile(vm, src, output);
        }
    }
}