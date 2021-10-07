using KScr.Lib.Core;

namespace KScr.Lib.Model
{
    public interface IEvaluable
    {
        public IObject? Evaluate(RuntimeBase vm, IEvaluable prev, IObject? prevResult);
    }
}