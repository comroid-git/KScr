using System;
using System.Globalization;
using System.IO;
using KScr.Lib;
using KScr.Lib.Core;
using KScr.Lib.Exception;
using KScr.Runtime;
using NUnit.Framework;
using static KScr.Test.TestUtil;

namespace KScr.Test;

public class OperatorTest
{
    public static readonly Random rng = new();
    private TextWriter bakWrt;

    [SetUp]
    public void setup()
    {
        Program.VM.Clear();
        bakWrt = Console.Out;
    }

    [TearDown]
    public void teardown()
    {
        Console.SetOut(bakWrt);
    }

    [Test, Repeat(TestRepeat)]
    public void TestStrPlus()
    {
        int a = rng.Next() % RngMax;
        int b = rng.Next() % RngMax;
        int buf;

        var writer = new StringWriter();
        Console.SetOut(writer);
        var code = RunSourcecode($"stdio << \"{a}\" + \"{b}\";");
        Assert.IsTrue(writer.ToString().Replace("\r\n", "\n").StartsWith($"{a}{b}"), $"{a}{b} != {writer.ToString()}");
    }

    [Test, Repeat(TestRepeat)]
    public void TestIntPlus()
    {
        int a = rng.Next() % RngMax;
        int b = rng.Next() % RngMax;
        int buf;

        var code = RunSourcecode($"return {a} + {b};");
        Assert.AreEqual(a + b, buf = (code.Value as Numeric).IntValue, $"{a} + {b} == {a + b} != {buf}");
    }

    [Test, Repeat(TestRepeat)]
    public void TestIntMinus()
    {
        int a = rng.Next() % RngMax;
        int b = rng.Next() % RngMax;
        int buf;

        var code = RunSourcecode($"return {a} - {b};");
        Assert.AreEqual(a - b, buf = (code.Value as Numeric).IntValue, $"{a} - {b} == {a - b} != {buf}");
    }

    [Test, Repeat(TestRepeat)]
    public void TestIntMultiply()
    {
        int a = rng.Next() % RngMax;
        int b = rng.Next() % RngMax;
        int buf;

        var code = RunSourcecode($"return {a} * {b};");
        Assert.AreEqual(a * b, buf = (code.Value as Numeric).IntValue, $"{a} * {b} == {a * b} != {buf}");
    }

    [Test, Repeat(TestRepeat)]
    public void TestIntDivide()
    {
        int a = rng.Next() % RngMax;
        int b = rng.Next() % RngMax + 1;
        int buf;

        var code = RunSourcecode($"return {a} / {b};");
        Assert.AreEqual(a / b, buf = (code.Value as Numeric).IntValue, $"{a} / {b} == {a / b} != {buf}");
    }

    [Test, Repeat(TestRepeat)]
    public void TestIntModulus()
    {
        int a = rng.Next() % RngMax;
        int b = rng.Next() % RngMax + 1;
        int buf;

        var code = RunSourcecode($"return {a} % {b};");
        Assert.AreEqual(a % b, buf = (code.Value as Numeric).IntValue, $"{a} % {b} == {a % b} != {buf}");
    }

    [Test, Repeat(TestRepeat)]
    public void TestLongPlus()
    {
        long a = rng.NextInt64() % RngMax;
        long b = rng.NextInt64() % RngMax;
        long buf;

        var code = RunSourcecode($"return {a.ToString(CultureInfo.InvariantCulture)}l + {b.ToString(CultureInfo.InvariantCulture)}l;");
        Assert.AreEqual(a + b, buf = (code.Value as Numeric).LongValue, $"{a} + {b} == {a + b} != {buf}");
    }

    [Test, Repeat(TestRepeat)]
    public void TestLongMinus()
    {
        long a = rng.NextInt64() % RngMax;
        long b = rng.NextInt64() % RngMax;
        long buf;

        var code = RunSourcecode($"return {a.ToString(CultureInfo.InvariantCulture)}l - {b.ToString(CultureInfo.InvariantCulture)}l;");
        Assert.AreEqual(a - b, buf = (code.Value as Numeric).LongValue, $"{a} - {b} == {a - b} != {buf}");
    }

    [Test, Repeat(TestRepeat)]
    public void TestLongMultiply()
    {
        long a = rng.NextInt64() % RngMax;
        long b = rng.NextInt64() % RngMax;
        long buf;

        var code = RunSourcecode($"return {a.ToString(CultureInfo.InvariantCulture)}l * {b.ToString(CultureInfo.InvariantCulture)}l;");
        Assert.AreEqual(a * b, buf = (code.Value as Numeric).LongValue, $"{a} * {b} == {a * b} != {buf}");
    }

    [Test, Repeat(TestRepeat)]
    public void TestLongDivide()
    {
        long a = rng.NextInt64() % RngMax;
        long b = rng.NextInt64() % RngMax + 1;
        long buf;

        var code = RunSourcecode($"return {a.ToString(CultureInfo.InvariantCulture)}l / {b.ToString(CultureInfo.InvariantCulture)}l;");
        Assert.AreEqual(a / b, buf = (code.Value as Numeric).LongValue, $"{a} / {b} == {a / b} != {buf}");
    }

    [Test, Repeat(TestRepeat)]
    public void TestLongModulus()
    {
        long a = rng.NextInt64() % RngMax;
        long b = rng.NextInt64() % RngMax + 1;
        long buf;

        var code = RunSourcecode($"return {a.ToString(CultureInfo.InvariantCulture)}l % {b.ToString(CultureInfo.InvariantCulture)}l;");
        Assert.AreEqual(a % b, buf = (code.Value as Numeric).LongValue, $"{a} % {b} == {a % b} != {buf}");
    }

