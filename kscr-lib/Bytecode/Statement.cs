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
        public BytecodeType CodeType { get; set; } = BytecodeType.Undefined;
        public string? Arg { get; set; }
        public StatementComponentType Type { get; set; }
        public IClassInstance TargetType { get; set; } = Class.VoidType.DefaultInstance;
        public List<StatementComponent> Main { get; } = new();

        public State Evaluate(RuntimeBase vm, ref ObjectRef rev)
        {
            var state = State.Normal;
            rev = vm.Stack.This!;

            foreach (var component in Main)
            {
                switch (component.Type)
                {
                    default:
                        state = component.Evaluate(vm, ref rev);
                        break;
                }

                if (state != State.Normal)
                    break;
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
            TargetType = vm.ClassStore.FindType(BitConverter.ToInt64(data, index)).BaseClass.DefaultInstance!;
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
        public Statement? AltStatement { get; set; }
        public StatementComponent? SubComponent { get; set; }
        public StatementComponent? AltComponent { get; set; }
        public StatementComponent? PostComponent { get; set; }
        public ExecutableCode? InnerCode { get; set; }

        protected override IEnumerable<AbstractBytecode> BytecodeMembers =>
            SubComponent != null ? new[] { SubComponent } : Array.Empty<AbstractBytecode>();

        public StatementComponentType Type { get; set; }
        public BytecodeType CodeType { get; set; } = BytecodeType.Undefined;

        public virtual State Evaluate(RuntimeBase vm, ref ObjectRef rev)
        {
            ObjectRef? buf = vm.Stack.This!;;
            var state = State.Normal;
            switch (Type, CodeType)
            {
                case (StatementComponentType.Expression, BytecodeType.LiteralNumeric):
                    rev = Numeric.Compile(vm, Arg);
                    break;
                case (StatementComponentType.Expression, BytecodeType.LiteralString):
                    rev = String.Instance(vm, Arg);
                    break;
                case (StatementComponentType.Expression, BytecodeType.LiteralTrue):
                    rev = vm.ConstantTrue;
                    break;
                case (StatementComponentType.Expression, BytecodeType.LiteralFalse):
                    rev = vm.ConstantFalse;
                    break;
                case (StatementComponentType.Expression, BytecodeType.LiteralRange):
                    byte[] bs = BitConverter.GetBytes(ByteArg);
                    int start = BitConverter.ToInt32(new[] { bs[0], bs[1], bs[2], bs[3] }),
                        end = BitConverter.ToInt32(new[] { bs[4], bs[5], bs[6], bs[7] });
                    rev = Range.Instance(vm, start, end);
                    break;
                case (StatementComponentType.Expression, BytecodeType.Null):
                    rev = vm.ConstantVoid;
                    break;
                case (StatementComponentType.Expression, BytecodeType.Parentheses):
                    SubStatement!.Evaluate(vm, ref rev);
                    break;
                case (StatementComponentType.Expression, BytecodeType.TypeExpression):
                    rev = Package.RootPackage.GetClass(vm, Arg.Split("."))!.SelfRef;
                    break;
                case (StatementComponentType.Expression, BytecodeType.ConstructorCall):
                    if (SubComponent?.CodeType != BytecodeType.ParameterExpression)
                        throw new InternalException("Invalid constructor call; missing parameter expression");
                    var type = vm.FindType(Arg)!;
                    var ctor = (type.DeclaredMembers["ctor"] as IMethod)!;
                    var obj = new CodeObject(vm, type);
                    buf = new ObjectRef(Class.VoidType.DefaultInstance, ctor.Parameters.Count);
                    rev = vm.PutObject(VariableContext.Absolute, "instance-" + type.FullName + "-" + obj.ObjectId, obj);
                    vm.Stack.StepInside(vm, type.Name + ".ctor", ref rev, _rev =>
                    {
                        State state = State.Normal;
                        SubComponent.Evaluate(vm, ref buf);
                        for (var i = 0; i < ctor.Parameters.Count; i++)
                            vm.PutObject(VariableContext.Local, ctor.Parameters[i].Name, buf[vm, i]);
                        IRuntimeSite? site = ctor;
                        while (site != null)
                            site = site.Evaluate(vm, ref state, ref _rev!);
                        return _rev;
                    });
                    break;
                case (StatementComponentType.Declaration, _):
                    // variable declaration

                    rev = vm[VariableContext, Arg] = new ObjectRef(Statement.TargetType);
                    break;
                case (StatementComponentType.Pipe, _):
                    throw new NotImplementedException();
                case (StatementComponentType.Code, BytecodeType.Assignment):
                    // assignment
                    if (rev == null)
                        throw new InternalException("Invalid assignment; missing variable name");
                    if (SubComponent == null || (SubComponent.Type & StatementComponentType.Expression) == 0)
                        throw new InternalException("Invalid assignment; no Expression found");
                    buf = null;
                    state = SubComponent.Evaluate(vm, ref buf!);
                    rev.Value = buf?.Value;
                    break;
                case (StatementComponentType.Code, BytecodeType.Return):
                    // return
                    if (SubStatement == null || (SubStatement.Type & StatementComponentType.Expression) == 0)
                        throw new InternalException("Invalid return statement; no Expression found");
                    state = SubStatement.Evaluate(vm, ref rev) == State.Normal ? State.Return : state;
                    break;
                case (StatementComponentType.Code, BytecodeType.Throw):
                    if (SubStatement == null || (SubStatement.Type & StatementComponentType.Expression) == 0)
                        throw new InternalException("Invalid throw statement; no Exception found");
                    state = SubStatement.Evaluate(vm, ref rev) == State.Normal ? State.Throw : state;
                    throw new InternalException("+++ EXCEPTION IN CODE: " + (rev.Value?.ToString(1) ?? "no message") +
                                                " +++");
                case (StatementComponentType.Code, BytecodeType.ParameterExpression):
                    if (InnerCode == null)
                        break;
                    rev = new ObjectRef(Class.VoidType.DefaultInstance, InnerCode!.Main.Count);
                    for (var i = 0; i < InnerCode!.Main.Count; i++)
                    {
                        var val = vm.ConstantVoid;
                        InnerCode!.Main[i].Evaluate(vm, ref val!);
                        rev[vm, i] = val.Value;
                    }

                    break;
                case (StatementComponentType.Code, BytecodeType.StmtIf):
                    vm.Stack.StepInside(vm, "if", ref rev, _rev =>
                    {
                        state = SubStatement!.Evaluate(vm, ref buf!);
                        if (buf.ToBool())
                            state = InnerCode!.Evaluate(vm, ref _rev);
                        else if (SubComponent?.CodeType == BytecodeType.StmtElse)
                            state = SubComponent!.InnerCode!.Evaluate(vm, ref _rev);
                        return _rev;
                    });
                    break;
                case (StatementComponentType.Code, BytecodeType.StmtFor):
                    vm.Stack.StepInside(vm, "for", ref rev, _rev =>
                    {
                        state = SubStatement!.Evaluate(vm, ref buf!);
                        if (state != State.Normal)
                            return _rev;
                        while (SubComponent!.Evaluate(vm, ref buf) == State.Normal && buf.ToBool())
                        {
                            state = InnerCode!.Evaluate(vm, ref _rev);
                            if (state != State.Normal)
                                break;
                            state = AltStatement!.Evaluate(vm, ref _rev);
                            if (state != State.Normal)
                                break;
                        }

                        return _rev;
                    });
                    break;
                case (StatementComponentType.Code, BytecodeType.StmtForN):
                    vm.Stack.StepInside(vm, "forn", ref rev, _rev =>
                    {
                        state = SubStatement!.Evaluate(vm, ref buf!);
                        if (state != State.Normal)
                            return _rev;
                        var range = (buf.Value as Range)!;
                        var n = vm[VariableContext.Local, Arg] = new ObjectRef(Class.NumericIntType);
                        n.Value = range.start(vm).Value;
                        do
                        {
                            state = InnerCode!.Evaluate(vm, ref _rev);
                            n.Value = range.accumulate(vm, (n.Value as Numeric)!).Value;
                        } while (state == State.Normal && range.test(vm, (n.Value as Numeric)!).ToBool());

                        vm[VariableContext.Local, Arg] = null;
                        return _rev;
                    });
                    break;
                case (StatementComponentType.Operator, _):
                    if (state != State.Normal)
                        break;
                    var op = (Operator)ByteArg;
                    switch (op)
                    {
                        case Operator.LogicalNot:
                            if (SubComponent == null || (SubComponent.Type & StatementComponentType.Expression) == 0)
                                throw new InternalException("Invalid unary operator; missing right operand");
                            state = SubComponent.Evaluate(vm, ref buf!);
                            rev = rev.LogicalNot(vm);
                            break;
                        case Operator.Equals:
                        case Operator.NotEquals:
                            if (rev == null)
                                throw new InternalException("Invalid binary operator; missing left operand");
                            if (SubComponent == null || (SubComponent.Type & StatementComponentType.Expression) == 0)
                                throw new InternalException("Invalid binary operator; missing right operand");
                            state = SubComponent.Evaluate(vm, ref buf!);
                            rev = rev.Value!.Invoke(vm, "equals", ref rev, buf.Value) ?? vm.ConstantFalse;
                            if (op == Operator.NotEquals)
                                rev = rev.LogicalNot(vm);
                            break;
                        case Operator.IncrementRead:
                        case Operator.DecrementRead:
                        case Operator.ArithmeticNot:
                            if (SubComponent == null || (SubComponent.Type & StatementComponentType.Expression) == 0)
                                throw new InternalException("Invalid unary operator; missing right numeric operand");
                            state = SubComponent.Evaluate(vm, ref buf!);
                            if (buf.Value is not Numeric right2)
                                throw new InternalException("Invalid unary operator; missing right numeric operand");
                            buf.Value = right2.Operator(vm, op).Value;
                            break;
                        case Operator.ReadIncrement:
                        case Operator.ReadDecrement:
                            if (rev.Value is not Numeric left2)
                                throw new InternalException("Invalid unary operator; missing left numeric operand");
                            rev = left2.Operator(vm, op);
                            break;
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
                            state = SubComponent.Evaluate(vm, ref buf!);
                            // try to use overrides
                            if (op == Operator.Plus && rev?.Value?.Type.Name == "str"
                                || (rev?.Value?.Type.BaseClass.DeclaredMembers.ContainsKey("op" + op) ?? false))
                            {
                                rev = rev.Value!.Invoke(vm, "op" + op, ref buf, buf.Value)!;
                            }
                            else
                            {
                                // else try numeric operation
                                if (rev.Value is not Numeric left3)
                                    throw new InternalException(
                                        "Invalid binary operator; missing left numeric operand");
                                rev = left3.Operator(vm, op, (buf.Value as Numeric)!);
                            }

                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    break;
                case (StatementComponentType.Provider, _):
                    // non-constant expressions

                    switch (CodeType)
                    {
                        case BytecodeType.ExpressionVariable:
                            if (rev?.Value is { } obj1
                                && obj1.Type.DeclaredMembers.ContainsKey(Arg) 
                                && obj1.Type.DeclaredMembers[Arg] is {} icm1)
                            { // call member
                                IRuntimeSite? site = icm1;
                                while (site != null && state == State.Normal)
                                    site = site.Evaluate(vm, ref state, ref rev!);
                            } 
                            else
                            { // read variable
                                rev = vm[VariableContext, Arg]!;
                            }
                            break;
                        case BytecodeType.Call:
                            // invoke member
                            if (rev == null)
                                throw new InternalException("Invalid call; no target found");
                            if (rev.Value == null)
                                break;
                            if (rev.Value is Class.Instance cli1
                                && cli1.DeclaredMembers.ContainsKey(Arg)
                                && cli1.DeclaredMembers[Arg] is IMethod mtd1)
                            {
                                var param1 = mtd1.Parameters;
                                buf = new ObjectRef(Class.VoidType.DefaultInstance, param1.Count);
                                state = SubComponent!.Evaluate(vm, ref buf);
                                if (state != State.Normal)
                                    throw new InternalException("Invalid state after evaluating method parameters");
                                vm.Stack.StepDown(vm, cli1, cli1.Name + '.' + Arg, ref rev, _rev =>
                                {
                                    for (var i = 0; i < param1.Count; i++)
                                        vm.PutObject(VariableContext.Local, param1[i].Name, buf[vm, i]);
                                    return _rev.Value!.Invoke(vm, Arg, ref _rev!, buf.Stack)!;
                                });
                            }
                            else if (rev.Value!.Type.Primitive
                                     && rev.Value.Type.DeclaredMembers.ContainsKey(Arg)
                                     && rev.Value.Type.DeclaredMembers[Arg] is IMethod mtd2)
                            {
                                var param2 = mtd2.Parameters;
                                buf = new ObjectRef(Class.VoidType.DefaultInstance, param2.Count);
                                state = SubComponent!.Evaluate(vm, ref buf);
                                if (state != State.Normal)
                                    throw new InternalException("Invalid state after evaluating method parameters");
                                vm.Stack.StepDown(vm, rev, rev.Value.ObjectId + '.' + Arg, ref rev, _rev =>
                                {
                                    for (var i = 0; i < param2.Count; i++)
                                        vm.PutObject(VariableContext.Local, param2[i].Name, buf[vm, i]);
                                    return _rev.Value!.Invoke(vm, Arg, ref _rev!, buf.Stack)!;
                                });
                            }
                            else if (rev.Value!.Type.DeclaredMembers.ContainsKey(Arg)
                                     && rev.Value.Type.DeclaredMembers[Arg] is IMethod mtd3)
                            {
                                var param3 = mtd3.Parameters;
                                buf = new ObjectRef(Class.VoidType.DefaultInstance, param3.Count);
                                state = SubComponent!.Evaluate(vm, ref buf);
                                if (state != State.Normal)
                                    throw new InternalException("Invalid state after evaluating method parameters");
                                vm.Stack.StepDown(vm, rev, rev.Value.ToString() + '.' + Arg, ref rev, _rev =>
                                {
                                    for (var i = 0; i < param3.Count; i++)
                                        vm.PutObject(VariableContext.Local, param3[i].Name, buf[vm, i]);
                                    mtd3.Evaluate(vm, ref state, ref _rev!);
                                    return _rev;
                                });
                            }
                            else if (rev.Value is Class.Instance cli2
                                     && cli2.DeclaredMembers.ContainsKey(Arg)
                                     && cli2.DeclaredMembers[Arg] is Field fld1)
                            {
                                fld1.Evaluate(vm, ref state, ref rev!);
                            }
                            else if (rev.Value!.Type.DeclaredMembers.ContainsKey(Arg)
                                     && rev.Value!.Type.DeclaredMembers[Arg] is Field fld2)
                            {
                                fld2.Evaluate(vm, ref state, ref rev!);
                            }
                            else
                            {
                                throw new System.Exception("Invalid state; not a method or property");
                            }

                            break;
                        case BytecodeType.StdioExpression:
                            rev = vm.StdioRef;
                            break;
                    }

                    break;
                case (StatementComponentType.Setter, _):
                    // assignment
                    if (rev == null)
                        throw new InternalException("Invalid assignment; missing variable name");
                    if (SubStatement == null || (SubStatement.Type & StatementComponentType.Expression) == 0)
                        throw new InternalException("Invalid assignment; no Expression found");
                    buf = null;
                    state = SubStatement!.Evaluate(vm, ref buf!);
                    rev.Value = buf.Value;
                    break;
                case (StatementComponentType.Emitter, _):
                    if (SubStatement == null || (SubStatement.Type & StatementComponentType.Expression) == 0)
                        throw new InternalException("Invalid emitter; no Expression found");
                    if (!rev.IsPipe)
                        throw new InternalException("Cannot emit value into non-pipe accessor");
                    state = SubStatement.Evaluate(vm, ref buf!);
                    rev.WriteAccessor!.Evaluate(vm, ref buf);
                    break;
                case (StatementComponentType.Consumer, _):
                    if (SubStatement == null || (SubStatement.Type & StatementComponentType.Declaration) == 0)
                        throw new InternalException("Invalid consumer; no declaration found");
                    if (!rev.IsPipe)
                        throw new InternalException("Cannot consume value from non-pipe accessor");
                    state = SubStatement.Evaluate(vm, ref buf!);
                    rev.ReadAccessor!.Evaluate(vm, ref buf);
                    break;
                case (StatementComponentType.Lambda, _):
                    throw new NotImplementedException();
                default:
                    throw new NotImplementedException(CodeType.ToString());
            }

            if (state == State.Normal && PostComponent != null)
                state = PostComponent.Evaluate(vm, ref rev!);
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