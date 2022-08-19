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
        WriteElementType(stream, bytecode);
        
        switch (bytecode)
        {
            case Class cls:
                Write(stream, strings, cls);
                break;
            case Property prop:
                Write(stream, strings, prop);
                break;
            case Method mtd:
                Write(stream, strings, mtd);
                break;
            case MethodParameter param:
                Write(stream, strings, param);
                break;
            case SourcefilePosition srcPos:
                Write(stream, strings, srcPos);
                break;
            case ExecutableCode code:
                Write(stream, strings, code);
                break;
            case Statement stmt:
                Write(stream, strings, stmt);
                break;
            case StatementComponent comp:
                Write(stream, strings, comp);
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
                throw new FatalException("Unknown bytecode type: " + bytecode.GetType());
        }

        stream.Flush();
    }
    public abstract void Write(Stream stream, StringCache strings, Class cls);
    public abstract void Write(Stream stream, StringCache strings, Property prop);
    public abstract void Write(Stream stream, StringCache strings, Method mtd);
    public abstract void Write(Stream stream, StringCache strings, MethodParameter param);
    public abstract void Write(Stream stream, StringCache strings, SourcefilePosition srcPos);
    public abstract void Write(Stream stream, StringCache strings, ExecutableCode code);
    public abstract void Write(Stream stream, StringCache strings, Statement stmt);
    public abstract void Write(Stream stream, StringCache strings, StatementComponent comp);
    protected void WriteElementType(Stream stream, IBytecode bytecode) => Write(stream, (byte)bytecode.ElementType);
    protected void Write(Stream stream, byte b) => stream!.Write(new[] { b });
    protected void Write(Stream stream, int i) => stream!.Write(BitConverter.GetBytes(i));
    protected void Write(Stream stream, uint ui) => stream!.Write(BitConverter.GetBytes(ui));
    protected void Write(Stream stream, ulong ul) => stream!.Write(BitConverter.GetBytes(ul));
    protected void Write(Stream stream, StringCache strings, string str) => Write(stream, strings[str]);
    protected void Write<T>(Stream stream, StringCache strings, T[] arr) where T : IBytecode
    {
        stream.Write(BitConverter.GetBytes(arr.Length));
        foreach (var node in arr)
            Write(stream, strings, node);
    }
    
    #endregion

    public abstract T Load<T>(RuntimeBase vm, StringCache strings, Stream stream, Package pkg, Class? cls);
}