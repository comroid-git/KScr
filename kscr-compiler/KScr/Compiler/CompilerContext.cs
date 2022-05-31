using System.Collections.Generic;
using System.Linq;
using KScr.Antlr;
using KScr.Core;
using KScr.Core.Bytecode;
using KScr.Core.Model;

namespace KScr.Compiler;

public class CompilerContext
{
    internal readonly IClassInfo? _class;
    internal readonly List<string>? _imports;
    internal readonly Package? _package;
    internal readonly Dictionary<string, Lambda> _lambdas = new();

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

    public ITypeInfo? FindType(RuntimeBase vm, string name)
    {
        if (name == Class?.Name)
            return Class;
        if (Imports.FirstOrDefault(n => n.EndsWith(name)) is { } imported)
            return vm.FindType(imported, Package);
        return null;
    }

    public void AddLambda(string? key, KScrParser.TupleExprContext? tupleExpr, ExecutableCode body)
    {
        throw new System.NotImplementedException();
    }
}