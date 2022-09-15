using System.Collections.Generic;
using KScr.Core.Model;

namespace KScr.Core.Bytecode;

public class Lambda : IBytecode
{
    public string Name { get; init; }
    public ExecutableCode Code { get; init; }
    public List<LambdaParameter> Parameters { get; init; } = new();

    public BytecodeElementType ElementType => BytecodeElementType.Lambda;
}

public class LambdaParameter : IBytecode
{
    public BytecodeElementType ElementType => BytecodeElementType.LambdaParameter;
}