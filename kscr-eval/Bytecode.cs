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
        public StatementComponentType Type { get; }
        public List<Statement> Main { get; } = new List<Statement>();

        public ObjectRef? Evaluate(RuntimeBase vm, IEvaluable? _, ObjectRef? __)
        {
            ObjectRef? rev = null;
            Statement? prev = null;
            
            foreach (var statement in Main)
            {
                rev = statement.Evaluate(vm, prev, rev);
                if (rev?.Value is ReturnValue)
                    return rev;
                prev = statement;
            }

            return rev;
        }

        public void Append(IEvaluable? here)
        {
            if (here is Bytecode bc)
                Main.AddRange(bc.Main);
        }
    }
}