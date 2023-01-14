using System;
using System.Globalization;
using System.IO;
using KScr.Core.Std;
using NUnit.Framework;
using static KScr.Test.TestUtil;

namespace KScr.Test;

[Parallelizable(ParallelScope.None)]
public class OperatorTest
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

    [Test]
    [Repeat(TestScale)]
    [NonParallelizable]
    public void TestStrPlus()
    {
        var a = rng.Next() % TestScale;
        var b = rng.Next() % TestScale;
        int buf;

        var writer = new StringWriter();
        Console.SetOut(writer);
        var code = RunSourcecode("TestStrPlus", $"stdio << \"{a}\" + \"{b}\";");
        Assert.IsTrue(writer.ToString().Replace("\r\n", "\n").StartsWith($"{a}{b}"), $"{a}{b} != {writer.ToString()}");
    }

    [Test]
    [Repeat(TestScale)]
    public void TestIntPlus()
    {
        var a = rng.Next() % TestScale;
        var b = rng.Next() % TestScale;
        int buf;

        var result = RunSourcecode("TestIntPlus", $"return {a} + {b};");
        Assert.AreEqual(a + b, buf = result.exitCode, $"{a} + {b} == {a + b} != {buf}");
    }

    [Test]
    [Repeat(TestScale)]
    public void TestIntMinus()
    {
        var a = rng.Next() % TestScale;
        var b = rng.Next() % TestScale;
        int buf;

        var result = RunSourcecode("TestIntMinus", $"return {a} - {b};");
        Assert.AreEqual(a - b, buf = result.exitCode, $"{a} - {b} == {a - b} != {buf}");
    }

    [Test]
    [Repeat(TestScale)]
    public void TestIntMultiply()
    {
        var a = rng.Next() % TestScale;
        var b = rng.Next() % TestScale;
        int buf;

        var result = RunSourcecode("TestIntMultiply", $"return {a} * {b};");
        Assert.AreEqual(a * b, buf = result.exitCode, $"{a} * {b} == {a * b} != {buf}");
    }

    [Test]
    [Repeat(TestScale)]
    public void TestIntDivide()
    {
        var a = rng.Next() % TestScale;
        var b = rng.Next() % TestScale + 1;
        int buf;

        var result = RunSourcecode("TestIntDivide", $"return {a} / {b};");
        Assert.AreEqual(a / b, buf = result.exitCode, $"{a} / {b} == {a / b} != {buf}");
    }

    [Test]
    [Repeat(TestScale)]
    public void TestIntModulus()
    {
        var a = rng.Next() % TestScale;
        var b = rng.Next() % TestScale + 1;
        int buf;

        var result = RunSourcecode("TestIntModulus", $"return {a} % {b};");
        Assert.AreEqual(a % b, buf = result.exitCode, $"{a} % {b} == {a % b} != {buf}");
    }

    [Test]
    [Repeat(TestScale)]
    public void TestLongPlus()
    {
        var a = rng.NextInt64() % TestScale;
        var b = rng.NextInt64() % TestScale;
        long buf;

        var result = RunSourcecode("TestLongPlus",
            $"return {a.ToString(CultureInfo.InvariantCulture)}l + {b.ToString(CultureInfo.InvariantCulture)}l;");
        Assert.AreEqual(a + b, buf = result.exitCode, $"{a} + {b} == {a + b} != {buf}");
    }

    [Test]
    [Repeat(TestScale)]
    public void TestLongMinus()
    {
        var a = rng.NextInt64() % TestScale;
        var b = rng.NextInt64() % TestScale;
        long buf;

        var result = RunSourcecode("TestLongMinus",
            $"return {a.ToString(CultureInfo.InvariantCulture)}l - {b.ToString(CultureInfo.InvariantCulture)}l;");
        Assert.AreEqual(a - b, buf = result.exitCode, $"{a} - {b} == {a - b} != {buf}");
    }

    [Test]
    [Repeat(TestScale)]
    public void TestLongMultiply()
    {
        var a = rng.NextInt64() % TestScale;
        var b = rng.NextInt64() % TestScale;
        long buf;

        var result = RunSourcecode("TestLongMultiply",
            $"return {a.ToString(CultureInfo.InvariantCulture)}l * {b.ToString(CultureInfo.InvariantCulture)}l;");
        Assert.AreEqual(a * b, buf = result.exitCode, $"{a} * {b} == {a * b} != {buf}");
    }

    [Test]
    [Repeat(TestScale)]
    public void TestLongDivide()
    {
        var a = rng.NextInt64() % TestScale;
        var b = rng.NextInt64() % TestScale + 1;
        long buf;

        var result = RunSourcecode("TestLongDivide",
            $"return {a.ToString(CultureInfo.InvariantCulture)}l / {b.ToString(CultureInfo.InvariantCulture)}l;");
        Assert.AreEqual(a / b, buf = result.exitCode, $"{a} / {b} == {a / b} != {buf}");
    }

    [Test]
    [Repeat(TestScale)]
    public void TestLongModulus()
    {
        var a = rng.NextInt64() % TestScale;
        var b = rng.NextInt64() % TestScale + 1;
        long buf;

        var result = RunSourcecode("TestLongModulus",
            $"return {a.ToString(CultureInfo.InvariantCulture)}l % {b.ToString(CultureInfo.InvariantCulture)}l;");
        Assert.AreEqual(a % b, buf = result.exitCode, $"{a} % {b} == {a % b} != {buf}");
    }

    [Test]
    [Repeat(TestScale)]
    public void TestFloatPlus()
    {
        float a = rng.NextInt64() % TestScale;
        float b = rng.NextInt64() % TestScale;
        float buf;

        var result = RunSourcecode("TestFloatPlus",
            $"return {a.ToString(CultureInfo.InvariantCulture)}f + {b.ToString(CultureInfo.InvariantCulture)}f;");
        Assert.AreEqual(a + b, buf = (result.value as Numeric)!.FloatValue, $"{a} + {b} == {a + b} != {buf}");
    }

    [Test]
    [Repeat(TestScale)]
    public void TestFloatMinus()
    {
        float a = rng.NextInt64() % TestScale;
        float b = rng.NextInt64() % TestScale;
        float buf;

        var result = RunSourcecode("TestFloatMinus",
            $"return {a.ToString(CultureInfo.InvariantCulture)}f - {b.ToString(CultureInfo.InvariantCulture)}f;");
        Assert.AreEqual(a - b, buf = (result.value as Numeric)!.FloatValue, $"{a} - {b} == {a - b} != {buf}");
    }

    [Test]
    [Repeat(TestScale)]
    public void TestFloatMultiply()
    {
        float a = rng.NextInt64() % TestScale;
        float b = rng.NextInt64() % TestScale;
        float buf;

        var result = RunSourcecode("TestFloatMultiply",
            $"return {a.ToString(CultureInfo.InvariantCulture)}f * {b.ToString(CultureInfo.InvariantCulture)}f;");
        Assert.AreEqual(a * b, buf = (result.value as Numeric)!.FloatValue, $"{a} * {b} == {a * b} != {buf}");
    }

    [Test]
    [Repeat(TestScale)]
    public void TestFloatDivide()
    {
        float a = rng.NextInt64() % TestScale;
        float b = rng.NextInt64() % TestScale + 1;
        float buf;

        var result = RunSourcecode("TestFloatDivide",
            $"return {a.ToString(CultureInfo.InvariantCulture)}f / {b.ToString(CultureInfo.InvariantCulture)}f;");
        Assert.AreEqual(a / b, buf = (result.value as Numeric)!.FloatValue, $"{a} / {b} == {a / b} != {buf}");
    }

    [Test]
    [Repeat(TestScale)]
    public void TestFloatModulus()
    {
        float a = rng.NextInt64() % TestScale;
        float b = rng.NextInt64() % TestScale + 1;
        float buf;

        var result = RunSourcecode("TestFloatModulus",
            $"return {a.ToString(CultureInfo.InvariantCulture)}f % {b.ToString(CultureInfo.InvariantCulture)}f;");
        Assert.AreEqual(a % b, buf = (result.value as Numeric)!.FloatValue, $"{a} % {b} == {a % b} != {buf}");
    }

    [Test]
    [Repeat(TestScale)]
    public void TestDoublePlus()
    {
        var a = rng.NextDouble() % TestScale;
        var b = rng.NextDouble() % TestScale;
        double buf;

        var result = RunSourcecode("TestDoublePlus",
            $"return {a.ToString(CultureInfo.InvariantCulture)}d + {b.ToString(CultureInfo.InvariantCulture)}d;");
        Assert.AreEqual(a + b, buf = (result.value as Numeric)!.DoubleValue, $"{a} + {b} == {a + b} != {buf}");
    }

    [Test]
    [Repeat(TestScale)]
    public void TestDoubleMinus()
    {
        var a = rng.NextDouble() % TestScale;
        var b = rng.NextDouble() % TestScale;
        double buf;

        var result = RunSourcecode("TestDoubleMinus",
            $"return {a.ToString(CultureInfo.InvariantCulture)}d - {b.ToString(CultureInfo.InvariantCulture)}d;");
        Assert.AreEqual(a - b, buf = (result.value as Numeric)!.DoubleValue, $"{a} - {b} == {a - b} != {buf}");
    }

    [Test]
    [Repeat(TestScale)]
    public void TestDoubleMultiply()
    {
        var a = rng.NextDouble() % TestScale;
        var b = rng.NextDouble() % TestScale;
        double buf;

        var result = RunSourcecode("TestDoubleMultiply",
            $"return {a.ToString(CultureInfo.InvariantCulture)}d * {b.ToString(CultureInfo.InvariantCulture)}d;");
        Assert.AreEqual(a * b, buf = (result.value as Numeric)!.DoubleValue, $"{a} * {b} == {a * b} != {buf}");
    }

    [Test]
    [Repeat(TestScale)]
    public void TestDoubleDivide()
    {
        var a = rng.NextDouble() % TestScale;
        var b = rng.NextDouble() % TestScale + 1;
        double buf;

        var result = RunSourcecode("TestDoubleDivide",
            $"return {a.ToString(CultureInfo.InvariantCulture)}d / {b.ToString(CultureInfo.InvariantCulture)}d;");
        Assert.AreEqual(a / b, buf = (result.value as Numeric)!.DoubleValue, $"{a} / {b} == {a / b} != {buf}");
    }

    [Test]
    [Repeat(TestScale)]
    public void TestDoubleModulus()
    {
        var a = rng.NextDouble() % TestScale;
        var b = rng.NextDouble() % TestScale + 1;
        double buf;

        var result = RunSourcecode("TestDoubleModulus",
            $"return {a.ToString(CultureInfo.InvariantCulture)}d % {b.ToString(CultureInfo.InvariantCulture)}d;");
        Assert.AreEqual(a % b, buf = (result.value as Numeric)!.DoubleValue, $"{a} % {b} == {a % b} != {buf}");
    }
}