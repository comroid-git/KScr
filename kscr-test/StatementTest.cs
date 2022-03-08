using System;
using System.IO;
using KScr.Lib;
using KScr.Runtime;
using NUnit.Framework;

namespace KScr.Test
{
    public class StatementTest
    {
        public static readonly Random rng = new Random();
        
        [Test]
        public void TestReturn()
        {
            int desiredCode = rng.Next() % 16;
            
            TestUtil.RunSourcecode($"return {desiredCode};");
            Console.WriteLine($"test: ExitCode == {desiredCode}");
            Assert.AreEqual(desiredCode, RuntimeBase.ExitCode);
        }
        
        [Test]
        public void TestThrow()
        {
            int desiredCode = rng.Next() % 16;
            
            var writer = new StringWriter();
            Console.SetOut(writer);
            TestUtil.RunSourcecode($"throw {desiredCode};");
            string expected = "";
            
            Console.WriteLine($"test: ExitCode == {desiredCode}");
            Assert.AreEqual(desiredCode, RuntimeBase.ExitCode);
        }
        
        [Test]
        public void TestDeclaration()
        {
            TestUtil.RunSourcecode("");
        }
        
        [Test]
        public void TestIf()
        {
            TestUtil.RunSourcecode("");
        }
        
        [Test]
        public void TestIfElse()
        {
            TestUtil.RunSourcecode("");
        }
        
        [Test]
        public void TestFor()
        {
            TestUtil.RunSourcecode("");
        }
        
        [Test]
        public void TestForEach()
        {
            TestUtil.RunSourcecode("");
        }
        
        [Test]
        public void TestWhile()
        {
            TestUtil.RunSourcecode("");
        }
    }
}