using System;
using System.Collections.Generic;
using System.IO;
using KScr.Lib.Core;
using KScr.Lib.Model;
using KScr.Lib.Store;

namespace KScr.Lib.Bytecode
{
    public class ExecutableCode : AbstractBytecode, IStatement<Statement>, IRuntimeSite
    {
        protected override IEnumerable<AbstractBytecode> BytecodeMembers => Main;
        public StatementComponentType Type => StatementComponentType.Code;
        public IClassInstance TargetType { get; protected set; } = Class.VoidType.DefaultInstance;

        public List<Statement> Main { get; } = new();

        public IRuntimeSite? Evaluate(RuntimeBase vm, ref State state, ref ObjectRef? rev, byte alt = 0)
        {
            foreach (var statement in Main)
            {
                state = statement.Evaluate(vm, ref rev);
                if (rev?.Value is ReturnValue)
                    state = State.Return;
            }

            return null;
        }

        public State Evaluate(RuntimeBase vm, ref ObjectRef rev)
        {
            var state = State.Normal;
            Evaluate(vm, ref state, ref rev);
            return state;
        }

        public void Append(IEvaluable? here)
        {
            if (here is ExecutableCode bc)
                Main.AddRange(bc.Main);
        }

        public override void Write(Stream stream)
        {
            stream.Write(BitConverter.GetBytes(Main.Count));
            foreach (var abstractBytecode in Main)
                abstractBytecode.Write(stream);
        }

        public override void Load(RuntimeBase vm, byte[] data, ref int index)
        {
            Main.Clear();

            var len = BitConverter.ToInt32(data, index);
            index += 4;
            Statement stmt;
            for (; len > 0; len--)
            {
                stmt = new Statement();
                stmt.Load(vm, data, ref index);
                Main.Add(stmt);
            }
        }

        public void Clear()
        {
            Main.ForEach(st => st.Clear());
            Main.Clear();
        }
    }
}