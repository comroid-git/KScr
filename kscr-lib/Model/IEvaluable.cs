using KScr.Lib.Store;

namespace KScr.Lib.Model
{
    public interface IEvaluable
    {
        public State Evaluate(RuntimeBase vm, ref ObjectRef rev);
    }
}