using KScr.Core.Std;
using KScr.Core.Store;

namespace KScr.Core.Model;

public interface IInvokable
{
    public Stack Invoke(RuntimeBase vm, Stack stack, IObjectRef target, params IObject?[] args);
}