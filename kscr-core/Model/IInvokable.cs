using KScr.Core.Std;
using KScr.Core.Store;

namespace KScr.Core.Model;

public interface IInvokable
{
    public Stack Invoke(RuntimeBase vm, Stack stack, IObject? target = null, StackOutput maintain = StackOutput.Omg,
        params IObject?[] args);
}