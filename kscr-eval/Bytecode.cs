using System;
using System.Collections.Generic;
using KScr.Lib;
using KScr.Lib.Core;
using KScr.Lib.Model;

namespace KScr.Eval
{
    public class Bytecode : IStatement<Statement>
    {
        public StatementComponentType Type { get; }
        public List<Statement> Main { get; } = new List<Statement>();

        public IObject? Evaluate(RuntimeBase vm, IEvaluable prev, IObject? prevResult)
        {
            throw new NotImplementedException();
        }
    }
}