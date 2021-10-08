using System;
using System.IO;
using System.Linq;
using KScr.Eval;
using KScr.Lib.Core;
using KScr.Lib.Model;
using KScr.Lib.Store;

namespace KScr.Runtime
{
    internal class Program
    {
        private static readonly KScrRuntime vm = new KScrRuntime();

        private static int Main(string[] args)
        {
            var eval = new KScrRuntime();

            if (args.Length == 0)
                return StdIoMode(eval);
            string fullSource = string.Join('\n',
                args.Where(it => it.EndsWith(".kscr"))
                    .Select(File.ReadAllText));
            if (fullSource != string.Empty)
            {
                var yield = HandleSourcecode(eval, fullSource, out IEvaluable? bytecode, out var time);
                var code = yield?.Value is Numeric num ? num.IntValue : 0;

                Console.WriteLine("Program finished with exit code " + code);
                PressToExit();
                return code;
            }

            Console.WriteLine("Uh-oh, I really don't know what this is: [" + string.Join(',', args) + ']');
            PressToExit();
            return -1;
        }

        private static void PressToExit()
        {
            Console.WriteLine("Press any key to exit...");
            Console.Read();
        }

        private static int RunCode(KScrRuntime runtime, Bytecode bytecode)
        {
            ObjectRef? result = null;
            try
            {
                result = runtime.Execute(bytecode);
            }
            catch (ThrownValue thr)
            {
                var v = thr.Value;
                Console.WriteLine("Program failed with exit value " + v?.ToString(IObject.ToString_LongName) ?? "null");

                if (v is Numeric code) return code.IntValue;
            }

            if (result?.Value is Numeric num)
            {
                Console.WriteLine("Program finished with exit code " + result);
                PressToExit();
                return num.IntValue;
            }

            Console.WriteLine("Program finished without exit value");
            PressToExit();
            return 0;
        }

        private static int StdIoMode(KScrRuntime runtime)
        {
            Bytecode full = new Bytecode();
            var verbose = false;

            while (true)
            {
                // write prefix
                Console.Write("kscr> ");
                // read command or code
                var input = Console.ReadLine();

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
                        full = new Bytecode();
                        continue;
                    case "run":
                        ClearEval(runtime);
                        return RunCode(runtime, full);
                    default:
                        if (!input.EndsWith(';'))
                            input += ';';
                        var result = HandleSourcecode(runtime, input, out IEvaluable here, out var time);
                        var isnull = result == null;

                        string str0 = result?.Value!.ToString(0) ?? "null";
                        if (verbose)
                            str0 += "\t\t" + (result?.Value!.ToString(-1) ?? "void");
                        str0 += $" [{time} µs]";
                        Console.WriteLine(str0);
                        full.Append(here);
                        continue;
                }
            }
        }

        private static void ClearEval(KScrRuntime runtime)
        {
            Console.Clear();
            vm.Clear();
        }

        private static ObjectRef? HandleSourcecode(KScrRuntime runtime, string? input, out IEvaluable? here, out long time)
        {
            var tokens = runtime.Tokenize(input);
            here = runtime.Compile(tokens);
            var result = runtime.Execute(here, out time);
            return result;
        }
    }
}