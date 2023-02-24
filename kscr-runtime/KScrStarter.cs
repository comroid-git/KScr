using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CommandLine;
using comroid.csapi.common;
using KScr.Core;
using KScr.Core.Bytecode;
using KScr.Core.Exception;
using KScr.Core.Model;
using KScr.Core.System;
using KScr.Core.Store;
using KScr.Core.Util;

namespace KScr.Runtime;

public class KScrStarter
{
    public static readonly KScrRuntime VM;

    private static readonly string
        DefaultOutput = Path.Combine(Directory.GetCurrentDirectory(), "build", "compile");

    private static readonly string
        SystemLibLocation = Path.Combine(RuntimeBase.SdkHome.FullName, "system.kmod");

    static KScrStarter()
    {
#if RELEASE
        Log.Root.Level = LogLevel.None;
#endif

        VM = new KScrRuntime();
        VM.Initialize();
    }

    public static void HandleCompile(CmdCompile cmd)
    {
        CopyProps(cmd);
        if (!cmd.System)
        {
            LoadSystemPackage();
            LoadClasspath(cmd);
        }

        compileTime = CompileSource(cmd, cmd.PkgBase);
        if (VM.CompilerErrors.Count > 0) return;
        ioTime = WriteClasses(cmd);
    }

    public static void HandleExecute(CmdExecute cmd)
    {
        CopyProps(cmd);
        LoadSystemPackage();
        LoadClasspath(cmd);

        compileTime = CompileSource(cmd, cmd.PkgBase);
        if (VM.CompilerErrors.Count > 0) return;
        if (cmd.Output != null) ioTime = WriteClasses(cmd);
        executeTime = Execute(out _);
    }

    public static void HandleParsed(CmdRun cmd)
    {
        var stack = RuntimeBase.MainStack;
        CopyProps(cmd);
        LoadSystemPackage();

        ioTime = LoadClasspath(cmd);
        VM.LateInitializeNonPrimitives(stack);
        executeTime = Execute(out stack);
    }

    public static void HandleConfig(CmdConfig cmd)
    {
        if (cmd.Install) Installer.CheckInstallation();
    }


    private static long compileTime = -1, executeTime = -1, ioTime = -1;
    public static int Main(params string[] args)
    {
        Parser.Default.ParseArguments<CmdCompile, CmdExecute, CmdRun, CmdConfig>(args)
            .WithParsed<CmdCompile>(HandleCompile)
            .WithParsed<CmdExecute>(HandleExecute)
            .WithParsed<CmdRun>(HandleParsed)
            .WithParsed<CmdConfig>(HandleConfig);

        HandleErrors();

        return HandleExit(RuntimeBase.MainStack.State, RuntimeBase.MainStack.Omg?.Value, compileTime, executeTime, ioTime, RuntimeBase.ConfirmExit);
    }

    public static void CopyProps(IGenericCmd cmd)
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

    public static void LoadSystemPackage()
    {
        // load system package
        //VM.Load(SystemPackageLocation);
        Package.ReadAll(VM, new FileInfo(SystemLibLocation));
    }

    public static long LoadClasspath(IClasspathCmd cmd)
    {
        // load classpath packages
        if (cmd.Classpath.Count(dir => dir.Exists) == 0)
            return -1;
        return DebugUtil.Measure(() =>
        {
            foreach (var classpath in cmd.Classpath.Where(dir => dir.Exists))
                //VM.Load(classpath.FullName);
                Package.ReadAll(VM, classpath);
        });
    }

    public static long CompileSource(ISourcesCmd cmd, string? basePackage = null)
    {
        return DebugUtil.Measure(() =>
        {
            try
            {
                VM.CompileSource(cmd.Source, basePackage);
            }
            catch (Exception e)
            {
                VM.CompilerErrors.Add(e as CompilerException ?? new CompilerException(RuntimeBase.SystemSrcPos,
                    CompilerErrorMessage.UnderlyingDetail, e.GetType().Name + ": " + e.Message));
            }
        });
    }