    [Test, Repeat(TestRepeat)]
    public void TestFloatPlus()
    {
        float a = rng.NextInt64() % RngMax;
        float b = rng.NextInt64() % RngMax;
        float buf;

        var code = RunSourcecode($"return {a.ToString(CultureInfo.InvariantCulture)}f + {b.ToString(CultureInfo.InvariantCulture)}f;");
        Assert.AreEqual(a + b, buf = (code.Value as Numeric).FloatValue, $"{a} + {b} == {a + b} != {buf}");
    }

    [Test, Repeat(TestRepeat)]
    public void TestFloatMinus()
    {
        float a = rng.NextInt64() % RngMax;
        float b = rng.NextInt64() % RngMax;
        float buf;

        var code = RunSourcecode($"return {a.ToString(CultureInfo.InvariantCulture)}f - {b.ToString(CultureInfo.InvariantCulture)}f;");
        Assert.AreEqual(a - b, buf = (code.Value as Numeric).FloatValue, $"{a} - {b} == {a - b} != {buf}");
    }

    [Test, Repeat(TestRepeat)]
    public void TestFloatMultiply()
    {
        float a = rng.NextInt64() % RngMax;
        float b = rng.NextInt64() % RngMax;
        float buf;

        var code = RunSourcecode($"return {a.ToString(CultureInfo.InvariantCulture)}f * {b.ToString(CultureInfo.InvariantCulture)}f;");
        Assert.AreEqual(a * b, buf = (code.Value as Numeric).FloatValue, $"{a} * {b} == {a * b} != {buf}");
    }

    [Test, Repeat(TestRepeat)]
    public void TestFloatDivide()
    {
        float a = rng.NextInt64() % RngMax;
        float b = rng.NextInt64() % RngMax + 1;
        float buf;

        var code = RunSourcecode($"return {a.ToString(CultureInfo.InvariantCulture)}f / {b.ToString(CultureInfo.InvariantCulture)}f;");
        Assert.AreEqual(a / b, buf = (code.Value as Numeric).FloatValue, $"{a} / {b} == {a / b} != {buf}");
    }

    [Test, Repeat(TestRepeat)]
    public void TestFloatModulus()
    {
        float a = rng.NextInt64() % RngMax;
        float b = rng.NextInt64() % RngMax + 1;
        float buf;

        var code = RunSourcecode($"return {a.ToString(CultureInfo.InvariantCulture)}f % {b.ToString(CultureInfo.InvariantCulture)}f;");
        Assert.AreEqual(a % b, buf = (code.Value as Numeric).FloatValue, $"{a} % {b} == {a % b} != {buf}");
    }

    [Test, Repeat(TestRepeat)]
    public void TestDoublePlus()
    {
        double a = rng.NextDouble() % RngMax;
        double b = rng.NextDouble() % RngMax;
        double buf;

        var code = RunSourcecode($"return {a.ToString(CultureInfo.InvariantCulture)}d + {b.ToString(CultureInfo.InvariantCulture)}d;");
        Assert.AreEqual(a + b, buf = (code.Value as Numeric).DoubleValue, $"{a} + {b} == {a + b} != {buf}");
    }

    [Test, Repeat(TestRepeat)]
    public void TestDoubleMinus()
    {
        double a = rng.NextDouble() % RngMax;
        double b = rng.NextDouble() % RngMax;
        double buf;

        var code = RunSourcecode($"return {a.ToString(CultureInfo.InvariantCulture)}d - {b.ToString(CultureInfo.InvariantCulture)}d;");
        Assert.AreEqual(a - b, buf = (code.Value as Numeric).DoubleValue, $"{a} - {b} == {a - b} != {buf}");
    }

    [Test, Repeat(TestRepeat)]
    public void TestDoubleMultiply()
    {
        double a = rng.NextDouble() % RngMax;
        double b = rng.NextDouble() % RngMax;
        double buf;

        var code = RunSourcecode($"return {a.ToString(CultureInfo.InvariantCulture)}d * {b.ToString(CultureInfo.InvariantCulture)}d;");
        Assert.AreEqual(a * b, buf = (code.Value as Numeric).DoubleValue, $"{a} * {b} == {a * b} != {buf}");
    }

    [Test, Repeat(TestRepeat)]
    public void TestDoubleDivide()
    {
        double a = rng.NextDouble() % RngMax;
        double b = rng.NextDouble() % RngMax + 1;
        double buf;

        var code = RunSourcecode($"return {a.ToString(CultureInfo.InvariantCulture)}d / {b.ToString(CultureInfo.InvariantCulture)}d;");
        Assert.AreEqual(a / b, buf = (code.Value as Numeric).DoubleValue, $"{a} / {b} == {a / b} != {buf}");
    }

    [Test, Repeat(TestRepeat)]
    public void TestDoubleModulus()
    {
        double a = rng.NextDouble() % RngMax;
        double b = rng.NextDouble() % RngMax + 1;
        double buf;

        var code = RunSourcecode($"return {a.ToString(CultureInfo.InvariantCulture)}d % {b.ToString(CultureInfo.InvariantCulture)}d;");
        Assert.AreEqual(a % b, buf = (code.Value as Numeric).DoubleValue, $"{a} % {b} == {a % b} != {buf}");
    }
}