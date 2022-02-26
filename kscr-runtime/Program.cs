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
            var state = State.Normal;
            var yield = VM.ConstantVoid.Value!;
            long compileTime = -1, executeTime = -1;

            if (args.Length == 0)
            {
                StdIoMode(ref state, ref yield);
            }
            else
            {
                var paths = new string[args.Length - 1];
                Array.Copy(args, 1, paths, 0, paths.Length);
                var files = paths.Select(path => new FileInfo(path)).GetEnumerator();

                switch (args[0])
                {
                    case "compile":
                        compileTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                        VM.CompileFiles(files);
                        compileTime = DateTimeOffset.Now.ToUnixTimeMilliseconds() - compileTime;
                        WriteClasses(DefaultOutput);
                        break;
                    case "execute":
                        compileTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                        VM.CompileFiles(files);
                        compileTime = DateTimeOffset.Now.ToUnixTimeMilliseconds() - compileTime;
                        
                        executeTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                        yield = Run(VM, ref state);
                        executeTime = DateTimeOffset.Now.ToUnixTimeMilliseconds() - executeTime;
                        break;
                    case "run":
                        string classpath = args.Length >= 2 ? args[1] : Directory.GetCurrentDirectory();
                        Package.Read(VM, new DirectoryInfo(classpath));
                        executeTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                        yield = Run(VM, ref state);
                        executeTime = DateTimeOffset.Now.ToUnixTimeMilliseconds() - executeTime;
                        break;
                    default:
                        Console.WriteLine("Invalid arguments: " + string.Join(' ', args));
                        break;
                }

                files.Dispose();
            }

            return HandleExit(state, yield, compileTime, executeTime);
        }

        private static void StdIoMode(ref State state, ref IObject yield)
        {
            Console.WriteLine("Entering StdIoMode - Only Expressions are allowed");

            var compiler = new StatementCompiler();
            var contextBase = new CompilerContext();
            var output = new ObjectRef(Class.VoidType);
            VM.Stack.StepDown(Class.VoidType, "scratch");

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
                var tokens = new TokenContext(new Tokenizer().Tokenize(VM, code));
                var context = new CompilerContext(contextBase, tokens, CompilerType.CodeStatement);
                AbstractCompiler.CompilerLoop(VM, compiler, ref context);
                compileTime = DateTimeOffset.Now.ToUnixTimeMilliseconds() - compileTime;

                long executeTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                state = context.ExecutableCode.Evaluate(VM, null, ref output);
                executeTime = DateTimeOffset.Now.ToUnixTimeMilliseconds() - executeTime;
                context.Clear();

                HandleResult(state, output.Value, compileTime, executeTime);
            }
        }

        private static void WriteClasses(string output)
        {
            Package.RootPackage.Write(new DirectoryInfo(output));
        }

        private static IObject Run(RuntimeBase vm, ref State state)
        {
            return vm.Execute(ref state) ?? vm.ConstantVoid.Value!;
        }

        private static void HandleResult(State state, IObject? result, long compileTime, long executeTime)
        {
            Console.WriteLine($"State: {state.ToString()} - Compile: {compileTime}ms - Execute: {executeTime}ms");
            //Console.WriteLine($"Type: {result?.Type} - Value: {result?.ToString(0)}");
        }

        private static int HandleExit(State state, IObject? result, long compileTime = -1, long executeTime = -1)
        {
            if (compileTime != -1)
                Console.Write($"Compile took {compileTime}ms");
            if (compileTime != -1 && executeTime != -1)
                Console.Write("; ");
            if (executeTime != -1)
                Console.Write($"Execute took {executeTime}ms");
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