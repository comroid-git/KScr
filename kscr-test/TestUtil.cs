using KScr.Runtime;

namespace KScr.Test
{
    public class TestUtil
    {
        public static void RunSourcecode(string code)
        {
            var vm = Program.VM;
            if (!code.Contains("main()"))
                code = "public static void main() { " + code + " }";
            code = "package org.comroid.kscr.test; public class TestClass { " + code + " }";
            vm.CompileClass("TestClass", code);
            vm.Execute();
        }
    }
}