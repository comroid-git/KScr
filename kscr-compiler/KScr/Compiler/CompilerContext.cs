using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using KScr.Antlr;
using KScr.Core;
using KScr.Core.Bytecode;
using KScr.Core.Exception;
using KScr.Core.Model;
using KScr.Core.Std;

namespace KScr.Compiler;

public class CompilerContext : ISymbolValidator
{
    private readonly IClassInfo? _class;
    private readonly List<string>? _imports;
    private readonly Package? _package;
    internal readonly Dictionary<string, Lambda> _lambdas = new();
    private readonly HashSet<Symbol> _symbols = new();
    private readonly Stack<string> _symbolGroups = new();
    private readonly Stack<IClass> _contexts = new();

    internal string SymbolGroup => _symbolGroups.Count == 0 ? Package.RootPackageName : _symbolGroups.Peek();
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
        return vm.FindType(name, _package, CurrentContext(vm));
    }

    public Symbol RegisterSymbol(string name, ITypeInfo type, SymbolType symbolType = SymbolType.Variable)
    {
        var symbol = new Symbol(symbolType, SymbolGroup, name, type);
        _symbols.Add(symbol);
        return symbol;
    }

    public bool UnregisterSymbol(Symbol symbol) => _symbols.Remove(symbol);
    public int UnregisterSymbols(string name) => _symbols.RemoveWhere(sym => sym.Name == name);

    public Symbol? FindSymbol(string name, ITypeInfo? type = null)
    {
        if (_symbols.FirstOrDefault(x => x.Name == name && (type == null || x.Type == type)) is { } s)
            return s;
        return Parent?.FindSymbol(name);
    }

    public T NextSymbolLevel<T>(string group, Func<T> task)
    {
        group = SymbolGroup + '.' + group;
        _symbolGroups.Push(group);
        T t = task();
        if (_symbolGroups.Pop() != group)
            throw new FatalException("Invalid symbol group removed from stack");
        _symbols.RemoveWhere(sym => sym.Group == group);
        return t;
    }

    public void PushContext(IClass cls) => _contexts.Push(cls);

    public IClass DropContext() => _contexts.Pop();

    public IClass CurrentContext(RuntimeBase vm) => _contexts.Count == 0 ? Class!.AsClass(vm) : _contexts.Peek();

    public void AddLambda(string? key, KScrParser.TupleExprContext? tupleExpr, ExecutableCode body)
    {
        throw new System.NotImplementedException();
    }
}
