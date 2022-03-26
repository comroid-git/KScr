using System;
using System.IO;
using KScr.Lib;
using KScr.Lib.Core;
using KScr.Lib.Exception;
using NUnit.Framework;
using static KScr.Test.TestUtil;

namespace KScr.Test
{
    public class StatementTest
    {
        public static readonly Random rng = new();
        private TextWriter bakWrt;

        [SetUp]
        public void setup()
        {
            bakWrt = Console.Out;
        }

        [TearDown]
        public void teardown()
        {
            Console.SetOut(bakWrt);
        }

        [Test, Repeat(TestRepeat)]
        public void TestReturn()
        {
            int desiredCode = rng.Next() % RngMax;
            if (desiredCode < 10)
                desiredCode = RngMax;

            var code = RunSourcecode($"return {desiredCode};");
            Assert.AreEqual(desiredCode, (code.Value as Numeric).IntValue);
        }

        [Test, Repeat(TestRepeat)]
        public void TestThrow()
        {
            int desiredCode = rng.Next() % RngMax;
            if (desiredCode < 10)
                desiredCode = RngMax;

            var writer = new StringWriter();
            Console.SetOut(writer);
            try
            {
                RunSourcecode($"throw {desiredCode};");
            }
            catch (InternalException expected)
            {
            }

            var expectedOut = "";

            Console.WriteLine($"test: ExitCode == {desiredCode}");
            Assert.AreEqual(desiredCode, RuntimeBase.ExitCode);
            Assert.IsTrue(writer.ToString().StartsWith(expectedOut), $"Expected output was:\n{expectedOut}\nActual Output was \n{writer}");
        }

        [Test, Repeat(TestRepeat)]
        public void TestDeclaration()
        {
            int desired = rng.Next() % RngMax;
            if (desired < 10)
                desired = RngMax;
            
            var writer = new StringWriter();
            Console.SetOut(writer);
            var code = RunSourcecode($"public static int main() {{ int x = {desired}; return x; }}");
            
            Assert.AreEqual(desired, (code.Value as Numeric).IntValue);
        }

        [Test, Repeat(TestRepeat)]
        public void TestIf()
        {
            int desired = rng.Next() % RngMax;
            if (desired < 10)
                desired = RngMax;
            
            var code = RunSourcecode($"public static int main() {{ int x = {desired}; if (x > 8) return x; return x * 2; }}");
            
            Assert.AreEqual(desired > 8 ? desired : desired * 2, (code.Value as Numeric).IntValue);
        }

        [Test, Repeat(TestRepeat)]
        public void TestIfElse()
        {
            int desired = rng.Next() % RngMax;
            if (desired < 10)
                desired = RngMax;
            
            var code = RunSourcecode($"public static int main() {{ int x = {desired}; if (x % 2) {{ return x; }} else return x * 2; throw x; }}");
            
            Assert.AreEqual(desired % 2 > 0 ? desired : desired * 2, (code.Value as Numeric).IntValue);
        }

        [Test, Repeat(TestRepeat)]
        public void TestFor()
        {
            int desiredLen = rng.Next() % RngMax;
            if (desiredLen < 10)
                desiredLen = RngMax;

            var writer = new StringWriter();
            Console.SetOut(writer);
            var code = RunSourcecode($"for (int i = {desiredLen}; i; i -= 1) stdio << i;");
            string expected = "";
            for (int i = desiredLen; i > 0; i -= 1)
                expected += $"{i}\n";
            expected = expected.Substring(0, expected.Length - 1);

            Assert.IsTrue(writer.ToString().Replace("\r\n", "\n").StartsWith(expected), $"Expected output was:\n{expected}\nActual Output was \n{writer}");
        }

        [Test, Repeat(TestRepeat)]
        public void TestForEach()
        {
            int desiredLen = rng.Next() % RngMax;
            if (desiredLen < 10)
                desiredLen = RngMax;

            var writer = new StringWriter();
            Console.SetOut(writer);
            var code = RunSourcecode($"foreach (i : 0~{desiredLen}) stdio << i;");
            string expected = "";
            for (int i = 0; i < desiredLen; i++)
                expected += $"{i}\n";
            expected = expected.Substring(0, expected.Length - 1);

            Assert.IsTrue(writer.ToString().Replace("\r\n", "\n").StartsWith(expected), $"Expected output was:\n{expected}\nActual Output was \n{writer}");
        }

        [Test, Repeat(TestRepeat)]
        public void TestWhile()
        {
            int desiredLen = rng.Next() % RngMax;
            if (desiredLen < 10)
                desiredLen = RngMax;

            var writer = new StringWriter();
            Console.SetOut(writer);
            var code = RunSourcecode($"int i = {desiredLen}; while (i--) stdio << i;");
            string expected = "";
            int i = desiredLen;
            while (i-- > 0)
                expected += $"{i}\n";
            expected = expected.Substring(0, expected.Length - 1);

            Assert.IsTrue(writer.ToString().Replace("\r\n", "\n").StartsWith(expected), $"Expected output was:\n{expected}\nActual Output was \n{writer}");
        }
    }
}