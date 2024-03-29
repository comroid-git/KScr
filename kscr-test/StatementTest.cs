using System;
using System.IO;
using KScr.Core.System;
using NUnit.Framework;
using static KScr.Test.TestUtil;

namespace KScr.Test;

[Parallelizable(ParallelScope.None)]
public class StatementTest
{
    public static readonly Random rng = new();

    [Test]
    [Repeat(TestScale)]
    public void TestReturn()
    {
        var desiredCode = rng.Next() % TestScale;
        if (desiredCode < 10)
            desiredCode = TestScale;

        var result = RunSourcecode("TestReturn", $"return {desiredCode};");
        Assert.AreEqual(desiredCode, (result.value as Numeric)!.IntValue);
    }

    [Test]
    [Repeat(TestScale)]
    public void TestThrow()
    {
        var desiredCode = rng.Next() % TestScale;
        if (desiredCode < 10)
            desiredCode = TestScale;

        var result = RunSourcecode("TestThrow", $"throw {desiredCode};");
        
        Console.WriteLine($"test: ExitCode == {desiredCode}");
        Assert.AreEqual(desiredCode, result.exitCode);
    }

    [Test]
    [Repeat(TestScale)]
    public void TestDeclaration()
    {
        var desired = rng.Next() % TestScale;
        if (desired < 10)
            desired = TestScale;

        var writer = new StringWriter();
        Console.SetOut(writer);
        var result = RunSourcecode("TestDeclaration", $"public static int main() {{ int x = {desired}; return x; }}");

        Assert.AreEqual(desired, (result.value as Numeric)!.IntValue);
    }

    [Test]
    [Repeat(TestScale)]
    public void TestIf()
    {
        var desired = rng.Next() % TestScale;
        if (desired < 10)
            desired = TestScale;

        var result = RunSourcecode("TestIf",
            $"public static int main() {{ int x = {desired}; if (x > 8) return x; return x * 2; }}");

        Assert.AreEqual(desired > 8 ? desired : desired * 2, (result.value as Numeric)!.IntValue);
    }

    [Test]
    [Repeat(TestScale)]
    public void TestIfElse()
    {
        var desired = rng.Next() % TestScale;
        if (desired < 10)
            desired = TestScale;

        var result = RunSourcecode("TestIfElse",
            $"public static int main() {{ int x = {desired}; if (x % 2) {{ return x; }} else return x * 2; throw x; }}");

        Assert.AreEqual(desired % 2 > 0 ? desired : desired * 2, (result.value as Numeric)!.IntValue);
    }

    //todo: fixme [Test] [Timeout(TestTimeout)] [Repeat(TestScale)] [NonParallelizable]
    public void TestFor()
    {
        var desiredLen = rng.Next() % TestScale;
        if (desiredLen < 10)
            desiredLen = TestScale;

        var result = RunSourcecode("TestFor", $"for (int i = {desiredLen}; i; i -= 1) stdio <<- i;");
        var expected = "";
        for (var i = desiredLen; i > 0; i -= 1)
            expected += i;
        expected = expected.Substring(0, expected.Length - 1);

        Assert.IsTrue(result.output.Replace("\r\n", "\n").StartsWith(expected),
            $"Expected output was:\n{expected}\nActual Output was \n{result.output}");
    }

    //todo: fixme [Test] [Timeout(TestTimeout)] [Repeat(TestScale)] [NonParallelizable]
    public void TestForEach()
    {
        var desiredLen = rng.Next() % TestScale;
        if (desiredLen < 10)
            desiredLen = TestScale;

        var result = RunSourcecode("TestForEach", $"foreach (i : 0..{desiredLen}) stdio <<- i;");
        var expected = "";
        for (var i = 0; i < desiredLen; i++)
            expected += i;
        expected = expected.Substring(0, expected.Length - 1);

        Assert.IsTrue(result.output.Replace("\r\n", "\n").StartsWith(expected),
            $"Expected output was:\n{expected}\nActual Output was \n{result.output}");
    }

    //todo: fixme [Test] [Timeout(TestTimeout)] [Repeat(TestScale)] [NonParallelizable]
    public void TestWhile()
    {
        var desiredLen = rng.Next() % TestScale;
        if (desiredLen < 10)
            desiredLen = TestScale;

        var result = RunSourcecode("TestWhile", $"int i = {desiredLen}; while (i--) stdio <<- i;");
        var expected = "";
        var i = desiredLen;
        while (i-- > 0)
            expected += i;
        expected = expected.Substring(0, expected.Length - 1);

        Assert.IsTrue(result.output.Replace("\r\n", "\n").StartsWith(expected),
            $"Expected output was:\n{expected}\nActual Output was \n{result.output}");
    }

    [Test]
    [Timeout(TestTimeout)]
    [Repeat(TestScale)]
    [NonParallelizable]
    public void TestDoWhile()
    {
        var desiredLen = rng.Next() % TestScale;
        if (desiredLen < 10)
            desiredLen = TestScale;

        var result = RunSourcecode("TestDoWhile", $"int i = {desiredLen}; do {{ stdio <<- i; }} while (i--);");
        var expected = "";
        var i = desiredLen;
        do
        {
            expected += i;
        } while (i-- > 0);

        expected = expected.Substring(0, expected.Length - 1);

        Assert.IsTrue(result.output.Replace("\r\n", "\n").StartsWith(expected),
            $"Expected output was:\n{expected}\nActual Output was \n{result.output}");
    }
}