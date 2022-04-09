using System.Collections.Generic;
using KScr.Core.Bytecode;
using KScr.Core.Model;

namespace KScr.Compiler;

public class CompilerContext
{
    private readonly IClassInfo? _class;
    private readonly List<string>? _imports;
    private readonly Package? _package;

    internal CompilerContext? Parent { get; init; }

    public Package Package
    {
        get => _package ?? Parent?.Package ?? Package.RootPackage;
        init => _package = value;
    }

    public List<string> Imports
    {
        get => _imports ?? Parent?.Imports ?? new List<string>();
        init => _imports = value;
    }

    public IClassInfo? Class
    {
        get => _class ?? Parent?.Class;
        init => _class = value;
    }
}