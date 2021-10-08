using KScr.Lib.Core;
using KScr.Lib.Store;

namespace KScr.Lib.Model
{
    public interface IEvaluable
    {
        public ObjectRef? Evaluate(RuntimeBase vm, IEvaluable? prev, ObjectRef? prevRef);
    }
}