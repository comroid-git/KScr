using System.Diagnostics;
using KScr.Core;
using KScr.Core.Bytecode;
using KScr.Core.Exception;
using KScr.Core.Model;
using KScr.Core.Std;
using KScr.Core.Util;

namespace KScr.Bytecode.Adapter;

public abstract class AbstractBytecodeAdapter : IBytecodePort
{
    protected AbstractBytecodeAdapter(BytecodeVersion version)
    {
        BytecodeVersion = version;
    }

    public BytecodeVersion BytecodeVersion { get; }

    #region Writing

    public void Write(Stream stream, StringCache strings, IBytecode bytecode)
    {
        Write(stream, bytecode.ElementType);
        
        switch (bytecode)
        {
            case Class cls:
                WriteClass(stream, strings, cls);
                break;
            case Property prop:
                WriteProperty(stream, strings, prop);
                break;
            case Method mtd:
                WriteMethod(stream, strings, mtd);
                break;
            case MethodParameter param:
                WriteMethodParameter(stream, strings, param);
                break;
            case SourcefilePosition srcPos:
                WriteSrcPos(stream, strings, srcPos);
                break;
            case ExecutableCode code:
                WriteCode(stream, strings, code);
                break;
            case Statement stmt:
                WriteStatement(stream, strings, stmt);
                break;
            case StatementComponent comp:
                WriteComponent(stream, strings, comp);
                break;
            case LiteralBytecode<byte> b:
                Write(stream, b.Value);
                break;
            case LiteralBytecode<int> i:
                Write(stream, i.Value);
                break;
            case LiteralBytecode<uint> ui:
                Write(stream, ui.Value);
                break;
            case LiteralBytecode<ulong> ul:
                Write(stream, ul.Value);
                break;
            case LiteralBytecode<string> str:
                Write(stream, strings, str.Value);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(bytecode.ElementType), bytecode.ElementType, "Unknown bytecode type");
        }

