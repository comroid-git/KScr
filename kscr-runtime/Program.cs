using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CommandLine;
using KScr.Core;
using KScr.Core.Bytecode;
using KScr.Core.Model;
using KScr.Core.Std;
using KScr.Core.Store;
using KScr.Core.Util;

namespace KScr.Runtime;

public class Program
{
    public static readonly KScrRuntime VM;

    private static readonly string
        DefaultOutput = Path.Combine(Directory.GetCurrentDirectory(), "build", "compile");

    private static readonly string
        StdPackageLocation = Path.Combine(RuntimeBase.SdkHome.FullName, "std");

    static Program()
    {
        VM = new KScrRuntime();
        VM.Initialize();
    }

    public static int Main(params string[] args)
    {
        var stack = RuntimeBase.MainStack;
        long compileTime = -1, executeTime = -1, ioTime = -1;

        Parser.Default.ParseArguments<CmdCompile, CmdExecute, CmdRun, CmdConfig>(args)
            .WithParsed<CmdCompile>(cmd =>
            {
                CopyProps(cmd);
                if (!cmd.System)
                {
                    LoadStdPackage();
                    LoadClasspath(cmd);
                }

                compileTime = CompileSource(cmd, cmd.PkgBase);
                if (VM.CompilerErrors.Count > 0)
                    return;
                ioTime = WriteClasses(cmd);
            })
            .WithParsed<CmdExecute>(cmd =>
            {
                CopyProps(cmd);
                LoadStdPackage();
                LoadClasspath(cmd);

                compileTime = CompileSource(cmd, cmd.PkgBase);
                if (VM.CompilerErrors.Count > 0)
                    return;
                if (cmd.Output != null)
                    ioTime = WriteClasses(cmd);
                executeTime = Execute(out stack);
            })
            .WithParsed<CmdRun>(cmd =>
            {
                CopyProps(cmd);
                LoadStdPackage();

                ioTime = LoadClasspath(cmd);
                VM.LateInitializeNonPrimitives(stack);
                executeTime = Execute(out stack);
            })
            .WithParsed<CmdConfig>(cmd =>
            {
                if (cmd.Install)
                    Installer.CheckInstallation();
            });

        HandleErrors();

        return HandleExit(stack.State, stack.Omg?.Value, compileTime, executeTime, ioTime, RuntimeBase.ConfirmExit);
    }

    private static void CopyProps(IGenericCmd cmd)
    {
        RuntimeBase.Encoding = Encoding.GetEncoding(cmd.Encoding ?? "ASCII");
        RuntimeBase.ConfirmExit = cmd.Confirm;
        RuntimeBase.DebugMode = cmd.Debug;
        RuntimeBase.ExtraArgs = cmd.Args.ToArray();
        if (cmd is IOutputCmd output)
        {
            VM.CompressionType = output.Compression;
            VM.CompressionLevel = output.CompressionLevel;
        }
    }

    private static void LoadStdPackage()
    {
        // load std package
        //VM.Load(StdPackageLocation);
        Package.ReadAll(VM, new DirectoryInfo(StdPackageLocation));
    }

    private static long LoadClasspath(IClasspathCmd cmd)
    {
        // load classpath packages
        if (cmd.Classpath.Count(dir => dir.Exists) == 0)
            return -1;
        var ioTime = RuntimeBase.UnixTime();
        foreach (var classpath in cmd.Classpath.Where(dir => dir.Exists))
            //VM.Load(classpath.FullName);
            Package.ReadAll(VM, classpath);
        ioTime = RuntimeBase.UnixTime() - ioTime;
        return ioTime;
    }

    public static long CompileSource(ISourcesCmd cmd, string? basePackage = null)
    {
        var compileTime = RuntimeBase.UnixTime();
        VM.CompileSource(cmd.Source, basePackage);
        compileTime = RuntimeBase.UnixTime() - compileTime;
        return compileTime;
    }

    private static long WriteClasses<TC>(TC cmd) where TC : ISourcesCmd, IOutputCmd
    {
        var ioTime = RuntimeBase.UnixTime();
        WriteClasses(cmd.Output ?? new DirectoryInfo(DefaultOutput), new[] { cmd.Source }.SelectMany(path =>
            Directory.Exists(path)
                ? new DirectoryInfo(path).EnumerateFiles("*.kscr", SearchOption.AllDirectories)
                : new[] { new FileInfo(path) }));
        ioTime = RuntimeBase.UnixTime() - ioTime;
        return ioTime;
    }

    private static void WriteClasses(DirectoryInfo output, IEnumerable<FileInfo> sources)
    {
        if (output.Exists)
            output.Delete(true);
        Package.RootPackage.Write(VM, output, sources.Select(f => VM.FindClassInfo(f)).ToArray(), new StringCache());
    }

