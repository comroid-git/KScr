using System;
using System.Collections.Generic;
using System.IO;
using KScr.Core.Bytecode;
using KScr.Core.Std;
using KScr.Core.Util;

namespace KScr.Core.Model;

public enum BytecodeElementType : byte
{
    Byte,
    Int32,
    UInt32,
    UInt64,
    String,

    Class,
    Method,
    MethodParameter,
    Property,
    SourcePosition,

    CodeBlock,
    Statement,
    Component,
    Lambda,
    LambdaParameter
}

public interface IBytecode
{
    BytecodeElementType ElementType { get; }

    static LiteralBytecode<byte> Byte(byte b)
    {
        return new LiteralBytecode<byte>(BytecodeElementType.Byte, b);
    }

    static LiteralBytecode<int> Int(int i)
    {
        return new LiteralBytecode<int>(BytecodeElementType.Int32, i);
    }

    static LiteralBytecode<uint> UInt(uint i)
    {
        return new LiteralBytecode<uint>(BytecodeElementType.UInt32, i);
    }

    static LiteralBytecode<ulong> ULong(ulong l)
    {
        return new LiteralBytecode<ulong>(BytecodeElementType.UInt64, l);
    }

    static LiteralBytecode<string> String(string str)
    {
        return new LiteralBytecode<string>(BytecodeElementType.String, str);
    }
}

public sealed class LiteralBytecode<T> : IBytecode
{
    public T Value;

    public LiteralBytecode(BytecodeElementType elementType, T value)
    {
        ElementType = elementType;
        Value = value;
    }

    public IEnumerable<IBytecode> Components => ArraySegment<IBytecode>.Empty;

    public IEnumerable<IBytecode> Header { get; } = Array.Empty<IBytecode>();
    public BytecodeElementType ElementType { get; }
}

public interface IBytecodePort
{
    BytecodeVersion BytecodeVersion { get; }
    void Write(Stream stream, StringCache strings, IBytecode bytecode);
    T Load<T>(RuntimeBase vm, StringCache strings, Stream stream, Package pkg, Class? cls);
}