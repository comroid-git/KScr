using KScr.Core.Bytecode;
using KScr.Core.Std;
using KScr.Core.Store;

namespace KScr.Core.Model;

public delegate IObjectRef? NativeImplMember(RuntimeBase vm, Stack stack, IObject target, params IObject[] args);

public interface INativeRunner
{
    Stack Invoke(RuntimeBase vm, Stack stack, IObject target, IClassMember member);
}