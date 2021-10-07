using System.Collections.Generic;
using KScr.Lib;
using KScr.Lib.Core;
using KScr.Lib.Model;
using KScr.Lib.Store;

namespace KScr.Eval
{
    public class Statement : IStatement<StatementComponent>
    {
        public StatementComponentType Type { get; internal set; }
        public TypeRef TargetType { get; internal set; } = TypeRef.VoidType;
        public List<StatementComponent> Main { get; internal set; }
        public IObject? Evaluate(RuntimeBase vm, IEvaluable prev, IObject? prevResult)
        {
            throw new System.NotImplementedException();
        }
    }
    public class StatementComponent : IStatementComponent
    {
        public StatementComponentType Type { get; internal set; }
        public string Arg { get; internal set; } = string.Empty;
        public BytecodeType CodeType { get; internal set; } = BytecodeType.Terminator;
        public IObject? Evaluate(RuntimeBase vm, IEvaluable prev, IObject? prevResult)
        {
            throw new System.NotImplementedException();
        }
    }
}