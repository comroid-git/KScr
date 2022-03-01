using System;
using System.IO;
using System.Linq;
using KScr.Compiler;
using KScr.Compiler.Code;
using KScr.Lib;
using KScr.Lib.Bytecode;
using KScr.Lib.Core;
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

        private static int Main(string[] args)
        {
            VM.Initialize();
            
            var state = State.Normal;
            var yield = VM.ConstantVoid.Value!;
            long compileTime = -1, executeTime = -1;

            if (args.Length == 0)
                VM.Stack.StepInto(VM, RuntimeBase.MainInvocPos, Class.VoidType, "main", ref state, _ =>
                {
                    StdIoMode(ref state, ref yield);
                    return state;
                });
            else
            {
                var paths = new string[args.Length - 1];
                Array.Copy(args, 1, paths, 0, paths.Length);
                var files = paths.Select(path => new FileInfo(path)).GetEnumerator();
                //VM.Stack.MethodParamsExpr = BuildProgramArgsParams(args);

                switch (args[0])
                {
                    case "compile":
                        compileTime = RuntimeBase.UnixTime();
                        VM.CompileFiles(files);
                        compileTime = RuntimeBase.UnixTime() - compileTime;
                        WriteClasses(DefaultOutput);
                        break;
                    case "execute":
                        compileTime = RuntimeBase.UnixTime();
                        VM.CompileFiles(files);
                        compileTime = RuntimeBase.UnixTime() - compileTime;
                        
                        yield = VM.Execute(out executeTime);
                        break;
                    case "run":
                        string classpath = args.Length >= 2 ? args[1] : Directory.GetCurrentDirectory();
                        Package.Read(VM, new DirectoryInfo(classpath));
                        yield = VM.Execute(out executeTime);
                        break;
                    default:
                        Console.WriteLine("Invalid arguments: " + string.Join(' ', args));
                        break;
                }

                files.Dispose();
            }

            return HandleExit(state, yield, compileTime, executeTime);
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
                var tokens = new TokenContext(new Tokenizer().Tokenize(VM, Directory.GetCurrentDirectory(), code));
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

        private static void WriteClasses(string output)
        {
            Package.RootPackage.Write(new DirectoryInfo(output));
        }

        private static void HandleResult(State state, IObject? result, long compileTime, long executeTime)
        {
            Console.WriteLine($"State: {state.ToString()} - Compile: {compileTime}ms - Execute: {executeTime}ms");
            //Console.WriteLine($"Type: {result?.Type} - Value: {result?.ToString(0)}");
        }

        private static int HandleExit(State state, IObject? result, long compileTime = -1, long executeTime = -1)
        {
            if (compileTime != -1)
                Console.Write($"Compile took {(double)compileTime/1000:#,##0.00}ms");
            if (compileTime != -1 && executeTime != -1)
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

            if (result == null)
            {
                Console.WriteLine("without exit value;");
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

            //PressToExit();
            return state switch
            {
                State.Normal => 0,
                State.Return => 1,
                State.Throw => -1,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        private static void PressToExit()
        {
            Console.WriteLine("Press any key to exit...");
            Console.Read();
        }
    }
}