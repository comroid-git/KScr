using System.Collections.Concurrent;
using KScr.Bytecode.Port;
using KScr.Core;
using KScr.Core.Model;
using static KScr.Core.BytecodeVersion;

namespace KScr.Bytecode;

public abstract class BytecodeRuntime : RuntimeBase
{
    public override IDictionary<BytecodeVersion, IBytecodePort> BytecodePorts { get; } =
        new ConcurrentDictionary<BytecodeVersion, IBytecodePort>()
        {
            [V_0_10] = new BytecodePortV0_10()
        };
}