using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KScr.Lib.Core;
using KScr.Lib.Exception;
using KScr.Lib.Model;
using KScr.Lib.Store;
using static KScr.Lib.Store.StackOutput;
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
        public ExecutableCode? Finally { get; set; }
        public StatementComponentType Type { get; set; }
        public IClassInstance TargetType { get; set; } = Class.VoidType.DefaultInstance;
        public List<StatementComponent> Main { get; } = new();

        public void Evaluate(RuntimeBase vm, Stack stack)
        {
            try
            {
                foreach (var component in Main)
                    switch (component.Type)
                    {
                        default:
                            component.Evaluate(vm, stack);
                            break;
                    }
            }
            finally
            {
                if (Finally != null)
                    Finally.Evaluate(vm, stack);
            }
        }

        public override void Write(Stream stream)
        {
            stream.Write(BitConverter.GetBytes((uint)Type));
            stream.Write(BitConverter.GetBytes((uint)CodeType));
            byte[] buf = RuntimeBase.Encoding.GetBytes(TargetType.FullName);
            stream.Write(BitConverter.GetBytes(buf.Length));
            stream.Write(buf);
            stream.Write(BitConverter.GetBytes(Main.Count));
            foreach (var component in Main)
                component.Write(stream);
            stream.Write(BitConverter.GetBytes(Finally != null));
            Finally?.Write(stream);
        }

        public override void Load(RuntimeBase vm, byte[] data, ref int index)
        {
            Main.Clear();

            Type = (StatementComponentType)BitConverter.ToUInt32(data, index);
            index += 4;
            CodeType = (BytecodeType)BitConverter.ToUInt32(data, index);
            index += 4;
            var len = BitConverter.ToInt32(data, index);
            index += 4;
            TargetType = vm.FindType(RuntimeBase.Encoding.GetString(data, index, len))!;
            index += len;
            len = BitConverter.ToInt32(data, index);
            index += 4;
            StatementComponent stmt;
            for (var i = 0; i < len; i++)
            {
                stmt = new StatementComponent { Statement = this };
                stmt.Load(vm, data, ref index);
                Main.Add(stmt);
            }

            if (BitConverter.ToBoolean(data, index++))
            {
                Finally = new ExecutableCode();
                Finally.Load(vm, data, ref index);
            }
        }

        public void Clear()
        {
            Main.Clear();
        }
    }

    [Flags]
    public enum ComponentMember : byte
    {
        None = 0x0,
        SubStatement = 0x01,
        AltStatement = 0x02,
        SubComponent = 0x04,
        AltComponent = 0x08,
        PostComponent = 0x10,
        InnerCode = 0x20
    }

    public class StatementComponent : AbstractBytecode, IStatementComponent
    {
        public Statement Statement { get; set; } = null!;
        public VariableContext VariableContext { get; set; }
        public string Arg { get; set; } = string.Empty;
        public ulong ByteArg { get; set; }
        public SourcefilePosition SourcefilePosition { get; set; }
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

        public virtual void Evaluate(RuntimeBase vm, Stack stack)
        {
            switch (Type, CodeType)
            {
                case (StatementComponentType.Expression, BytecodeType.LiteralNumeric):
                    stack[Default] = Numeric.Compile(vm, Arg);
                    break;
                case (StatementComponentType.Expression, BytecodeType.LiteralString):
                    stack[Default] = String.Instance(vm, stack, Arg);
                    break;
                case (StatementComponentType.Expression, BytecodeType.LiteralTrue):
                    stack[Default] = vm.ConstantTrue;
                    break;
                case (StatementComponentType.Expression, BytecodeType.LiteralFalse):
                    stack[Default] = vm.ConstantFalse;
                    break;
                case (StatementComponentType.Expression, BytecodeType.Null):
                    stack[Default] = vm.ConstantVoid;
                    break;
                case (StatementComponentType.Expression, BytecodeType.Parentheses):
                    SubStatement!.Evaluate(vm, stack.Output(Alp));
                    break;
                case (StatementComponentType.Expression, BytecodeType.TypeExpression):
                    stack[Default] = vm.FindType(Arg).SelfRef;
                    break;
                case (StatementComponentType.Expression, BytecodeType.ConstructorCall):
                    if (SubComponent?.CodeType != BytecodeType.ParameterExpression)
                        throw new FatalException(
                            "Invalid constructor call; missing parameter expression");
                    var type = vm.FindType(Arg)!;
                    var ctor = (type.ClassMembers.First(x => x.Name == Method.ConstructorName) as IMethod)!;
                    var obj = new CodeObject(vm, type);
                    var objRef = stack[Bet] = vm.PutObject(VariableContext.Absolute, obj);
                    stack[Del] = new ObjectRef(Class.VoidType.DefaultInstance, ctor.Parameters.Count);
                    stack.StepInto(vm, SourcefilePosition, stack.Alp, ctor, stack =>
                    {
                        SubComponent.Evaluate(vm, stack);
                        for (var i = 0; i < ctor.Parameters.Count; i++)
                            vm.PutLocal(ctor.Parameters[i].Name, stack.Bet[vm, stack, i]);
                        ctor.Evaluate(vm, stack.Output(Alp));
                    }, Alp);
                    break;
                case (StatementComponentType.Declaration, _):
                    // variable declaration
                    stack[Default] = vm[stack.KeyGen, VariableContext, Arg] = new ObjectRef(Statement.TargetType);
                    break;
                case (StatementComponentType.Pipe, _):
                    throw new NotImplementedException();
                case (StatementComponentType.Code, BytecodeType.Assignment):
                    // assignment
                    if (stack.Alp == null)
                        throw new FatalException(
                            "Invalid assignment; missing variable name");
                    if (SubComponent == null || (SubComponent.Type & StatementComponentType.Expression) == 0)
                        throw new FatalException(
                            "Invalid assignment; no Expression found");
                    SubComponent.Evaluate(vm, stack.Output(Del));
                    stack[Default].Value = stack.Del?.Value;
                    break;
                case (StatementComponentType.Code, BytecodeType.Return):
                    // return
                    if (SubStatement == null || (SubStatement.Type & StatementComponentType.Expression) == 0)
                        throw new FatalException("Invalid return statement; no Expression found");
                    SubStatement.Evaluate(vm, stack.Output(Alp | Omg));
                    stack.State = State.Return;
                    break;
                case (StatementComponentType.Code, BytecodeType.Throw):
                    if (SubStatement == null || (SubStatement.Type & StatementComponentType.Expression) == 0)
                        throw new FatalException(
                            "Invalid throw statement; no Exception found");
                    SubStatement.Evaluate(vm, stack.Output(Alp | Bet | Omg));
                    stack.State = State.Throw;
                    break;
                case (StatementComponentType.Code, BytecodeType.ParameterExpression):
                    if (InnerCode == null)
                        break;
                    stack[Alp] = new ObjectRef(Class.VoidType.DefaultInstance, InnerCode!.Main.Count);
                    for (var i = 0; i < InnerCode!.Main.Count; i++)
                    {
                        var val = vm.ConstantVoid;
                        InnerCode!.Main[i].Evaluate(vm, stack);
                        stack.Alp[vm, stack, i] = val.Value;
                    }

                    break;
                case (StatementComponentType.Code, BytecodeType.StmtIf):
                    stack.StepInside(vm, SourcefilePosition, "if", stack =>
                    {
                        SubStatement!.Evaluate(vm, stack.Output(Phi));
                        if (stack.Phi.ToBool())
                            InnerCode!.Evaluate(vm, stack.Output(Alp));
                        else if (SubComponent?.CodeType == BytecodeType.StmtElse)
                            SubComponent!.InnerCode!.Evaluate(vm, stack.Output(Alp));
                    });
                    break;
                case (StatementComponentType.Code, BytecodeType.StmtFor):
                    stack.StepInside(vm, SourcefilePosition, "for", stack =>
                    {
                        SubStatement!.Evaluate(vm, stack.Output(Alp));
                        SubComponent!.Evaluate(vm, stack.Output(Phi));
                        while (stack.Phi.ToBool())
                        {
                            InnerCode!.Evaluate(vm, stack.Output(Bet));
                            AltStatement!.Evaluate(vm, stack.Output(Alp));
                            SubComponent!.Evaluate(vm, stack.Output(Phi));
                        }
                    });
                    break;
                case (StatementComponentType.Code, BytecodeType.StmtForEach):
                    stack.StepInside(vm, SourcefilePosition, "foreach", stack =>
                    {
                        SubStatement!.Evaluate(vm, stack.Output(Alp));
                        var iterable = stack.Alp.Value!;
                        iterable.Invoke(vm, stack.Output(Eps), "iterator");
                        var iterator = stack.Eps.Value;
                        vm[stack.KeyGen, VariableContext.Local, Arg] = stack[Del] 
                            = new ObjectRef(iterator.Type.TypeParameterInstances[0].ResolveType(vm, iterator.Type));
                        while (iterator.Invoke(vm, stack.Output(Phi), "hasNext").ToBool())
                        {
                            iterator.Invoke(vm, stack.Channel(Eps, Del), "next");
                            InnerCode!.Evaluate(vm, stack.Output(Bet));
                        }
                    });
                    break;
                case (StatementComponentType.Code, BytecodeType.StmtWhile):
                    stack.StepInside(vm, SourcefilePosition, "while", stack =>
                    {
                        SubStatement.Evaluate(vm, stack.Output(Phi));
                        while (stack.Phi.ToBool())
                        {
                            InnerCode.Evaluate(vm, stack.Channel(Bet, Bet));
                            SubStatement.Evaluate(vm, stack.Output(Phi));
                        }
                    });
                    break;
                case (StatementComponentType.Code, BytecodeType.StmtDo):
                    stack.StepInside(vm, SourcefilePosition, "do-while", stack =>
                    {
                        do
                        {
                            InnerCode.Evaluate(vm, stack.Output(Bet));
                            SubStatement.Evaluate(vm, stack.Output(Phi));
                        } while (stack.Phi.ToBool());
                    });
                    break;
                case (StatementComponentType.Operator, _):
                    var op = (Operator)ByteArg;
                    switch (op)
                    {
                        case Operator.LogicalNot:
                            if (SubComponent == null ||
                                (SubComponent.Type & StatementComponentType.Expression) == 0)
                                throw new FatalException(
                                    "Invalid unary operator; missing right operand");
                            SubComponent.Evaluate(vm, stack.Output(Alp));
                            stack[Default] = stack.Alp.LogicalNot(vm);
                            break;
                        case Operator.Equals:
                        case Operator.NotEquals:
                            if (stack.Alp == null)
                                throw new FatalException(
                                    "Invalid binary operator; missing left operand");
                            if (SubComponent == null ||
                                (SubComponent.Type & StatementComponentType.Expression) == 0)
                                throw new FatalException(
                                    "Invalid binary operator; missing right operand");
                            SubComponent.Evaluate(vm, stack.Output(Bet));
                            stack[Default] = stack.Alp.Value!.Invoke(vm, 
                                stack.Output(Phi), "equals", stack.Bet.Value) ?? vm.ConstantFalse;
                            if (op == Operator.NotEquals)
                                stack[Default] = stack[Default].LogicalNot(vm);
                            break;
                        case Operator.IncrementRead:
                        case Operator.DecrementRead:
                        case Operator.ArithmeticNot:
                            if (SubComponent == null ||
                                (SubComponent.Type & StatementComponentType.Expression) == 0)
                                throw new FatalException(
                                    "Invalid unary operator; missing right numeric operand");
                            SubComponent.Evaluate(vm, stack.Output(Alp));
                            if (stack.Alp.Value is not Numeric right2)
                                throw new FatalException(
                                    "Invalid unary operator; missing right numeric operand");
                            stack[Default] = right2.Operator(vm, op);
                            break;
                        case Operator.ReadIncrement:
                        case Operator.ReadDecrement:
                            if (stack.Alp.Value is not Numeric left2)
                                throw new FatalException(
                                    "Invalid unary operator; missing left numeric operand");
                            stack[Default] = left2.Operator(vm, op);
                            break;
                        default:
                            if (SubComponent == null ||
                                (SubComponent.Type & StatementComponentType.Expression) == 0)
                                throw new FatalException(
                                    "Invalid binary operator; missing right numeric operand");
                            var bak = stack.Alp;
                            SubComponent.Evaluate(vm, stack.Output(Bet));
                            // try to use overrides
                            if ((op & Operator.Plus) == Operator.Plus && stack.Alp?.Value?.Type.Name == "str"
                                || (stack.Alp?.Value?.Type.BaseClass as IClass).ClassMembers.Any(x => x.Name == "op" + op))
                            {
                                stack[Default] = stack.Alp.Value!.Invoke(vm,
                                    stack.Output(Bet), "op" + ((op & Operator.Compound) == Operator.Compound
                                        ? op ^ Operator.Compound
                                        : op), stack.Bet.Value)!;
                            }
                            else
                            {
                                // else try numeric operation
                                if (stack.Alp.Value is not Numeric left3)
                                    throw new FatalException(
                                        "Invalid binary operator; missing left numeric operand");
                                stack[Default] = left3.Operator(vm, op, (stack.Bet.Value as Numeric)!);
                            }

                            if ((op & Operator.Compound) == Operator.Compound)
                                bak.Value = stack[Default].Value;

                            break;
                    }

                    break;
                case (StatementComponentType.Provider, BytecodeType.LiteralRange):
                    SubComponent.Evaluate(vm, stack.Output(Bet));
                    stack[Default] = Range.Instance(vm, (stack.Alp.Value as Numeric)!.IntValue, (stack.Bet.Value as Numeric)!.IntValue);
                    break;
                case (StatementComponentType.Provider, BytecodeType.ExpressionVariable):
                    if (stack.Alp?.Value is { } obj1
                        && obj1.Type.ClassMembers.FirstOrDefault(x => x.Name == Arg) is { } icm1)
                    {
                        // call member
                        icm1.Evaluate(vm, stack);
                    }
                    else
                    {
                        // read variable
                        stack[Default] = vm[stack.KeyGen, VariableContext, Arg]!;
                    }

                    break;
                case (StatementComponentType.Provider, BytecodeType.Call):
                    // invoke member
                    if ((stack.Alp.Value is IClass cls || (cls = stack.Alp.Value.Type) != null)
                        && cls.ClassMembers.FirstOrDefault(x => x.MemberType == ClassMemberType.Method && x.Name == Arg) is IMethod mtd)
                    {
                        var param = mtd.Parameters;
                        stack[Eps] = new ObjectRef(Class.VoidType.DefaultInstance, param.Count);
                        SubComponent!.Evaluate(vm, stack.Output(Eps));
                        stack.StepInto(vm, SourcefilePosition, stack.Alp, mtd, stack =>
                        {
                            for (var i = 0; i < param.Count; i++)
                                vm.PutLocal(param[i].Name, stack.Eps[vm, stack, i]);
                            stack.Alp.Value!.Invoke(vm, stack.Output(Alp), Arg, (stack.Eps as ObjectRef)!.Refs);
                        }, Alp);
                    }
                    else if (cls.ClassMembers.FirstOrDefault(x => x.MemberType == ClassMemberType.Property && x.Name == Arg) is Property prop)
                    {
                        prop.Evaluate(vm, stack.Output(Alp));
                    }
                    else
                    {
                        throw new System.Exception("Invalid state; not a method or property");
                    }
                    stack[Default] = stack.Alp;

                    break;
                case (StatementComponentType.Provider, BytecodeType.StdioExpression):
                    stack[Default] = vm.StdioRef;
                    break;
                case (StatementComponentType.Provider, BytecodeType.Undefined):
                    stack[Default] = stack.This!;
                    break;
                case (StatementComponentType.Setter, _):
                    // assignment
                    if (stack.Alp == null)
                        throw new FatalException("Invalid assignment; missing target");
                    if (SubStatement == null || (SubStatement.Type & StatementComponentType.Expression) == 0)
                        throw new FatalException("Invalid assignment; no Expression found");
                    SubStatement!.Evaluate(vm, stack.Output(Bet));
                    stack[Default].Value = stack.Bet.Value;
                    break;
                case (StatementComponentType.Emitter, _):
                    if (SubStatement == null || (SubStatement.Type & StatementComponentType.Expression) == 0)
                        throw new FatalException("Invalid emitter; no Expression found");
                    if (!stack.Alp.IsPipe)
                        throw new FatalException("Cannot emit value into non-pipe accessor");
                    SubStatement.Evaluate(vm, stack.Output(Bet));
                    stack.Alp.WriteAccessor!.Evaluate(vm, stack.Channel(Bet, Alp));
                    break;
                case (StatementComponentType.Consumer, _):
                    if (SubStatement == null || (SubStatement.Type & StatementComponentType.Declaration) == 0)
                        throw new FatalException("Invalid consumer; no declaration found");
                    if (!stack.Alp.IsPipe)
                        throw new FatalException("Cannot consume value from non-pipe accessor");
                    SubStatement.Evaluate(vm, stack.Output(Bet));
                    stack.Alp.ReadAccessor!.Evaluate(vm, stack.Channel(Bet, Alp));
                    break;
                default:
                    throw new NotImplementedException($"Not Implemented: {Type} {CodeType}");
            }

            if (PostComponent != null)
                PostComponent.Evaluate(vm, stack.Output(Default));
        }

        public override void Write(Stream stream)
        {
            stream.Write(BitConverter.GetBytes((uint)Type));
            stream.Write(BitConverter.GetBytes((uint)CodeType));
            stream.Write(new[] { (byte)VariableContext });
            stream.Write(BitConverter.GetBytes(ByteArg));
            byte[] buf = RuntimeBase.Encoding.GetBytes(Arg);
            stream.Write(BitConverter.GetBytes(buf.Length));
            stream.Write(buf);
            SourcefilePosition.Write(stream);
            var memberState = ComponentMember.None;
            if (SubStatement != null)
                memberState |= ComponentMember.SubStatement;
            if (AltStatement != null)
                memberState |= ComponentMember.AltStatement;
            if (SubComponent != null)
                memberState |= ComponentMember.SubComponent;
            if (AltComponent != null)
                memberState |= ComponentMember.AltComponent;
            if (PostComponent != null)
                memberState |= ComponentMember.PostComponent;
            if (InnerCode != null)
                memberState |= ComponentMember.InnerCode;
            stream.Write(new[] { (byte)memberState });
            if ((memberState & ComponentMember.SubStatement) != 0)
                SubStatement!.Write(stream);
            if ((memberState & ComponentMember.AltStatement) != 0)
                AltStatement!.Write(stream);
            if ((memberState & ComponentMember.SubComponent) != 0)
                SubComponent!.Write(stream);
            if ((memberState & ComponentMember.AltComponent) != 0)
                AltComponent!.Write(stream);
            if ((memberState & ComponentMember.PostComponent) != 0)
                PostComponent!.Write(stream);
            if ((memberState & ComponentMember.InnerCode) != 0)
                InnerCode!.Write(stream);
        }

        public override void Load(RuntimeBase vm, byte[] data, ref int index)
        {
            _Load(vm, data, ref index, out var sct, out var vct, out var bty, out ulong byteArg, out string arg, out var srcPos, out var subStmt, out var altStmt, out var subComp, out var altComp, out var postComp, out var innerCode);
            Type = sct;
            VariableContext = vct;
            CodeType = bty;
            ByteArg = byteArg;
            Arg = arg;
            SourcefilePosition = srcPos;
            SubStatement = subStmt;
            AltStatement = altStmt;
            SubComponent = subComp;
            AltComponent = altComp;
            PostComponent = postComp;
            InnerCode = innerCode;
        }

        private static void _Load(RuntimeBase vm, byte[] data, ref int index,
            out StatementComponentType sct,
            out VariableContext vct,
            out BytecodeType bty,
            out ulong bya,
            out string arg,
            out SourcefilePosition srcPos,
            out Statement? subStmt,
            out Statement? altStmt,
            out StatementComponent? subComp,
            out StatementComponent? altComp,
            out StatementComponent? postComp,
            out ExecutableCode? innerCode)
        {
            sct = (StatementComponentType)BitConverter.ToUInt32(data, index);
            index += 4;
            bty = (BytecodeType)BitConverter.ToUInt32(data, index);
            index += 4;
            vct = (VariableContext)data[index];
            index += 1;
            bya = BitConverter.ToUInt64(data, index);
            index += 8;
            var len = BitConverter.ToInt32(data, index);
            index += 4;
            arg = RuntimeBase.Encoding.GetString(data, index, len);
            index += len;
            srcPos = SourcefilePosition.Read(vm, data, ref index);
            var memberState = (ComponentMember)data[index];
            index += 1;
            if ((memberState & ComponentMember.SubStatement) == ComponentMember.SubStatement)
            {
                subStmt = new Statement();
                subStmt.Load(vm, data, ref index);
            }
            else
            {
                subStmt = null;
            }

            if ((memberState & ComponentMember.AltStatement) == ComponentMember.AltStatement)
            {
                altStmt = new Statement();
                altStmt.Load(vm, data, ref index);
            }
            else
            {
                altStmt = null;
            }

            if ((memberState & ComponentMember.SubComponent) == ComponentMember.SubComponent)
            {
                subComp = new StatementComponent();
                subComp.Load(vm, data, ref index);
            }
            else
            {
                subComp = null;
            }

            if ((memberState & ComponentMember.AltComponent) == ComponentMember.AltComponent)
            {
                altComp = new StatementComponent();
                altComp.Load(vm, data, ref index);
            }
            else
            {
                altComp = null;
            }

            if ((memberState & ComponentMember.PostComponent) == ComponentMember.PostComponent)
            {
                postComp = new StatementComponent();
                postComp.Load(vm, data, ref index);
            }
            else
            {
                postComp = null;
            }

            if ((memberState & ComponentMember.InnerCode) == ComponentMember.InnerCode)
            {
                innerCode = new ExecutableCode();
                innerCode.Load(vm, data, ref index);
            }
            else
            {
                innerCode = null;
            }
        }

        private static StatementComponent Read(RuntimeBase vm, Stack stack, byte[] data, int index)
        {
            var comp = new StatementComponent();
            comp.Load(vm, data, ref index);
            return comp;
        }
    }
}