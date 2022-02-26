using System;
using System.Collections.Generic;
using System.IO;
using KScr.Lib.Core;
using KScr.Lib.Exception;
using KScr.Lib.Model;
using KScr.Lib.Store;
using Array = System.Array;
using Range = KScr.Lib.Core.Range;
using String = KScr.Lib.Core.String;

namespace KScr.Lib.Bytecode
{
    public class Statement : AbstractBytecode, IStatement<StatementComponent>
    {
        protected override IEnumerable<AbstractBytecode> BytecodeMembers => Main;
        public StatementComponentType Type { get; set; }
        public BytecodeType CodeType { get; set; } = BytecodeType.Undefined;
        public IClassInstance TargetType { get; set; } = Class.VoidType;
        public List<StatementComponent> Main { get; } = new();

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

        public override void Write(Stream stream)
        {
            stream.Write(BitConverter.GetBytes((byte)Type));
            //stream.Write(BitConverter.GetBytes(TargetType.TypeId));
            stream.Write(BitConverter.GetBytes(Main.Count));
            foreach (var component in Main)
                component!.Write(stream);
        }

        public override void Load(RuntimeBase vm, byte[] data, ref int index)
        {
            Main.Clear();

            Type = (StatementComponentType)BitConverter.ToInt16(data, index);
            index += 2;
            TargetType = (vm.ClassStore.FindType(BitConverter.ToInt64(data, index)) as Class)!;
            index += 8;
            var len = BitConverter.ToInt32(data, index);
            index += 4;
            StatementComponent stmt;
            for (var i = 0; i < len; i++)
            {
                stmt = new StatementComponent();
                stmt.Load(vm, data, ref index);
                Main.Add(stmt);
            }
        }

        public void Clear()
        {
            Main.Clear();
        }
    }

    public class StatementComponent : AbstractBytecode, IStatementComponent
    {
        public Statement Statement { get; set; } = null!;
        public VariableContext VariableContext { get; set; }
        public string Arg { get; set; } = string.Empty;
        public ulong ByteArg { get; set; } = 0x0;
        public Statement? SubStatement { get; set; }
        public StatementComponent? SubComponent { get; set; }
        public Statement? AltStatement { get; set; }
        public StatementComponent? AltComponent { get; set; }
        public ExecutableCode? InnerCode { get; set; }

        protected override IEnumerable<AbstractBytecode> BytecodeMembers =>
            SubComponent != null ? new[] { SubComponent } : Array.Empty<AbstractBytecode>();

        public StatementComponentType Type { get; set; }
        public BytecodeType CodeType { get; set; } = BytecodeType.Undefined;

        public virtual State Evaluate(RuntimeBase vm, IEvaluable? prev, ref ObjectRef rev)
        {
            ObjectRef? buf = null;
            State state = State.Normal;
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
                        case BytecodeType.LiteralRange:
                            var bs = BitConverter.GetBytes(ByteArg);
                            int start = BitConverter.ToInt32(new[]{bs[0],bs[1],bs[2],bs[3]}),
                                end = BitConverter.ToInt32(new[]{bs[4],bs[5],bs[6],bs[7]});
                            rev = Range.Instance(vm, start, end);
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
                            buf = null;
                            state = SubComponent.Evaluate(vm, this, ref buf!);
                            rev.Value = buf?.Value;
                            return state;
                        case BytecodeType.Return:
                            // return
                            if (SubComponent == null || (SubComponent.Type & StatementComponentType.Expression) == 0)
                                throw new InternalException("Invalid return statement; no Expression found");
                            state = SubComponent.Evaluate(vm, this, ref rev);
                            return state == State.Normal ? State.Return : state;
                        case BytecodeType.StmtIf:
                            state = SubStatement!.Evaluate(vm, this, ref buf!);
                            if (buf.ToBool())
                                state = InnerCode!.Evaluate(vm, this, ref rev);
                            else if (SubComponent?.CodeType == BytecodeType.StmtElse)
                                state = SubComponent!.InnerCode!.Evaluate(vm, this, ref rev);
                            return state;
                        case BytecodeType.StmtForN:
                            var n = vm[VariableContext.Local, Arg] = new ObjectRef(Class.NumericIntegerType);
                            state = SubStatement!.Evaluate(vm, this, ref buf!);
                            if (state != State.Normal)
                                break;
                            var range = (buf.Value as Range)!;
                            n.Value = range.start(vm).Value;
                            do
                            {
                                state = InnerCode!.Evaluate(vm, null, ref rev);
                                n.Value = range.accumulate(vm, (n.Value as Numeric)!).Value;
                            } while (state == State.Normal && range.test(vm, (n.Value as Numeric)!).ToBool());

                            vm[VariableContext.Local, Arg] = null;
                            return state;
                        default:
                            throw new NotImplementedException();
                    }

