using System;
using System.Diagnostics;
using KScr.Core;
using KScr.Core.Bytecode;
using KScr.Core.Model;
using KScr.Core.Store;
using KScr.Runtime;

namespace KScr.Test
{
    public class TestUtil
    {
        public const int TestScale
#if DEBUG
            = 64;
#else
            = 512;
#endif
        public const int TestTimeout = 2000;

        public static IObjectRef RunSourcecode(string testName, string code)
        {
            var vm = Program.VM;
            if (!code.Contains("main()"))
                code = "public static void main() { " + code + " }";
            code = "package org.comroid.kscr.test; public class " + testName + " { " + code + " }";
            var cls = vm.CompileClass(testName, source: code);
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