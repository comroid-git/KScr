using KScr.Core;
using KScr.Core.Bytecode;
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

    public abstract void Write(StringCache strings, Stream stream,
        IBytecode bytecode);

    public abstract T Load<T>(RuntimeBase vm, StringCache strings, Stream stream, Package pkg, Class? cls);
}