    public static long WriteClasses<TC>(TC cmd) where TC : ISourcesCmd, IOutputCmd
    {
        return DebugUtil.Measure(() =>
            WriteClasses(cmd.Output ?? new DirectoryInfo(DefaultOutput), new[] { cmd.Source }.SelectMany(path =>
                Directory.Exists(path)
                    ? new DirectoryInfo(path).EnumerateFiles("*.kscr", SearchOption.AllDirectories)
                    : new[] { new FileInfo(path) })));
    }

    private static void WriteClasses(DirectoryInfo output, IEnumerable<FileInfo> sources)
    {
        if (output.Exists)
            output.Delete(true);
        Package.RootPackage.Write(VM, output, sources.Select(f => VM.FindClassInfo(f)).ToArray(), new StringCache());
    }

    public static long Execute(out Stack stack, string? mainClassName = null)
    {
        var executeTime = DebugUtil.UnixTime();
        stack = VM.Execute(mainClassName);
        executeTime = DebugUtil.UnixTime() - executeTime;
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

    private static void SystemIoMode()
    {
        throw new NotImplementedException("SystemIoMode currently not supported");

        /*
        Log<KScrStarter>.At(LogLevel.Info, "Entering SystemIoMode - Only Expressions are allowed");
        VM.SystemIoMode = true;

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

            string code = "systemio << " + expr + ';';

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
        Log<KScrStarter>.At(LogLevel.Info, $"State: {state.ToString()} - Compile: {compileTime}ms - Execute: {executeTime}ms");
        //Log<KScrStarter>.At(LogLevel.Info, $"Type: {result?.Type} - Value: {result?.ToString(0)}");
    }

    private static void HandleErrors()
    {
        var compilerErrorsCount = VM.CompilerErrors.Count;
        if (compilerErrorsCount > 0)
        {
            VM.PrintCompilerErrors<KScrStarter>();
            var plural = compilerErrorsCount != 1;
            Log<KScrStarter>.At(LogLevel.Warning,
                $"There {(plural ? "were" : "was")} {compilerErrorsCount} compilation error{(plural ? 's' : string.Empty)}.");
        }
    }

    public static string IOTimeString(long compileTime = -1, long executeTime = -1, long ioTime = -1)
    {
        var str = string.Empty;
        if (compileTime != -1)
            str += $"Compile took {(double)compileTime / 1000:#,##0.00}ms";
        if (compileTime != -1 && (ioTime != -1 || executeTime != -1))
            str += "; ";
        if (ioTime != -1)
            str += $"Read/Write took {(double)ioTime / 1000:#,##0.00}ms";
        if (ioTime != -1 && executeTime != -1)
            str += "; ";
        if (executeTime != -1)
            str += $"Execute took {(double)executeTime / 1000:#,##0.00}ms";
        if (compileTime != -1 || executeTime != -1)
            str += '\n';
        return str;
    }

    private static int HandleExit(State state, IObject? result, long compileTime = -1, long executeTime = -1,
        long ioTime = -1, bool pressToExit = false){
        
        Log<KScrStarter>.At(LogLevel.Info, IOTimeString(compileTime, executeTime, ioTime));
        var str = "";

        switch (state)
        {
            case State.Normal:
                str += "Program stopped ";
                break;
            case State.Return:
                str += "Program finished ";
                break;
            case State.Throw:
                str += "Program failed ";
                break;
        }

        if (result is Numeric num)
        {
            var rtn = num.IntValue;
            Log<KScrStarter>.At(LogLevel.Info, $"with exit code {rtn} (0x{rtn:X8})");
            return rtn;
        }

        str += $"with exit code {RuntimeBase.ExitCode} (0x{RuntimeBase.ExitCode:X8})";
        Log<KScrStarter>.At(LogLevel.Info,
            RuntimeBase.ExitMessage != null ? str + $" and message: {RuntimeBase.ExitMessage}" : str);

        if (pressToExit)
            PressToExit();
        return RuntimeBase.ExitCode;
    }

    private static void PressToExit()
    {
        Log<KScrStarter>.At(LogLevel.Info, "Press any key to exit...");
        Console.Read();
    }
}