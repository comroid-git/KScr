﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommandLine;
using CommandLine.Text;
using KScr.Compiler;
using KScr.Compiler.Code;
using KScr.Lib;
using KScr.Lib.Bytecode;
using KScr.Lib.Core;
using KScr.Lib.Exception;
using KScr.Lib.Model;
using KScr.Lib.Store;
using Array = System.Array;

namespace KScr.Runtime
{
    internal class Program
    {
        private static readonly KScrRuntime VM = new();

        private static readonly string
            DefaultOutput = Path.Combine(Directory.GetCurrentDirectory(), "build", "compile");
        private static readonly string
            StdPackageLocation = Path.Combine(RuntimeBase.GetSdkHome().FullName, "std");

        private static int Main(string[] args)
        {
            VM.Initialize();
            var state = State.Normal;
            var yield = VM.ConstantVoid.Value!;
            long compileTime = -1, executeTime = -1, ioTime = -1;

            Parser.Default.ParseArguments<CmdCompile, CmdExecute, CmdRun>(args)
                .WithParsed<CmdCompile>(cmd =>
                {
                    RuntimeBase.ConfirmExit = cmd.Confirm;
                    RuntimeBase.DebugMode = cmd.Debug;
                    // load std package
                    Package.ReadAll(VM, new DirectoryInfo(StdPackageLocation));
                    // load additional classpath packages
                    foreach (var classpath in cmd.Classpath.Where(dir => dir.Exists))
                        Package.ReadAll(VM, classpath);
                    compileTime = RuntimeBase.UnixTime();
                    VM.CompileFiles(cmd.Sources.SelectMany(path => Directory.Exists(path)
                        ? new DirectoryInfo(path).EnumerateFiles("*.kscr", SearchOption.AllDirectories) : new[]{ new FileInfo(path) }));
                    compileTime = RuntimeBase.UnixTime() - compileTime;
                    ioTime = RuntimeBase.UnixTime();
                    WriteClasses(cmd.Output ?? new DirectoryInfo(DefaultOutput), cmd.Sources.SelectMany(path => Directory.Exists(path)
                        ? new DirectoryInfo(path).EnumerateFiles("*.kscr", SearchOption.AllDirectories) : new[]{ new FileInfo(path) }));
                    ioTime = RuntimeBase.UnixTime() - ioTime;
                })
                .WithParsed<CmdExecute>(cmd =>
                {
                    RuntimeBase.ConfirmExit = cmd.Confirm;
                    RuntimeBase.DebugMode = cmd.Debug;
                    // load std package
                    Package.ReadAll(VM, new DirectoryInfo(StdPackageLocation));
                    // load additional classpath packages
                    foreach (var classpath in cmd.Classpath.Where(dir => dir.Exists))
                        Package.ReadAll(VM, classpath);
                    compileTime = RuntimeBase.UnixTime();
                    VM.CompileFiles(cmd.Sources.Select(path => new FileInfo(path)));
                    compileTime = RuntimeBase.UnixTime() - compileTime;
                    if (cmd.Output != null)
                    {
                        ioTime = RuntimeBase.UnixTime();
                        WriteClasses(cmd.Output ?? new DirectoryInfo(DefaultOutput), cmd.Sources.SelectMany(path =>
                            Directory.Exists(path)
                                ? new DirectoryInfo(path).EnumerateFiles("*.kscr", SearchOption.AllDirectories)
                                : new[] { new FileInfo(path) }));
                        ioTime = RuntimeBase.UnixTime() - ioTime;
                    }
                    yield = VM.Execute(out executeTime);
                })
                .WithParsed<CmdRun>(cmd =>
                {
                    RuntimeBase.ConfirmExit = cmd.Confirm;
                    RuntimeBase.DebugMode = cmd.Debug;
                    // load std package
                    Package.ReadAll(VM, new DirectoryInfo(StdPackageLocation));
                    // load classpath packages
                    ioTime = RuntimeBase.UnixTime();
                    foreach (var classpath in cmd.Classpath.Where(dir => dir.Exists))
                        Package.ReadAll(VM, classpath);
                    ioTime = RuntimeBase.UnixTime() - ioTime;
                    yield = VM.Execute(out executeTime);
                })
                .WithNotParsed(errors =>
                {
                    if (!errors.Any())
                        VM.Stack.StepInto(VM, RuntimeBase.MainInvocPos, RuntimeBase.MainInvoc, ref state, _ =>
                        {
                            StdIoMode(ref state, ref yield);
                            return state;
                        });
                });

            return HandleExit(state, yield, compileTime, executeTime, ioTime, RuntimeBase.ConfirmExit);
        }

        private static StatementComponent BuildProgramArgsParams(string[] args)
        {
            var comp = new StatementComponent
            {
                Type = StatementComponentType.Code,
                CodeType = BytecodeType.ParameterExpression,
                InnerCode = new ExecutableCode()
            };
            foreach (string str in args)
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

        private static void StdIoMode(ref State state, ref IObject yield)
        {
            Console.WriteLine("Entering StdIoMode - Only Expressions are allowed");
            VM.StdIoMode = true;

            var compiler = new StatementCompiler();
            var contextBase = new CompilerContext();
            var output = VM.ConstantVoid;

            while (state == State.Normal)
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
                state = context.ExecutableCode.Evaluate(VM, ref output);
                executeTime = DateTimeOffset.Now.ToUnixTimeMilliseconds() - executeTime;
                context.Clear();

                HandleResult(state, output.Value, compileTime, executeTime);
            }
        }

        private static void WriteClasses(DirectoryInfo output, IEnumerable<FileInfo> sources)
        {
            if (output.Exists)
                output.Delete(true);
            Package.RootPackage.Write(output, sources.Select(f => AbstractCompiler.FindClassInfo(f, new Tokenizer())).ToArray());
        }

        private static void HandleResult(State state, IObject? result, long compileTime, long executeTime)
        {
            Console.WriteLine($"State: {state.ToString()} - Compile: {compileTime}ms - Execute: {executeTime}ms");
            //Console.WriteLine($"Type: {result?.Type} - Value: {result?.ToString(0)}");
        }

        private static int HandleExit(State state, IObject? result, long compileTime = -1, long executeTime = -1, long ioTime = -1, bool pressToExit = false)
        {
            if (compileTime != -1)
                Console.Write($"Compile took {(double)compileTime/1000:#,##0.00}ms");
            if (compileTime != -1 && (ioTime != -1 || executeTime != -1))
                Console.Write("; ");
            if (ioTime != -1)
                Console.Write($"Read/Write took {(double)ioTime/1000:#,##0.00}ms");
            if (ioTime != -1 && executeTime != -1)
                Console.Write("; ");
            if (executeTime != -1)
                Console.Write($"Execute took {(double)executeTime/1000:#,##0.00}ms");
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

            if (result == null || result.Type.Name == "type")
            {
                Console.WriteLine("without exit value");
            }
            else if (result is Numeric num)
            {
                int rtn = num.IntValue;
                Console.WriteLine("with exit code " + rtn);
                return rtn;
            }
            else
            {
                Console.WriteLine("with exit message: " + result.ToString(0));
            }

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
}