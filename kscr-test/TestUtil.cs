using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using KScr.Core;
using KScr.Core.Bytecode;
using KScr.Core.Model;
using KScr.Core.Std;
using KScr.Core.Store;
using KScr.Runtime;

namespace KScr.Test;

public class TestUtil
{
    public const int TestScale
#if DEBUG
        = 64;
#else
            = 512;
#endif
    public const int TestTimeout = 2000;

    public static (int exitCode, List<string> output) RunSourcecode(string testName, string code)
    {
        const string testPkg = "org.comroid.kscr.test";
        
        var vm = Program.VM;
        if (!code.Contains("main()"))
            code = "\npublic static void main() { " + code + " }";
        code = $"package {testPkg};\npublic class {testName} {{\n{code}\n}}";
 
        var testDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "src");
        var srcDir = Path.Combine(testDir, "src");
        var buildDir = Path.Combine(testDir, "build");
        var srcFile = Path.Combine(srcDir, testName + RuntimeBase.SourceFileExt);
        File.WriteAllText(srcFile, code);
        
        Console.WriteLine($"Running {testName} in test dir {testDir}");

        var execFile = Path.Combine(RuntimeBase.SdkHome.FullName, "kscr.exe");
        var execArgs = "execute" +
#if DEBUG
                       " --debug" +
#endif
                       $" --pkgbase {testPkg}" +
                       $" --source {srcFile}" +
                       $" --output {buildDir}";
        var execInfo = new ProcessStartInfo(execFile, execArgs) { WorkingDirectory = testDir };
        var exec = Process.Start(execInfo) ?? throw new Exception("Could not start testing process");
        exec.WaitForExit();
        List<string> output = new();
        while (!exec.StandardOutput.EndOfStream)
            output.Add(exec.StandardOutput.ReadLine()!);
        return (exec.ExitCode, output);
    }
}