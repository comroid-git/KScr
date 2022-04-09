using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;
using static KScr.Core.RuntimeBase;

namespace KScr.Runtime;

[SuppressMessage("Interoperability", "CA1416:Plattformkompatibilität überprüfen")]
public sealed class FileType
{
    public static readonly FileType SourceFile = new()
    {
        Extension = SourceFileExt,
        ExecVerb = "execute",
        ExecArgs = "--sources",
        PerceivedType = "Text"
    };
    public static readonly FileType BinaryFile = new()
    {
        Extension = BinaryFileExt,
        ExecVerb = "run",
        ExecArgs = "--classpath",
        PerceivedType = "System"
    };
    public static readonly FileType ModuleFile = new()
    {
        Extension = ModuleFileExt,
        ExecVerb = "run",
        ExecArgs = "--classpath",
        PerceivedType = "Application"
    };

    public FileType()
    {
        ExecName = Installer.EXEC_NAME;
        ExecPath = Path.Combine(new FileInfo(Assembly.Location).DirectoryName!, ExecName);
    }

    public string Extension { get; init; }
    public string ExecName { get; init; }
    public string ExecPath { get; init; }
    public string ExecVerb { get; init; }
    public string ExecArgs { get; init; }
    public string PerceivedType { get; init; }
    public string? IconPath { get; init; }

    public string Command => $"\"{ExecPath}\" {ExecVerb} {ExecArgs} %1";
    public string FileReg => Path.Combine("Software", "Classes", Extension);
    public string ProgId => Path.Combine("Applications", ExecName);
    public string AppReg => Path.Combine("Software", "Classes", ProgId);
    public string AppAssoc => Path.Combine("Software", "Microsoft", "Windows", "CurrentVersion", "Explorer", "FileExts", Extension);

    public override string ToString() => PerceivedType + ": " + Extension;
}

public static class Installer
{
    public const string ENV_PATH = "PATH";
    public const string EXEC_NAME = "kscr.exe";
    
    public static void CheckInstallation()
    {
        Debug.WriteLine($"[KScr Installer] Checking installation...");
        
        CheckPATH();
        
        if (CheckFileAssociation(FileType.SourceFile)
            | CheckFileAssociation(FileType.BinaryFile)
            | CheckFileAssociation(FileType.ModuleFile))
            SHChangeNotify(0x0800_0000, 0x0000, IntPtr.Zero, IntPtr.Zero);
        
        Debug.WriteLine($"[KScr Installer] Done");
    }

    private static void CheckPATH()
    {
        if (GetExecutable() != null)
            return;
        var scope = EnvironmentVariableTarget.Machine;
        Debug.WriteLine($"[KScr Installer] Executable not found in PATH. Adding it to scope {scope}");
        var dir = new FileInfo(Assembly.Location).Directory!;
        var oldValue = Environment.GetEnvironmentVariable(ENV_PATH, scope);
        var newValue  = oldValue + Path.PathSeparator + dir.FullName;
        Environment.SetEnvironmentVariable(ENV_PATH, newValue, scope);
    }

    private static bool CheckFileAssociation(FileType type)
    {
        if (IsAssociated(type))
            return false;
        Debug.WriteLine($"[KScr Installer] File type {type} is not associated. Attempting to associate...");
        Associate(type);
        return true;
    }

    [SuppressMessage("Interoperability", "CA1416:Plattformkompatibilität überprüfen")]
    private static bool IsAssociated(FileType type) => Registry.CurrentUser.OpenSubKey(
        Path.Combine("Software", "Classes", type.Extension), false) == null;

    [SuppressMessage("Interoperability", "CA1416:Plattformkompatibilität überprüfen")]
    private static void Associate(FileType type)
    {
        RegistryKey fileReg = Registry.CurrentUser.CreateSubKey(type.FileReg);
        RegistryKey appReg = Registry.CurrentUser.CreateSubKey(type.AppReg);
        RegistryKey appAssoc = Registry.CurrentUser.CreateSubKey(type.AppAssoc);

        fileReg.CreateSubKey("PerceivedType").SetValue("", type.PerceivedType);
        if (type.IconPath != null)
        {
            fileReg.CreateSubKey("DefaultIcon").SetValue("", type.IconPath);
            appReg.CreateSubKey("DefaultIcon").SetValue("", type.IconPath);
        }
        
        appReg.CreateSubKey(Path.Combine("shell", "open", "command")).SetValue("", type.Command);
        appReg.CreateSubKey(Path.Combine("shell", "edit", "command")).SetValue("", type.Command);

        appAssoc.CreateSubKey("UserChoice").SetValue("Progid", type.ProgId);
    }

    private static void Dump(string ext)
    {
        Console.WriteLine("1"+FileExtentionInfo(AssocStr.Command, ext), "Command");
        Console.WriteLine("2"+FileExtentionInfo(AssocStr.DDEApplication, ext), "DDEApplication");
        Console.WriteLine("3"+FileExtentionInfo(AssocStr.DDEIfExec, ext), "DDEIfExec");
        Console.WriteLine("4"+FileExtentionInfo(AssocStr.DDETopic, ext), "DDETopic");
        Console.WriteLine("5"+FileExtentionInfo(AssocStr.Executable, ext), "Executable");
        Console.WriteLine("6"+FileExtentionInfo(AssocStr.FriendlyAppName, ext), "FriendlyAppName");
        Console.WriteLine("7"+FileExtentionInfo(AssocStr.FriendlyDocName, ext), "FriendlyDocName");
        Console.WriteLine("8"+FileExtentionInfo(AssocStr.NoOpen, ext), "NoOpen");
        Console.WriteLine("9"+FileExtentionInfo(AssocStr.ShellNewValue, ext), "ShellNewValue");
    }


    [DllImport("shell32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);
    [DllImport("Shlwapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern uint AssocQueryString(AssocF flags, AssocStr str, string pszAssoc, string pszExtra, [Out] StringBuilder pszOut, [In][Out] ref uint pcchOut);

    public static string FileExtentionInfo(AssocStr assocStr, string doctype)
    {
        uint pcchOut = 0;
        AssocQueryString(AssocF.Verify, assocStr, doctype, null, null, ref pcchOut);

        StringBuilder pszOut = new StringBuilder((int)pcchOut);
        AssocQueryString(AssocF.Verify, assocStr, doctype, null, pszOut, ref pcchOut);
        return pszOut.ToString();
    }

    internal static FileInfo? GetExecutable()
    {
        return Environment.GetEnvironmentVariable(ENV_PATH)!.Split(Path.PathSeparator)
            .Select(x => new DirectoryInfo(x))
            .Where(x => x.Exists)
            .SelectMany(x => x.EnumerateFiles())
            .FirstOrDefault(f => f.Name == EXEC_NAME);
    }

    [Flags]
    public enum AssocF
    {
        Init_NoRemapCLSID = 0x1,
        Init_ByExeName = 0x2,
        Open_ByExeName = 0x2,
        Init_DefaultToStar = 0x4,
        Init_DefaultToFolder = 0x8,
        NoUserSettings = 0x10,
        NoTruncate = 0x20,
        Verify = 0x40,
        RemapRunDll = 0x80,
        NoFixUps = 0x100,
        IgnoreBaseClass = 0x200
    }

    public enum AssocStr
    {
        Command = 1,
        Executable,
        FriendlyDocName,
        FriendlyAppName,
        NoOpen,
        ShellNewValue,
        DDECommand,
        DDEIfExec,
        DDEApplication,
        DDETopic
    }
}