                    break;
                case StatementComponentType.Operator:
                    if (SubComponent == null || (SubComponent.Type & StatementComponentType.Expression) == 0)
                        throw new InternalException("Invalid operator; no right-hand Expression found");
                    state = SubComponent.Evaluate(vm, this, ref buf!);
                    if (state != State.Normal)
                        return state;
                    var op = (Operator)ByteArg;
                    switch (op)
                    {
                        case Operator.LogicalNot:
                            if (SubComponent == null || (SubComponent.Type & StatementComponentType.Expression) == 0)
                                throw new InternalException("Invalid unary operator; missing right operand");
                            state = SubComponent.Evaluate(vm, this, ref rev);
                            rev = rev.LogicalNot(vm);
                            return state;
                        case Operator.Equals:
                        case Operator.NotEquals:
                            if (rev == null)
                                throw new InternalException("Invalid binary operator; missing left operand");
                            if (SubComponent == null || (SubComponent.Type & StatementComponentType.Expression) == 0)
                                throw new InternalException("Invalid binary operator; missing right operand");
                            state = SubComponent.Evaluate(vm, this, ref buf);
                            rev = rev.Value!.Invoke(vm, "equals", buf.Value) ?? vm.ConstantFalse;
                            if (op == Operator.NotEquals)
                                rev = rev.LogicalNot(vm);
                            return state;
                        case Operator.IncrementRead:
                        case Operator.DecrementRead:
                        case Operator.ArithmeticNot:
                            if (SubComponent == null || (SubComponent.Type & StatementComponentType.Expression) == 0)
                                throw new InternalException("Invalid unary operator; missing right numeric operand");
                            state = SubComponent.Evaluate(vm, this, ref rev);
                            if (rev.Value is not Numeric right2)
                                throw new InternalException("Invalid unary operator; missing right numeric operand");
                            rev = right2.Operator(vm, op);
                            return state;
                        case Operator.ReadIncrement:
                        case Operator.ReadDecrement:
                            if (rev.Value is not Numeric left2)
                                throw new InternalException("Invalid unary operator; missing left numeric operand");
                            rev = left2.Operator(vm, op);
                            return state;
                        case Operator.Plus:
                        case Operator.Minus:
                        case Operator.Multiply:
                        case Operator.Divide:
                        case Operator.Modulus:
                        case Operator.Circumflex:
                        case Operator.Greater:
                        case Operator.GreaterEq:
                        case Operator.Lesser:
                        case Operator.LesserEq:
                            if (SubComponent == null ||
                                (SubComponent.Type & StatementComponentType.Expression) == 0)
                                throw new InternalException(
                                    "Invalid binary operator; missing right numeric operand");
                            state = SubComponent.Evaluate(vm, this, ref buf);
                            // try to use overrides
                            if (op == Operator.Plus && rev.Value?.Type.Name == "str" 
                                || (rev.Value?.Type.BaseClass.DeclaredMembers.ContainsKey("op" + op) ?? false))
                            {
                                rev = rev.Value!.Invoke(vm, "op" + op, buf.Value)!;
                            } 
                            else
                            {
                                // else try numeric operation
                                if (rev.Value is not Numeric left3)
                                    throw new InternalException(
                                        "Invalid binary operator; missing left numeric operand");
                                rev = left3.Operator(vm, op, buf.Value as Numeric);
                                return state;
                            }

                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
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
                            if (((Class)rev.Type).DeclaredMembers[Arg] is Method mtd)
                            {
                                if (!(SubComponent is MethodParameterComponent mpc) ||
                                    (SubComponent.Type & StatementComponentType.Code) == 0)
                                    throw new InternalException("Invalid method call; no parameters found");
                                buf = new ObjectRef(Class.VoidType, mtd.Parameters.Count);
                                state = mpc.Evaluate(vm, null, ref buf);
                                if (state != State.Normal)
                                    throw new InternalException("Invalid state after evaluating method parameters");
                                if (mtd.IsStatic())
                                    vm.Stack.StepDown(mtd.Parent, mtd.FullName);
                                else vm.Stack.StepDown(rev, mtd.FullName);
                                mtd.Evaluate(vm, ref state, ref buf); // todo inspect
                                vm.Stack.StepUp();
                                rev = buf;
                                //mpc.Evaluate(vm, null, ref output);
                            }
                            else
                            {
                                throw new System.Exception("Invalid state; not a method");
                            }

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
                    if (SubStatement == null || (SubStatement.Type & StatementComponentType.Expression) == 0)
                        throw new InternalException("Invalid assignment; no Expression found");
                    buf = null;
                    state = SubStatement!.Evaluate(vm, this, ref buf!);
                    rev.Value = buf.Value;
                    return state;
                case StatementComponentType.Emitter:
                    if (SubStatement == null || (SubStatement.Type & StatementComponentType.Expression) == 0)
                        throw new InternalException("Invalid emitter; no Expression found");
                    if (!rev.IsPipe)
                        throw new InternalException("Cannot emit value into non-pipe accessor");
                    state = SubStatement.Evaluate(vm, this, ref buf!);
                    rev.WriteAccessor!.Evaluate(vm, null, ref buf);
                    return state;
                case StatementComponentType.Consumer:
                    if (SubStatement == null || (SubStatement.Type & StatementComponentType.Declaration) == 0)
                        throw new InternalException("Invalid consumer; no declaration found");
                    if (!rev.IsPipe)
                        throw new InternalException("Cannot consume value from non-pipe accessor");
                    state = SubStatement.Evaluate(vm, this, ref buf!);
                    rev.ReadAccessor!.Evaluate(vm, null, ref buf);
                    return state;
                case StatementComponentType.Lambda:
                    throw new NotImplementedException();
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return state;
        }

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
            _Load(vm, data, ref index, out var sct, out var vct, out var bty, out string arg, out var sub);
            Type = sct;
            VariableContext = vct;
            CodeType = bty;
            Arg = arg;
            SubComponent = sub;
        }

        private static void _Load(RuntimeBase vm, byte[] data, ref int index, out StatementComponentType sct,
            out VariableContext vct, out BytecodeType bty, out string arg, out StatementComponent? sub)
        {
            sct = (StatementComponentType)data[index];
            index += 1;
            vct = (VariableContext)data[index];
            index += 1;
            bty = (BytecodeType)BitConverter.ToUInt32(data, index);
            index += 4;
            var len = BitConverter.ToInt32(data, index);
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