using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KScr.Lib.Core;
using KScr.Lib.Exception;
using KScr.Lib.Model;
using KScr.Lib.Store;
using String = KScr.Lib.Core.String;

namespace KScr.Lib.Bytecode
{
    public class Statement : AbstractBytecode, IStatement<StatementComponent>
    {
        public StatementComponentType Type { get; set; }
        public IClassRef TargetType { get; set; } = Class.VoidType;
        public List<StatementComponent> Main { get; } = new List<StatementComponent>();

        public State Evaluate(RuntimeBase vm, IEvaluable? prev, ref ObjectRef? rev)
        {
            var state = State.Normal;

            foreach (var component in Main)
            {
                switch (component.Type)
                {
                    default:
                        state = component.Evaluate(vm, prev, ref rev);
                        break;
                }

                if (state != State.Normal)
                    break;
                prev = component;
            }

            return state;
        }

        protected override IEnumerable<AbstractBytecode> BytecodeMembers => Main.Cast<AbstractBytecode>();
        public override void Write(Stream stream)
        {
            stream.Write(BitConverter.GetBytes((byte)Type));
            stream.Write(BitConverter.GetBytes(TargetType.TypeId));
            stream.Write(BitConverter.GetBytes(Main.Count));
            foreach (var component in Main)
                (component as AbstractBytecode)!.Write(stream);
        }

        public override void Load(RuntimeBase vm, byte[] data, ref int index)
        {
            Main.Clear();
            
            Type = (StatementComponentType) BitConverter.ToInt16(data, index);
            index += 2;
            TargetType = vm.ClassStore.FindType(BitConverter.ToInt64(data, index));
            index += 8;
            int len = BitConverter.ToInt32(data, index);
            index += 4;
            StatementComponent stmt;
            for (int i = 0; i < len; i++)
            {
                stmt = new StatementComponent();
                stmt.Load(vm, data, ref index);
                Main.Add(stmt);
            }
        }
    }

    public class StatementComponent : AbstractBytecode, IStatementComponent
    {
        public Statement Statement { get; set; } = null!;
        public VariableContext VariableContext { get; set; }
        public string Arg { get; set; } = string.Empty;
        public StatementComponent? SubComponent { get; set; }
        public StatementComponentType Type { get; set; }
        public BytecodeType CodeType { get; set; } = BytecodeType.Terminator;

        public State Evaluate(RuntimeBase vm, IEvaluable? prev, ref ObjectRef? rev)
        {
            ObjectRef? output;
            State state;
            switch (Type)
            {
                case StatementComponentType.Expression:
                    // constant expressions; literals
                    switch (CodeType)
                    {
                        case BytecodeType.LiteralNumeric:
                            rev = Numeric.Compile(vm, Arg);
                            return State.Normal;
                        case BytecodeType.LiteralString:
                            rev = String.Instance(vm, Arg);
                            return State.Normal;
                        case BytecodeType.LiteralTrue:
                            rev = vm.ConstantTrue;
                            return State.Normal;
                        case BytecodeType.LiteralFalse:
                            rev = vm.ConstantFalse;
                            return State.Normal;
                        case BytecodeType.Null:
                            rev = vm.ConstantVoid;
                            return State.Normal;
                        case BytecodeType.Expression:
                            rev = vm.Context.This;
                            return State.Normal;
                    }

                    return State.Normal;
                case StatementComponentType.Declaration:
                    // variable declaration
                    rev = vm[VariableContext, Arg] = new ObjectRef(Statement.TargetType);
                    break;
                case StatementComponentType.Pipe:
                    throw new NotImplementedException();
                case StatementComponentType.Code:
                    // subtypes
                    switch (CodeType)
                    {
                        case BytecodeType.Assignment:
                            // assignment
                            if (rev == null)
                                throw new InternalException("Invalid assignment; missing variable name");
                            if (SubComponent == null || (SubComponent.Type & StatementComponentType.Expression) == 0)
                                throw new InternalException("Invalid assignment; no Expression found");
                            output = null;
                            state = SubComponent.Evaluate(vm, this, ref output);
                            rev.Value = output?.Value;
                            return state;
                        case BytecodeType.Return:
                            // return
                            if (SubComponent == null || (SubComponent.Type & StatementComponentType.Expression) == 0)
                                throw new InternalException("Invalid return statement; no Expression found");
                            state = SubComponent.Evaluate(vm, this, ref rev);
                            return state == State.Normal ? State.Return : state;
                    }

                    throw new NotImplementedException();
                case StatementComponentType.Operator:
                    throw new NotImplementedException();
                case StatementComponentType.Provider:
                    // non-constant expressions
                    switch (CodeType)
                    {
                        case BytecodeType.ExpressionVariable:
                            // read variable
                            rev = vm[VariableContext, Arg];
                            break;
                        case BytecodeType.Call:
                            // invoke method
                            rev = rev?.Value?.Invoke(vm, Arg); // todo: allow parameters
                            break;
                    }

                    break;
                case StatementComponentType.Setter:
                    // assignment
                    if (rev == null)
                        throw new InternalException("Invalid assignment; missing variable name");
                    if (SubComponent == null || (SubComponent.Type & StatementComponentType.Expression) == 0)
                        throw new InternalException("Invalid assignment; no Expression found");
                    output = null;
                    state = SubComponent.Evaluate(vm, this, ref output);
                    rev.Value = output?.Value;
                    return state;

                    break;
                case StatementComponentType.Emitter:
                    throw new NotImplementedException();
                case StatementComponentType.Lambda:
                    throw new NotImplementedException();
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return State.Normal;
        }

        protected override IEnumerable<AbstractBytecode> BytecodeMembers => SubComponent != null ? new []{ SubComponent } : Array.Empty<AbstractBytecode>();
        
        public override void Write(Stream stream)
        {
            stream.Write(BitConverter.GetBytes((byte)Type));
            stream.Write(BitConverter.GetBytes((byte)VariableContext));
            stream.Write(BitConverter.GetBytes((uint)CodeType));
            stream.Write(BitConverter.GetBytes(Arg.Length));
            stream.Write(RuntimeBase.Encoding.GetBytes(Arg));
            bool b;
            stream.Write(BitConverter.GetBytes(b = SubComponent != null));
            if (b)
                SubComponent!.Write(stream);
        }

        public override void Load(RuntimeBase vm, byte[] data, ref int index)
        {
            _Load(vm, data, ref index, out var sct, out var vct, out var bty, out var arg, out var sub);
            Type = sct;
            VariableContext = vct;
            CodeType = bty;
            Arg = arg;
            SubComponent = sub;
        }

        private static void _Load(RuntimeBase vm, byte[] data, ref int index, out StatementComponentType sct, out VariableContext vct, out BytecodeType bty, out string arg, out StatementComponent? sub)
        {
            sct = (StatementComponentType)data[index];
            index += 1;
            vct = (VariableContext)data[index];
            index += 1;
            bty = (BytecodeType)BitConverter.ToUInt32(data, index);
            index += 4;
            int len = BitConverter.ToInt32(data, index);
            index += 4;
            arg = RuntimeBase.Encoding.GetString(data, index, len);
            index += len;
            if (BitConverter.ToBoolean(data, index++))
                sub = Read(vm, data, index);
            else sub = null;
        }

        private static StatementComponent Read(RuntimeBase vm, byte[] data, int index)
        {
            var comp = new StatementComponent();
            comp.Load(vm, data, ref index);
            return comp;
        }
    }
}