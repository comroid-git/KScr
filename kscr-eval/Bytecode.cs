using System;
using System.Collections.Generic;
using KScr.Lib;
using KScr.Lib.Core;
using KScr.Lib.Model;
using KScr.Lib.Store;

namespace KScr.Eval
{
    public class Bytecode : IStatement<Statement>
    {
        public StatementComponentType Type { get; } = StatementComponentType.Code;
        public List<Statement> Main { get; } = new List<Statement>();

        public State Evaluate(RuntimeBase vm, IEvaluable? _, ref ObjectRef? __)
        {
            ObjectRef? rev = null;
            Statement? prev = null;
            State state = State.Normal;
            
            foreach (var statement in Main)
            {
                state = statement.Evaluate(vm, prev, ref rev);
                if (rev?.Value is ReturnValue)
                    return State.Return;
                prev = statement;
            }
            return state;
        }

        public void Append(IEvaluable? here)
        {
            if (here is Bytecode bc)
                Main.AddRange(bc.Main);
        }
    }
}