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
        public IClass TargetType { get; set; } = Class.VoidType;
        public List<StatementComponent> Main { get; } = new List<StatementComponent>();

        public State Evaluate(RuntimeBase vm, IEvaluable? prev, ref ObjectRef rev)
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
            //stream.Write(BitConverter.GetBytes(TargetType.TypeId));
            stream.Write(BitConverter.GetBytes(Main.Count));
            foreach (var component in Main)
                (component as AbstractBytecode)!.Write(stream);
        }

        public override void Load(RuntimeBase vm, byte[] data, ref int index)
        {
            Main.Clear();
            
            Type = (StatementComponentType) BitConverter.ToInt16(data, index);
            index += 2;
            TargetType = (vm.ClassStore.FindType(BitConverter.ToInt64(data, index)) as Class)!;
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
        public Statement? SubStatement { get; set; }
        public StatementComponent? SubComponent { get; set; }
        public StatementComponentType Type { get; set; }
        public BytecodeType CodeType { get; set; } = BytecodeType.Terminator;

        public virtual State Evaluate(RuntimeBase vm, IEvaluable? prev, ref ObjectRef rev)
        {
            ObjectRef? output = null;
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
                        case BytecodeType.Parentheses:
                            return SubStatement!.Evaluate(vm, this, ref rev);
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
                    if (SubComponent == null || (SubComponent.Type & StatementComponentType.Expression) == 0)
                        throw new InternalException("Invalid operator; no right-hand Expression found");
                    state = SubComponent.Evaluate(vm, this, ref output!);
                    if (state != State.Normal)
                        return state;
                    rev = rev.Value!.Invoke(vm, "op" + Arg, output.Value!)!;
                    break;
                case StatementComponentType.Provider:
                    // non-constant expressions

                    if (VariableContext == VariableContext.This)
                    {
                        rev = vm.Stack.This;
                        return State.Normal;
                    }

                    switch (CodeType)
                    {
                        case BytecodeType.ExpressionVariable:
                            // read variable
                            rev = vm[VariableContext, Arg];
                            break;
                        case BytecodeType.Call:
                            // invoke member
                            if (rev == null)
                                throw new InternalException("Invalid call; no target found");
                            if (rev.Type.DeclaredMembers[Arg] is Method mtd)
                            {
                                if (!(SubComponent is MethodParameterComponent mpc) ||
                                    (SubComponent.Type & StatementComponentType.Code) == 0)
                                    throw new InternalException("Invalid method call; no parameters found");
                                output = new ObjectRef(Class.VoidType, mtd.Parameters.Count);
                                state = mpc.Evaluate(vm, null, ref output);
                                if (state != State.Normal)
                                    throw new InternalException("Invalid state after evaluating method parameters");
                                if (mtd.IsStatic())
                                    vm.Stack.StepDown(mtd.Parent, mtd.FullName);
                                else vm.Stack.StepDown(rev, mtd.FullName);
                                mtd.Evaluate(vm, ref state, ref output); // todo inspect
                                vm.Stack.StepUp();
                                rev = output;
                                //mpc.Evaluate(vm, null, ref output);
                            }
                            else throw new System.Exception("Invalid state; not a method");
                            
                            break;
                        case BytecodeType.StdioExpression:
                            rev = vm.StdioRef;
                            return State.Normal;
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
                case StatementComponentType.Emitter:
                    if (SubComponent == null || (SubComponent.Type & StatementComponentType.Expression) == 0)
                        throw new InternalException("Invalid emitter; no Expression found");
                    if (!rev.IsPipe)
                        throw new InternalException("Cannot emit value into non-pipe accessor");
                    state = SubComponent.Evaluate(vm, this, ref output);
                    rev.WriteAccessor!.Evaluate(vm, null, ref output);
                    return state;
                case StatementComponentType.Consumer:
                    if (SubComponent == null || (SubComponent.Type & StatementComponentType.Declaration) == 0)
                        throw new InternalException("Invalid consumer; no declaration found");
                    if (!rev.IsPipe)
                        throw new InternalException("Cannot consume value from non-pipe accessor");
                    state = SubComponent.Evaluate(vm, this, ref output);
                    rev.WriteAccessor!.Evaluate(vm, null, ref output);
                    return state;
                case StatementComponentType.Lambda:
                    throw new NotImplementedException();
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return State.Normal;
        }

        protected override IEnumerable<AbstractBytecode> BytecodeMembers => SubComponent != null ? new []{ SubComponent } : System.Array.Empty<AbstractBytecode>();
        
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