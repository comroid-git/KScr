using System;
using System.IO;
using System.Linq;
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
        private static readonly KScrRuntime VM = new KScrRuntime();
        private static readonly string DefaultOutput = Path.Combine(Directory.GetCurrentDirectory(), "build", "compile");

        private static int Main(string[] args)
        {
            if (args.Length == 0)
                return StdIoMode(VM);
            
            State state = State.Normal;
            IObject yield = VM.ConstantVoid.Value!;
            string[] files = new string[args.Length - 1];
            Array.Copy(args, 1, files, 0, files.Length);

            switch (args[0])
            {
                case "compile":
                    Compile(VM, files);
                    WriteClasses(DefaultOutput);
                    break;
                case "execute":
                    Compile(VM, files);
                    yield = Run(VM, ref state);
                    break;
                case "run":
                    var classpath = args.Length >= 2 ? args[1] : Path.Combine(Directory.GetCurrentDirectory(), "bin");
                    Package.Read(VM, new DirectoryInfo(classpath));
                    yield = Run(VM, ref state);
                    break;
            }

            return HandleExit(state, yield);
        }

        private static CompilerContext Compile(RuntimeBase vm, string[] sourcepath)
        {
            var sourcecode = string.Join('\n', sourcepath
                .Where(path => path.EndsWith(".kscr"))
                .Where(File.Exists)
                .Select(File.ReadAllText));
            var tokens = vm.Tokenizer.Tokenize(vm, sourcecode);
            var ctx = new CompilerContext();

            return vm.Compiler.Compile(vm, ctx, tokens);
        }

        private static void WriteClasses(string output) => Package.RootPackage.Write(new DirectoryInfo(output));

        private static IObject Run(RuntimeBase vm, ref State state) => vm.Execute(ref state) ?? vm.ConstantVoid.Value!;

        [Obsolete]
        private static int StdIoMode(KScrRuntime runtime)
        {
            runtime.StdIoMode = true;
            ExecutableCode full = new ExecutableCode();
            IObject? result = null;
            var state = State.Normal;
            var verbose = false;

            while (state == State.Normal)
            {
                // write prefix
                Console.Write("kscr> ");
                // read command or code
                string? input = Console.ReadLine();

                if (input == string.Empty)
                    continue;

                switch (input)
                {
                    case "exit":
                        return 0;
                    case "verbose":
                        // ReSharper disable once AssignmentInConditionalExpression
                        Console.WriteLine("Verbose console output " + ((verbose = !verbose) ? "on" : "off"));
                        continue;
                    case "clear":
                        ClearEval(runtime);
                        full = new ExecutableCode();
                        continue;
                    case "run":
                        ClearEval(runtime);
                        result = runtime.Execute(ref state);
                        return HandleExit(state, result);
                    default:
                        if (!input.EndsWith(';'))
                            input += ';';
                        result = HandleSourcecode(runtime, input, ref state, out IEvaluable here, out long time);
                        bool isnull = result == null;

                        string str0 = result?.ToString(0) ?? "null";
                        if (verbose)
                            str0 += "\t\t" + (result?.ToString(-1) ?? "void");
                        str0 += $" [{time} µs]";
                        Console.WriteLine(str0);
                        full.Append(here);
                        continue;
                }
            }

            return HandleExit(state, result);
        }

        private static int HandleExit(State state, IObject? result)
        {
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
                Console.WriteLine("with exit message: " + result.ToString(IObject.ToString_LongName));
            }

            PressToExit();
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

        private static void ClearEval(KScrRuntime runtime)
        {
            Console.Clear();
            runtime.Clear();
        }

        [Obsolete]
        private static IObject? HandleSourcecode(KScrRuntime runtime, string? input, ref State state,
            out IEvaluable? here, out long time)
        {
            time = RuntimeBase.UnixTime();
            var tokens = runtime.Tokenizer.Tokenize(runtime, input!);
            // todo: fix "live" compilation
            // here = runtime.CodeCompiler.Compile(runtime, new CompilerContext());
            here = null!;
            var result = runtime.Execute(ref state);
            time = RuntimeBase.UnixTime() - time;
            return result;
        }
    }
}