        stream.Flush();
    }
    protected abstract void WriteClass(Stream stream, StringCache strings, Class cls);
    protected abstract void WriteProperty(Stream stream, StringCache strings, Property prop);
    protected abstract void WriteMethod(Stream stream, StringCache strings, Method mtd);
    protected abstract void WriteMethodParameter(Stream stream, StringCache strings, MethodParameter param);
    protected abstract void WriteSrcPos(Stream stream, StringCache strings, SourcefilePosition srcPos);
    protected abstract void WriteCode(Stream stream, StringCache strings, ExecutableCode code);
    protected abstract void WriteStatement(Stream stream, StringCache strings, Statement stmt);
    protected abstract void WriteComponent(Stream stream, StringCache strings, StatementComponent comp);
    private void Write(Stream stream, byte[] bytes, BytecodeElementType type)
    {
        Write(stream, type);
        stream.Write(bytes);
    }

    protected void Write(Stream stream, BytecodeElementType type) => stream.Write(new[] { (byte)type });
    protected void Write(Stream stream, byte b) => Write(stream, new[] { b }, BytecodeElementType.Byte);
    protected void Write(Stream stream, int i) => Write(stream, BitConverter.GetBytes(i), BytecodeElementType.Int32);
    protected void Write(Stream stream, uint ui) =>
        Write(stream, BitConverter.GetBytes(ui), BytecodeElementType.UInt32);
    protected void Write(Stream stream, ulong ul) =>
        Write(stream, BitConverter.GetBytes(ul), BytecodeElementType.UInt64);
    protected void Write(Stream stream, StringCache strings, string str) =>
        Write(stream, BitConverter.GetBytes(strings[str]), BytecodeElementType.String);
    protected void Write<T>(Stream stream, StringCache strings, T[] arr) where T : IBytecode
    {
        Write(stream, arr.Length);
        foreach (var node in arr)
            Write(stream, strings, node);
    }
    
    #endregion

    #region Loading

    public T Load<T>(RuntimeBase vm, StringCache strings, Stream stream, Package pkg, Class? cls)
    {
        BytecodeElementType type = ReadElementType(stream);

        switch (type)
        {
            case BytecodeElementType.Byte:
                ValidateBRT<byte, T>();
                return (T)(object)ReadByte(stream);
            case BytecodeElementType.Int32:
                ValidateBRT<int, T>();
                return (T)(object)ReadInt(stream);
            case BytecodeElementType.UInt32:
                ValidateBRT<uint, T>();
                return (T)(object)ReadUInt(stream);
            case BytecodeElementType.UInt64:
                ValidateBRT<ulong, T>();
                return (T)(object)ReadULong(stream);
            case BytecodeElementType.String:
                ValidateBRT<string, T>();
                return (T)(object)ReadString(stream, strings);
            case BytecodeElementType.Class:
                ValidateBRT<Class, T>();
                return (T)(object)ReadClass(vm, stream, strings, pkg);
            case BytecodeElementType.Method:
                ValidateBRT<Method, T>();
                return (T)(object)ReadMethod(vm, stream, strings, pkg, cls ?? throw new NullReferenceException("Class cannot be null"));
            case BytecodeElementType.MethodParameter:
                ValidateBRT<MethodParameter, T>();
                return (T)(object)ReadMethodParameter(vm, stream, strings, pkg, cls ?? throw new NullReferenceException("Class cannot be null"));
            case BytecodeElementType.Property:
                ValidateBRT<Property, T>();
                return (T)(object)ReadProperty(vm, stream, strings, pkg, cls ?? throw new NullReferenceException("Class cannot be null"));
            case BytecodeElementType.SourcePosition:
                ValidateBRT<SourcefilePosition, T>();
                return (T)(object)ReadSrcPos(vm, stream, strings, pkg, cls ?? throw new NullReferenceException("Class cannot be null"));
            case BytecodeElementType.CodeBlock:
                ValidateBRT<ExecutableCode, T>();
                return (T)(object)ReadCode(vm, stream, strings, pkg, cls ?? throw new NullReferenceException("Class cannot be null"));
            case BytecodeElementType.Statement:
                ValidateBRT<Statement, T>();
                return (T)(object)ReadStatement(vm, stream, strings, pkg, cls ?? throw new NullReferenceException("Class cannot be null"));
            case BytecodeElementType.Component:
                ValidateBRT<StatementComponent, T>();
                return (T)(object)ReadComponent(vm, stream, strings, pkg, cls ?? throw new NullReferenceException("Class cannot be null"));
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown bytecode type");
        }
    }
    protected abstract Class ReadClass(RuntimeBase vm, Stream stream, StringCache strings, Package pkg);
    protected abstract Property ReadProperty(RuntimeBase vm, Stream stream, StringCache strings, Package pkg, Class cls);
    protected abstract Method ReadMethod(RuntimeBase vm, Stream stream, StringCache strings, Package pkg, Class cls);
    protected abstract MethodParameter ReadMethodParameter(RuntimeBase vm, Stream stream, StringCache strings, Package pkg, Class cls);
    protected abstract SourcefilePosition ReadSrcPos(RuntimeBase vm, Stream stream, StringCache strings, Package pkg, Class cls);
    protected abstract ExecutableCode ReadCode(RuntimeBase vm, Stream stream, StringCache strings, Package pkg, Class cls);
    protected abstract Statement ReadStatement(RuntimeBase vm, Stream stream, StringCache strings, Package pkg, Class cls);
    protected abstract StatementComponent ReadComponent(RuntimeBase vm, Stream stream, StringCache strings, Package pkg, Class cls);
    private byte[] Read(Stream stream, int len, BytecodeElementType type)
    {
        ValidateBET(type, ReadElementType(stream));
        byte[] buf = new byte[len];
        if (len != stream.Read(buf, 0, buf.Length))
            throw new FatalException("Invalid amount of bytes read");
        return buf;
    }
    protected BytecodeElementType ReadElementType(Stream stream)
    {
        byte[] buf = new byte[1];
        if (1 != stream.Read(buf, 0, buf.Length))
            throw new FatalException("Invalid amount of bytes read for BytecodeElementType");
        return (BytecodeElementType)buf[0];
    }
    protected byte ReadByte(Stream stream) => Read(stream, sizeof(byte), BytecodeElementType.Byte)[0];
    protected int ReadInt(Stream stream) => BitConverter.ToInt32(Read(stream, sizeof(int), BytecodeElementType.Int32));
    protected uint ReadUInt(Stream stream) =>
        BitConverter.ToUInt32(Read(stream, sizeof(uint), BytecodeElementType.UInt32));
    protected ulong ReadULong(Stream stream) =>
        BitConverter.ToUInt64(Read(stream, sizeof(ulong), BytecodeElementType.UInt64));
    protected string ReadString(Stream stream, StringCache strings) =>
        strings[BitConverter.ToInt32(Read(stream, sizeof(int), BytecodeElementType.String))] ??
        throw new FatalException("Missing string!");
    protected T[] ReadArray<T>(RuntimeBase vm, Stream stream, StringCache strings, Package pkg, Class? cls)
    {
        var l = ReadInt(stream);
        var yields = new T[l];
        for (var i = 0; i < l; i++)
            yields[i] = Load<T>(vm, strings, stream, pkg, cls);
        return yields;
    }
    
    #endregion

    private void ValidateBET(BytecodeElementType expected, BytecodeElementType actual)
    {
        if (expected != actual)
            throw new FatalException($"Invalid BytecodeElementType {actual}; expected {expected}");
    }

    private void ValidateBRT<TExpected, TActual>()
    {
        var expected = typeof(TExpected);
        var actual = typeof(TActual);
        if (expected != actual && !expected.IsAssignableTo(actual))
            throw new FatalException($"Invalid Bytecode Return Type {actual}; expected {expected}");
    }
}