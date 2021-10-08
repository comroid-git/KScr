using System;
using System.IO;
using System.Linq;
using KScr.Eval;
using KScr.Lib;
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
                var yield = HandleSourcecode(eval, fullSource, out State state, out IEvaluable? bytecode, out var time);
                return HandleExit(state, yield);
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

        private static int StdIoMode(KScrRuntime runtime)
        {
            runtime.StdIoMode = true;
            Bytecode full = new Bytecode();
            IObject? result = null;
            State state = State.Normal;
            var verbose = false;

            while (state == State.Normal)
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
                        result = runtime.Execute(full, out state);
                        return HandleExit(state, result);
                    default:
                        if (!input.EndsWith(';'))
                            input += ';';
                        result = HandleSourcecode(runtime, input, out state, out IEvaluable here, out var time);
                        var isnull = result == null;

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
                Console.WriteLine("without exit value;");
            else if (result is Numeric num)
            {
                int rtn = num.IntValue;
                Console.WriteLine("with exit code " + rtn);
                return rtn;
            }
            else Console.WriteLine("with exit message: " + result.ToString(IObject.ToString_LongName));

            PressToExit();
            return state switch
            {
                State.Normal => 0,
                State.Return => 1,
                State.Throw => -1,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        private static void ClearEval(KScrRuntime runtime)
        {
            Console.Clear();
            vm.Clear();
        }

        private static IObject? HandleSourcecode(KScrRuntime runtime, string? input, out State state, out IEvaluable? here, out long time)
        {
            time = RuntimeBase.UnixTime();
            var tokens = runtime.Tokenize(input!);
            here = runtime.Compile(tokens);
            var result = runtime.Execute(here, out state);
            time = RuntimeBase.UnixTime() - time;
            return result;
        }
    }
}