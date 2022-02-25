using System;
using System.Collections.Generic;
using System.IO;
using KScr.Lib.Core;
using KScr.Lib.Model;
using KScr.Lib.Store;

namespace KScr.Lib.Bytecode
{
    public class ExecutableCode : AbstractBytecode, IStatement<Statement>
    {
        protected override IEnumerable<AbstractBytecode> BytecodeMembers => Main;
        public StatementComponentType Type => StatementComponentType.Code;
        public IClassInstance TargetType { get; protected set; } = Class.VoidType;

        public List<Statement> Main { get; } = new();

        public State Evaluate(RuntimeBase vm, IEvaluable? _, ref ObjectRef output)
        {
            ObjectRef? rev = null;
            Statement? prev = null;
            var state = State.Normal;

            foreach (var statement in Main)
            {
                if (vm.StdIoMode)
                    state = statement.Evaluate(vm, prev, ref output);
                else state = statement.Evaluate(vm, prev, ref rev);
                if (rev?.Value is ReturnValue)
                    state = State.Return;
                if (state != State.Normal)
                    output = rev;

                prev = statement;
            }

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
            for (var i = 0; i < len; i++)
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