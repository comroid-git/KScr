﻿using System;
using System.Collections.Generic;
using System.IO;
using KScr.Core.Model;
using KScr.Core.Store;

namespace KScr.Core.Bytecode
{
    public class ExecutableCode : AbstractBytecode, IStatement<Statement>, IEvaluable
    {
        protected override IEnumerable<AbstractBytecode> BytecodeMembers => Main;

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