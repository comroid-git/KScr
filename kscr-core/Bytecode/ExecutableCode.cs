using System;
using System.Collections.Generic;
using System.IO;
using KScr.Core.Model;
using KScr.Core.Store;
using KScr.Core.Util;

namespace KScr.Core.Bytecode
{
    public class ExecutableCode : IBytecode, IStatement<Statement>, IEvaluable
    {
        public BytecodeElementType ElementType => BytecodeElementType.CodeBlock;
        public IEnumerable<IBytecode> Components => Main;

        public Stack Evaluate(RuntimeBase vm, Stack stack)
        {
            foreach (var statement in Main)
            {
                statement.Evaluate(vm, stack);
                if (stack.State != State.Normal)
                    break;
            }

            return stack;
        }

        public StatementComponentType Type => StatementComponentType.Code;
        public IClassInstance TargetType { get; protected set; } = Class.VoidType.DefaultInstance;

        public List<Statement> Main { get; } = new();

        public void Append(Model.IEvaluable? here)
        {
            if (here is ExecutableCode bc)
                Main.AddRange(bc.Main);
        }
        
        public void Clear()
        {
            Main.ForEach(st => st.Clear());
            Main.Clear();
        }
    }
}