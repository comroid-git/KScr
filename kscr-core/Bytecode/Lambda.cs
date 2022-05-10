using System.Collections.Generic;
using KScr.Core.Model;

namespace KScr.Core.Bytecode;

public class Lambda : IBytecode
{
    public string Name { get; init; }
    public ExecutableCode Code { get; init; }
    public Dictionary<string, ITypeInfo> Parameters { get; init; } = new();

    public BytecodeElementType ElementType => BytecodeElementType.Lambda;
}