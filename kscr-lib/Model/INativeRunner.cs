using KScr.Lib.Bytecode;
using KScr.Lib.Store;

namespace KScr.Lib.Model;

public interface INativeRunner
{
    Stack Invoke(RuntimeBase vm, Stack stack, IClassMember member);
}