    public static long Execute(out Stack stack, string? mainClassName = null)
    {
        var executeTime = RuntimeBase.UnixTime();
        stack = VM.Execute(mainClassName);
        executeTime = RuntimeBase.UnixTime() - executeTime;
        return executeTime;
    }

    private static StatementComponent BuildProgramArgsParams(string[] args)
    {
        var comp = new StatementComponent
        {
            Type = StatementComponentType.Code,
            CodeType = BytecodeType.ParameterExpression,
            InnerCode = new ExecutableCode()
        };
        foreach (var str in args)
            comp.InnerCode.Main.Add(new Statement
            {
                Type = StatementComponentType.Expression,
                CodeType = BytecodeType.LiteralString,
                Main =
                {
                    new StatementComponent
                    {
                        Type = StatementComponentType.Expression,
                        CodeType = BytecodeType.LiteralString,
                        Arg = str
                    }
                }
            });
        return comp;
    }

    private static void StdIoMode()
    {
        throw new NotImplementedException("StdIoMode currently not supported");

        /*
        Console.WriteLine("Entering StdIoMode - Only Expressions are allowed");
        VM.StdIoMode = true;

        var compiler = new StatementCompiler();
        var contextBase = new CompilerContext();
        var output = VM.ConstantVoid;

        while (true)
        {
            Console.Write("kscr> ");
            string? expr = Console.ReadLine();
            if (string.IsNullOrEmpty(expr))
                continue;
            switch (expr)
            {
                case "exit":
                    return;
                case "clear":
                    Console.Clear();
                    continue;
            }

            string code = "stdio << " + expr + ';';

            long compileTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            var tokens = new TokenContext(new Tokenizer().Tokenize(Directory.GetCurrentDirectory(), code));
            var context = new CompilerContext(contextBase, tokens, CompilerType.CodeStatement);
            AbstractCompiler.CompilerLoop(VM, compiler, ref context);
            compileTime = DateTimeOffset.Now.ToUnixTimeMilliseconds() - compileTime;

            long executeTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            context.ExecutableCode.Evaluate(VM, RuntimeBase.MainStack);
            executeTime = DateTimeOffset.Now.ToUnixTimeMilliseconds() - executeTime;
            context.Clear();

            HandleResult(State.Return, output.Value, compileTime, executeTime);
        }
        */
    }

    private static void HandleResult(State state, IObject? result, long compileTime, long executeTime)
    {
        Console.WriteLine($"State: {state.ToString()} - Compile: {compileTime}ms - Execute: {executeTime}ms");
        //Console.WriteLine($"Type: {result?.Type} - Value: {result?.ToString(0)}");
    }

    private static void HandleErrors()
    {
        if (VM.CompilerErrors.Count > 0)
        {
            foreach (var compilerError in VM.CompilerErrors)
                Console.Error.WriteLine(compilerError.Message);
            var plural = VM.CompilerErrors.Count != 1;
            Console.Error.WriteLine(
                $"There {(plural ? "were" : "was")} {VM.CompilerErrors.Count} compilation error{(plural ? 's' : string.Empty)}.");
        }
    }

    private static int HandleExit(State state, IObject? result, long compileTime = -1, long executeTime = -1,
        long ioTime = -1, bool pressToExit = false)
    {
        if (compileTime != -1)
            Console.Write($"Compile took {(double)compileTime / 1000:#,##0.00}ms");
        if (compileTime != -1 && (ioTime != -1 || executeTime != -1))
            Console.Write("; ");
        if (ioTime != -1)
            Console.Write($"Read/Write took {(double)ioTime / 1000:#,##0.00}ms");
        if (ioTime != -1 && executeTime != -1)
            Console.Write("; ");
        if (executeTime != -1)
            Console.Write($"Execute took {(double)executeTime / 1000:#,##0.00}ms");
        if (compileTime != -1 || executeTime != -1)
            Console.WriteLine();

        switch (state)
        {
            case State.Normal:
                Console.Write("Program stopped ");
                break;
            case State.Return:
                Console.Write("Program finished ");
                break;
            case State.Throw:
                Console.Write("Program failed ");
                break;
        }

        if (result is Numeric num)
        {
            var rtn = num.IntValue;
            Console.WriteLine($"with exit code {rtn} (0x{rtn:X8})");
            return rtn;
        }

        Console.Write($"with exit code {RuntimeBase.ExitCode} (0x{RuntimeBase.ExitCode:X8})");
        if (RuntimeBase.ExitMessage != null)
            Console.WriteLine($" and message: {RuntimeBase.ExitMessage}");
        else Console.WriteLine();

        if (pressToExit)
            PressToExit();
        return RuntimeBase.ExitCode;
    }

    private static void PressToExit()
    {
        Console.WriteLine("Press any key to exit...");
        Console.Read();
    }
}