using KScr.Core.Store;

namespace KScr.Core.Model
{
    public interface IEvaluable
    {
        public Stack Evaluate(RuntimeBase vm, Stack stack);
    }
}