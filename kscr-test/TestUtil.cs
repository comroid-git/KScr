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
        public static IObjectRef RunSourcecode(string code)
        {
            var vm = Program.VM;
            if (!code.Contains("main()"))
                code = "public static void main() { " + code + " }";
            code = "package org.comroid.kscr.test; public class TestClass { " + code + " }";
            vm.CompileClass("TestClass", code);
            return vm.Execute()[StackOutput.Omg] ?? vm.ConstantVoid;
        }
    }
}