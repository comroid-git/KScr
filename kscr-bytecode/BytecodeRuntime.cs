using System.Collections.Concurrent;
using KScr.Bytecode.Port;
using KScr.Core;
using KScr.Core.Model;
using static KScr.Core.BytecodeVersion;

namespace KScr.Bytecode;

public abstract class BytecodeRuntime : RuntimeBase
{
    public override IDictionary<Version, IBytecodePort> BytecodePorts { get; } =
        new ConcurrentDictionary<Version, IBytecodePort>()
        {
            [V_0_10] = new BytecodePortV0_10()
        };
}