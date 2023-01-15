using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using comroid.csapi.common;
using KScr.Core;
using KScr.Core.System;
using KScr.Core.Store;
using KScr.Runtime;

namespace KScr.Test;

public class TestUtil
{
    public const int TestScale = byte.MaxValue;
    public const int TestTimeout = 2000;
    public static readonly Guid TestID = Guid.NewGuid();
    public static Dictionary<string, int> TestNo = new();

    public static (int exitCode, string output, IObject value) RunSourcecode(string testName, string code)
    {
        const string testPkg = "org.comroid.kscr.test";

        if (!TestNo.ContainsKey(testName))
            TestNo[testName] = 0;
        testName += ++TestNo[testName];

        var vm = KScrStarter.VM;
        if (!code.Contains("main()"))
            code = "\npublic static void main() {\n" + code + "\n" +
                   "}";
        code = $"package {testPkg};\npublic class {testName} {{\n{code}\n}}";

        var tesystemir = Path.Combine(Path.GetTempPath(), "kscr-test-" + TestID);
        Directory.CreateDirectory(tesystemir);
        var srcDir = Path.Combine(tesystemir, "src");
        Directory.CreateDirectory(srcDir);
        var buildDir = Path.Combine(tesystemir, "build");
        Directory.CreateDirectory(buildDir);
        var srcFile = Path.Combine(srcDir, testName + RuntimeBase.SourceFileExt);
        var fs = File.Create(srcFile);
        var sw = new StreamWriter(fs);
        sw.Write(code);
        sw.Close();

        Log<TestUtil>.At(LogLevel.Info, $"Running {testName} in test dir {tesystemir}");

        var cmd = new CmdExecute
        {
#if DEBUG
            Debug = true,
#endif
            PkgBase = testPkg,
            Source = srcFile
        };
        var compileTime = KScrStarter.CompileSource(cmd, testPkg);
        RuntimeBase.ExtraArgs = Array.Empty<string>();
        var outw = new StringWriter();
        var outb = Console.Out;
        Console.SetOut(outw);
        var executeTime = KScrStarter.Execute(out var stack, $"{testPkg}.{testName}");
        Console.SetOut(outb);

        Log<TestUtil>.At(LogLevel.Info, $"{testName} complete; {KScrStarter.IOTimeString(compileTime, executeTime)}");

        return (RuntimeBase.ExitCode, outw.ToString(), stack[StackOutput.Omg]?.Value);
    }
}