using KScr.Runtime;
using NUnit.Framework;

namespace KScr.Test
{
    public class ObjectTest
    {
        [SetUp]
        public void Setup()
        {
            Program.Main("compile",
                "--system",
                "--sources",
                "../../../../kscr-system/core/Throwable.kscr",
                "../../../../kscr-system/core/Iterator.kscr",
                "../../../../kscr-system/core/Iterable.kscr",
                "--output",
                "../../../../kscr-runtime/bin/Debug/net5.0/std/");
        }

        [Test]
        public void TestToString()
        {
            TestUtil.RunSourcecode("");
        }

        [Test]
        public void TestStackTrace()
        {
        }
    }
}