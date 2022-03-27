using System;
using System.Diagnostics;
using KScr.Lib;
using KScr.Lib.Bytecode;
using KScr.Lib.Model;
using KScr.Lib.Store;
using KScr.Runtime;

namespace KScr.Test
{
    public class TestUtil
    {
        public const int RngMax
#if DEBUG
            = 64;
#else
            = 512;
#endif
        public const int TestRepeat
#if DEBUG
            = 2;
#else
            = 64;
#endif
        public const int TestTimeout = 2000;

        public static IObjectRef RunSourcecode(string testName, string code)
        {
            var vm = Program.VM;
            if (!code.Contains("main()"))
                code = "public static void main() { " + code + " }";
            code = "package org.comroid.kscr.test; public class " + testName + " { " + code + " }";
            var cls = vm.CompileClass(testName, code);
            var method = (cls.DeclaredMembers["main"] as Method)!;
            IObjectRef? yield = null;
            RuntimeBase.MainStack.StepInto(vm, new SourcefilePosition(), method, stack =>
            {
                yield = method.Evaluate(vm, stack.Output(StackOutput.Omg)).Copy(StackOutput.Omg);
            });
            vm.Clear();
            return yield ?? vm.ConstantVoid;
        }
    }
}