using KScr.Lib.Bytecode;
using KScr.Lib.Core;
using KScr.Lib.Store;

namespace KScr.Lib.Model;

public delegate IObjectRef? NativeImplMember(RuntimeBase vm, Stack stack, IObject target, params IObject[] args);

public interface INativeRunner
{
    Stack Invoke(RuntimeBase vm, Stack stack, IObject target, IClassMember member);
}