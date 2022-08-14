using System;

namespace KScr.Core.Model;

public interface ISymbolValidator
{
    Symbol RegisterSymbol(string name, ITypeInfo type, SymbolType symbolType = SymbolType.Variable);
    bool UnregisterSymbol(Symbol symbol);
    int UnregisterSymbols(string name);
    Symbol? FindSymbol(string name, ITypeInfo? type = null);
    T NextSymbolLevel<T>(string group, Func<T> task);
    IClass CurrentContext(RuntimeBase vm);
    void PushContext(IClass cls);
    IClass DropContext();
}

public enum SymbolType
{
    Variable
}

public sealed class Symbol
{
    public readonly SymbolType SymbolType;
    public readonly string Group;
    public readonly string Name;
    public readonly ITypeInfo Type;

    public Symbol(SymbolType symbolType, string group, string name, ITypeInfo type)
    {
        SymbolType = symbolType;
        Group = group;
        Name = name;
        Type = type;
    }

    public override string ToString() => $"{Type.DetailedName}: {Name}";

    public override bool Equals(object? obj)
    {
        if (obj is not Symbol sym)
            return false;
        return Name == sym.Name && Type == sym.Type;
    }

    public override int GetHashCode() => ToString().GetHashCode();
}