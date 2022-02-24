using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KScr.Compiler;
using KScr.Lib;
using KScr.Lib.Bytecode;
using KScr.Lib.Core;
using KScr.Lib.Model;
using Array = System.Array;

namespace KScr.Runtime
{
    internal class Program
    {
        private static readonly KScrRuntime VM = new();
        private static readonly string DefaultOutput = Path.Combine(Directory.GetCurrentDirectory(), "build", "compile");

        private static int Main(string[] args)
        {
            if (args.Length == 0)
                throw new ArgumentException("Missing execution parameters");
            
            State state = State.Normal;
            IObject yield = VM.ConstantVoid.Value!;
            string[] paths = new string[args.Length - 1];
            Array.Copy(args, 1, paths, 0, paths.Length);
            var files = paths.Select(path => new FileInfo(path)).GetEnumerator();

            switch (args[0])
            {
                case "compile":
                    VM.CompileFiles(files);
                    WriteClasses(DefaultOutput);
                    break;
                case "execute":
                    VM.CompileFiles(files);
                    yield = Run(VM, ref state);
                    break;
                case "run":
                    var classpath = args.Length >= 2 ? args[1] : Directory.GetCurrentDirectory();
                    Package.Read(VM, new DirectoryInfo(classpath));
                    yield = Run(VM, ref state);
                    break;
                default:
                    Console.WriteLine("Invalid arguments: " + string.Join(' ', args));
                    break;
            }
            files.Dispose();

            return HandleExit(state, yield);
        }

        private static void WriteClasses(string output) => Package.RootPackage.Write(new DirectoryInfo(output));

        private static IObject Run(RuntimeBase vm, ref State state) => vm.Execute(ref state) ?? vm.ConstantVoid.Value!;

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
    }
}