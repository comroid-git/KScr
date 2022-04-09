using KScr.Core;
using KScr.Core.Bytecode;
using KScr.Core.Model;
using KScr.Core.Util;

namespace KScr.Bytecode.Port;

public abstract class AbstractBytecodePort : IBytecodePort
{
    protected AbstractBytecodePort(Version version)
    {
        Version = version;
    }

    public Version Version { get; }

    public abstract void Write(StringCache strings, Stream stream,
        IBytecode bytecode);
    public abstract T Load<T>(RuntimeBase vm, StringCache strings, Stream stream, Package pkg, Class? cls);
}