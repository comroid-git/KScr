using KScr.Lib.Store;

namespace KScr.Lib.Model
{
    public interface IEvaluable
    {
        public Stack Evaluate(RuntimeBase vm, Stack stack);
    }
}