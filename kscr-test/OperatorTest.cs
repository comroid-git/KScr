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

[Parallelizable(ParallelScope.Children)]
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

    [Test, Repeat(TestScale / 16)]
    public void TestStrPlus()
    {
        int a = rng.Next() % TestScale;
        int b = rng.Next() % TestScale;
        int buf;

        var writer = new StringWriter();
        Console.SetOut(writer);
        var code = RunSourcecode("TestStrPlus", $"stdio << \"{a}\" + \"{b}\";");
        Assert.IsTrue(writer.ToString().Replace("\r\n", "\n").StartsWith($"{a}{b}"), $"{a}{b} != {writer.ToString()}");
    }

    [Test, Repeat(TestScale / 16)]
    public void TestIntPlus()
    {
        int a = rng.Next() % TestScale;
        int b = rng.Next() % TestScale;
        int buf;

        var code = RunSourcecode("TestIntPlus", $"return {a} + {b};");
        Assert.AreEqual(a + b, buf = (code.Value as Numeric).IntValue, $"{a} + {b} == {a + b} != {buf}");
    }

    [Test, Repeat(TestScale / 16)]
    public void TestIntMinus()
    {
        int a = rng.Next() % TestScale;
        int b = rng.Next() % TestScale;
        int buf;

        var code = RunSourcecode("TestIntMinus", $"return {a} - {b};");
        Assert.AreEqual(a - b, buf = (code.Value as Numeric).IntValue, $"{a} - {b} == {a - b} != {buf}");
    }

    [Test, Repeat(TestScale / 16)]
    public void TestIntMultiply()
    {
        int a = rng.Next() % TestScale;
        int b = rng.Next() % TestScale;
        int buf;

        var code = RunSourcecode("TestIntMultiply", $"return {a} * {b};");
        Assert.AreEqual(a * b, buf = (code.Value as Numeric).IntValue, $"{a} * {b} == {a * b} != {buf}");
    }

    [Test, Repeat(TestScale / 16)]
    public void TestIntDivide()
    {
        int a = rng.Next() % TestScale;
        int b = rng.Next() % TestScale + 1;
        int buf;

        var code = RunSourcecode("TestIntDivide", $"return {a} / {b};");
        Assert.AreEqual(a / b, buf = (code.Value as Numeric).IntValue, $"{a} / {b} == {a / b} != {buf}");
    }

    [Test, Repeat(TestScale / 16)]
    public void TestIntModulus()
    {
        int a = rng.Next() % TestScale;
        int b = rng.Next() % TestScale + 1;
        int buf;

        var code = RunSourcecode("TestIntModulus", $"return {a} % {b};");
        Assert.AreEqual(a % b, buf = (code.Value as Numeric).IntValue, $"{a} % {b} == {a % b} != {buf}");
    }

    [Test, Repeat(TestScale / 16)]
    public void TestLongPlus()
    {
        long a = rng.NextInt64() % TestScale;
        long b = rng.NextInt64() % TestScale;
        long buf;

        var code = RunSourcecode("TestLongPlus", 
            $"return {a.ToString(CultureInfo.InvariantCulture)}l + {b.ToString(CultureInfo.InvariantCulture)}l;");
        Assert.AreEqual(a + b, buf = (code.Value as Numeric).LongValue, $"{a} + {b} == {a + b} != {buf}");
    }

    [Test, Repeat(TestScale / 16)]
    public void TestLongMinus()
    {
        long a = rng.NextInt64() % TestScale;
        long b = rng.NextInt64() % TestScale;
        long buf;

        var code = RunSourcecode("TestLongMinus", 
            $"return {a.ToString(CultureInfo.InvariantCulture)}l - {b.ToString(CultureInfo.InvariantCulture)}l;");
        Assert.AreEqual(a - b, buf = (code.Value as Numeric).LongValue, $"{a} - {b} == {a - b} != {buf}");
    }

    [Test, Repeat(TestScale / 16)]
    public void TestLongMultiply()
    {
        long a = rng.NextInt64() % TestScale;
        long b = rng.NextInt64() % TestScale;
        long buf;

        var code = RunSourcecode("TestLongMultiply", 
            $"return {a.ToString(CultureInfo.InvariantCulture)}l * {b.ToString(CultureInfo.InvariantCulture)}l;");
        Assert.AreEqual(a * b, buf = (code.Value as Numeric).LongValue, $"{a} * {b} == {a * b} != {buf}");
    }

    [Test, Repeat(TestScale / 16)]
    public void TestLongDivide()
    {
        long a = rng.NextInt64() % TestScale;
        long b = rng.NextInt64() % TestScale + 1;
        long buf;

        var code = RunSourcecode("TestLongDivide", 
            $"return {a.ToString(CultureInfo.InvariantCulture)}l / {b.ToString(CultureInfo.InvariantCulture)}l;");
        Assert.AreEqual(a / b, buf = (code.Value as Numeric).LongValue, $"{a} / {b} == {a / b} != {buf}");
    }

    [Test, Repeat(TestScale / 16)]
    public void TestLongModulus()
    {
        long a = rng.NextInt64() % TestScale;
        long b = rng.NextInt64() % TestScale + 1;
        long buf;

        var code = RunSourcecode("TestLongModulus", 
            $"return {a.ToString(CultureInfo.InvariantCulture)}l % {b.ToString(CultureInfo.InvariantCulture)}l;");
        Assert.AreEqual(a % b, buf = (code.Value as Numeric).LongValue, $"{a} % {b} == {a % b} != {buf}");
    }

    [Test, Repeat(TestScale / 16)]
    public void TestFloatPlus()
    {
        float a = rng.NextInt64() % TestScale;
        float b = rng.NextInt64() % TestScale;
        float buf;

        var code = RunSourcecode("TestFloatPlus", 
            $"return {a.ToString(CultureInfo.InvariantCulture)}f + {b.ToString(CultureInfo.InvariantCulture)}f;");
        Assert.AreEqual(a + b, buf = (code.Value as Numeric).FloatValue, $"{a} + {b} == {a + b} != {buf}");
    }

    [Test, Repeat(TestScale / 16)]
    public void TestFloatMinus()
    {
        float a = rng.NextInt64() % TestScale;
        float b = rng.NextInt64() % TestScale;
        float buf;

        var code = RunSourcecode("TestFloatMinus", 
            $"return {a.ToString(CultureInfo.InvariantCulture)}f - {b.ToString(CultureInfo.InvariantCulture)}f;");
        Assert.AreEqual(a - b, buf = (code.Value as Numeric).FloatValue, $"{a} - {b} == {a - b} != {buf}");
    }

    [Test, Repeat(TestScale / 16)]
    public void TestFloatMultiply()
    {
        float a = rng.NextInt64() % TestScale;
        float b = rng.NextInt64() % TestScale;
        float buf;

        var code = RunSourcecode("TestFloatMultiply", 
            $"return {a.ToString(CultureInfo.InvariantCulture)}f * {b.ToString(CultureInfo.InvariantCulture)}f;");
        Assert.AreEqual(a * b, buf = (code.Value as Numeric).FloatValue, $"{a} * {b} == {a * b} != {buf}");
    }

    [Test, Repeat(TestScale / 16)]
    public void TestFloatDivide()
    {
        float a = rng.NextInt64() % TestScale;
        float b = rng.NextInt64() % TestScale + 1;
        float buf;

        var code = RunSourcecode("TestFloatDivide", 
            $"return {a.ToString(CultureInfo.InvariantCulture)}f / {b.ToString(CultureInfo.InvariantCulture)}f;");
        Assert.AreEqual(a / b, buf = (code.Value as Numeric).FloatValue, $"{a} / {b} == {a / b} != {buf}");
    }

    [Test, Repeat(TestScale / 16)]
    public void TestFloatModulus()
    {
        float a = rng.NextInt64() % TestScale;
        float b = rng.NextInt64() % TestScale + 1;
        float buf;

        var code = RunSourcecode("TestFloatModulus", 
            $"return {a.ToString(CultureInfo.InvariantCulture)}f % {b.ToString(CultureInfo.InvariantCulture)}f;");
        Assert.AreEqual(a % b, buf = (code.Value as Numeric).FloatValue, $"{a} % {b} == {a % b} != {buf}");
    }

    [Test, Repeat(TestScale / 16)]
    public void TestDoublePlus()
    {
        double a = rng.NextDouble() % TestScale;
        double b = rng.NextDouble() % TestScale;
        double buf;

        var code = RunSourcecode("TestDoublePlus", 
            $"return {a.ToString(CultureInfo.InvariantCulture)}d + {b.ToString(CultureInfo.InvariantCulture)}d;");
        Assert.AreEqual(a + b, buf = (code.Value as Numeric).DoubleValue, $"{a} + {b} == {a + b} != {buf}");
    }

    [Test, Repeat(TestScale / 16)]
    public void TestDoubleMinus()
    {
        double a = rng.NextDouble() % TestScale;
        double b = rng.NextDouble() % TestScale;
        double buf;

        var code = RunSourcecode("TestDoubleMinus", 
            $"return {a.ToString(CultureInfo.InvariantCulture)}d - {b.ToString(CultureInfo.InvariantCulture)}d;");
        Assert.AreEqual(a - b, buf = (code.Value as Numeric).DoubleValue, $"{a} - {b} == {a - b} != {buf}");
    }

    [Test, Repeat(TestScale / 16)]
    public void TestDoubleMultiply()
    {
        double a = rng.NextDouble() % TestScale;
        double b = rng.NextDouble() % TestScale;
        double buf;

        var code = RunSourcecode("TestDoubleMultiply", 
            $"return {a.ToString(CultureInfo.InvariantCulture)}d * {b.ToString(CultureInfo.InvariantCulture)}d;");
        Assert.AreEqual(a * b, buf = (code.Value as Numeric).DoubleValue, $"{a} * {b} == {a * b} != {buf}");
    }

    [Test, Repeat(TestScale / 16)]
    public void TestDoubleDivide()
    {
        double a = rng.NextDouble() % TestScale;
        double b = rng.NextDouble() % TestScale + 1;
        double buf;

        var code = RunSourcecode("TestDoubleDivide", 
            $"return {a.ToString(CultureInfo.InvariantCulture)}d / {b.ToString(CultureInfo.InvariantCulture)}d;");
        Assert.AreEqual(a / b, buf = (code.Value as Numeric).DoubleValue, $"{a} / {b} == {a / b} != {buf}");
    }

    [Test, Repeat(TestScale / 16)]
    public void TestDoubleModulus()
    {
        double a = rng.NextDouble() % TestScale;
        double b = rng.NextDouble() % TestScale + 1;
        double buf;

        var code = RunSourcecode("TestDoubleModulus", 
            $"return {a.ToString(CultureInfo.InvariantCulture)}d % {b.ToString(CultureInfo.InvariantCulture)}d;");
        Assert.AreEqual(a % b, buf = (code.Value as Numeric).DoubleValue, $"{a} % {b} == {a % b} != {buf}");
    }
}