using KScr.Lib;
using KScr.Lib.Bytecode;
using KScr.Lib.Exception;
using KScr.Lib.Model;
using KScr.Lib.Store;

namespace KScr.Native;

public class NativeRunner : INativeRunner
{
    public NativeRunner()
    {
        // load all NativeImpl annotated members
    }

    public Stack Invoke(RuntimeBase vm, Stack stack, IClassMember member)
    {
        if (!member.IsNative())
            throw new FatalException("Member is not native: " + member);
        throw new NotImplementedException();
    }
}