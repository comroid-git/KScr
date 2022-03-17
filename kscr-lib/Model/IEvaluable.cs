using KScr.Lib.Store;

namespace KScr.Lib.Model
{
    public interface IEvaluable
    {
        public void Evaluate(RuntimeBase vm, Stack stack, StackOutput copyFromStack = StackOutput.None);
    }
}