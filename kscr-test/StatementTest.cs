using System;
using System.IO;
using KScr.Lib;
using KScr.Lib.Core;
using KScr.Lib.Exception;
using KScr.Runtime;
using NUnit.Framework;
using static KScr.Test.TestUtil;

namespace KScr.Test;

[Parallelizable(ParallelScope.Children)]
public class StatementTest
{
    public static readonly Random rng = new();
    private TextWriter bakWrt;

    [SetUp]
    public void setup()
    {
        //Program.VM.Clear();
        bakWrt = Console.Out;
    }

    [TearDown]
    public void teardown()
    {
        Console.SetOut(bakWrt);
    }

    [Test, Repeat(TestScale / 16)]
    public void TestReturn()
    {
        int desiredCode = rng.Next() % TestScale;
        if (desiredCode < 10)
            desiredCode = TestScale;

        var code = RunSourcecode("TestReturn", $"return {desiredCode};");
        Assert.AreEqual(desiredCode, (code.Value as Numeric).IntValue);
    }

    [Test, Repeat(TestScale / 16)]
    public void TestThrow()
    {
        int desiredCode = rng.Next() % TestScale;
        if (desiredCode < 10)
            desiredCode = TestScale;

        var writer = new StringWriter();
        Console.SetOut(writer);
        try
        {
            RunSourcecode("TestThrow", $"throw {desiredCode};");
        }
        catch (InternalException expected)
        {
            var expectedOut = "";

            Console.WriteLine($"test: ExitCode == {desiredCode}");
            Assert.AreEqual(desiredCode, RuntimeBase.ExitCode);
            Assert.IsTrue(writer.ToString().StartsWith(expectedOut), $"Expected output was:\n{expectedOut}\nActual Output was \n{writer}");
            Assert.Pass();
            return;
        }
        Assert.Fail("Did not throw");
    }

    [Test, Repeat(TestScale / 16)]
    public void TestDeclaration()
    {
        int desired = rng.Next() % TestScale;
        if (desired < 10)
            desired = TestScale;
            
        var writer = new StringWriter();
        Console.SetOut(writer);
        var code = RunSourcecode("TestDeclaration", $"public static int main() {{ int x = {desired}; return x; }}");
            
        Assert.AreEqual(desired, (code.Value as Numeric).IntValue);
    }

    [Test, Repeat(TestScale / 16)]
    public void TestIf()
    {
        int desired = rng.Next() % TestScale;
        if (desired < 10)
            desired = TestScale;
            
        var code = RunSourcecode("TestIf", $"public static int main() {{ int x = {desired}; if (x > 8) return x; return x * 2; }}");
            
        Assert.AreEqual(desired > 8 ? desired : desired * 2, (code.Value as Numeric).IntValue);
    }

    [Test, Repeat(TestScale / 16)]
    public void TestIfElse()
    {
        int desired = rng.Next() % TestScale;
        if (desired < 10)
            desired = TestScale;
            
        var code = RunSourcecode("TestIfElse", $"public static int main() {{ int x = {desired}; if (x % 2) {{ return x; }} else return x * 2; throw x; }}");
            
        Assert.AreEqual(desired % 2 > 0 ? desired : desired * 2, (code.Value as Numeric).IntValue);
    }

    [Test, Timeout(TestTimeout), Repeat(TestScale / 16)]
    public void TestFor()
    {
        int desiredLen = rng.Next() % TestScale;
        if (desiredLen < 10)
            desiredLen = TestScale;

        var writer = new StringWriter();
        Console.SetOut(writer);
        var code = RunSourcecode("TestFor", $"for (int i = {desiredLen}; i; i -= 1) stdio << i;");
        string expected = "";
        for (int i = desiredLen; i > 0; i -= 1)
            expected += $"{i}\n";
        expected = expected.Substring(0, expected.Length - 1);

        Assert.IsTrue(writer.ToString().Replace("\r\n", "\n").StartsWith(expected), $"Expected output was:\n{expected}\nActual Output was \n{writer}");
    }

    [Test, Timeout(TestTimeout), Repeat(TestScale / 16)]
    public void TestForEach()
    {
        int desiredLen = rng.Next() % TestScale;
        if (desiredLen < 10)
            desiredLen = TestScale;

        var writer = new StringWriter();
        Console.SetOut(writer);
        var code = RunSourcecode("TestForEach", $"foreach (i : 0~{desiredLen}) stdio << i;");
        string expected = "";
        for (int i = 0; i < desiredLen; i++)
            expected += $"{i}\n";
        expected = expected.Substring(0, expected.Length - 1);

        Assert.IsTrue(writer.ToString().Replace("\r\n", "\n").StartsWith(expected), $"Expected output was:\n{expected}\nActual Output was \n{writer}");
    }

    [Test, Timeout(TestTimeout), Repeat(TestScale / 16)]
    public void TestWhile()
    {
        int desiredLen = rng.Next() % TestScale;
        if (desiredLen < 10)
            desiredLen = TestScale;

        var writer = new StringWriter();
        Console.SetOut(writer);
        var code = RunSourcecode("TestWhile", $"int i = {desiredLen}; while (i--) stdio << i;");
        string expected = "";
        int i = desiredLen;
        while (i-- > 0)
            expected += $"{i}\n";
        expected = expected.Substring(0, expected.Length - 1);

        Assert.IsTrue(writer.ToString().Replace("\r\n", "\n").StartsWith(expected), $"Expected output was:\n{expected}\nActual Output was \n{writer}");
    }

    [Test, Timeout(TestTimeout), Repeat(TestScale / 16)]
    public void TestDoWhile()
    {
        int desiredLen = rng.Next() % TestScale;
        if (desiredLen < 10)
            desiredLen = TestScale;

        var writer = new StringWriter();
        Console.SetOut(writer);
        var code = RunSourcecode("TestDoWhile", $"int i = {desiredLen}; do {{ stdio << i; }} while (i--);");
        string expected = "";
        int i = desiredLen;
        do expected += $"{i}\n";
        while (i-- > 0);
        expected = expected.Substring(0, expected.Length - 1);

        Assert.IsTrue(writer.ToString().Replace("\r\n", "\n").StartsWith(expected), $"Expected output was:\n{expected}\nActual Output was \n{writer}");
    }
}