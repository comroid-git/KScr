using System;
using System.IO;
using System.Linq;
using KScr.Lib.Core;
using KScr.Lib.Model;

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
                var yield = HandleSourcecode(eval, fullSource, out Bytecode bytecode, out var time);
                var code = yield is Numeric num ? num.IntValue : 0;

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
            IObject? result = null;
            try
            {
                result = runtime.Execute(bytecode, vm);
            }
            catch (ThrownValue thr)
            {
                var v = thr.Value;
                Console.WriteLine("Program failed with exit value " + v?.ToString(IObject.ToString_LongName) ?? "null");

                if (v is Numeric code) return code.IntValue;
            }

            if (result is Numeric num)
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
                        var result = HandleSourcecode(runtime, input, out Bytecode here, out var time);
                        var isnull = result == null;

                        string str0 = !isnull ? result!.ToString(0) : "null";
                        if (verbose)
                            str0 += "\t\t" + (!isnull ? result!.ToString(-1) : "void");
                        str0 += $" [{time} µs]";
                        Console.WriteLine(str0);
                        full += here;
                        continue;
                }
            }
        }

        private static void ClearEval(KScrRuntime runtime)
        {
            Console.Clear();
            vm.Clear();
        }

        private static IObject? HandleSourcecode(KScrRuntime runtime, string? input, out Bytecode? here, out long time)
        {
            var tokens = runtime.Tokenize(input);
            here = runtime.Compile(tokens);
            var result = runtime.Execute(here, vm, out time);
            return result;
        }
    }
}