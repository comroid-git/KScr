using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KScr.Core.Core;
using KScr.Core.Exception;
using KScr.Core.Model;
using KScr.Core.Store;
using static KScr.Core.Store.StackOutput;
using Array = System.Array;
using Range = KScr.Core.Core.Range;
using String = KScr.Core.Core.String;
// ReSharper disable VariableHidesOuterVariable

namespace KScr.Core.Bytecode
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

        public Stack Evaluate(RuntimeBase vm, Stack stack)
        {
            try
            {
                foreach (var component in Main)
                {
                    switch (Type, CodeType)
                    {
                        case (StatementComponentType.Code, BytecodeType.ParameterExpression):
                            stack[Default] = new ObjectRef(Class.VoidType.DefaultInstance, Main.Count);
                            for (var i = 0; i < Main.Count; i++)
                            {
                                Main[i].Evaluate(vm, stack.Output()).Copy(output: Bet);
                                stack[Default]![vm, stack, i] = stack.Bet?[vm,stack,0] ?? IObject.Null;
                            }

                            break;
                        default:
                            component.Evaluate(vm, stack);
                            break;
                    }
                    if (stack.State != State.Normal)
                        break;
                }
            }
            finally
            {
                if (Finally != null)
                    Finally.Evaluate(vm, stack);
            }

            return stack;
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

        public virtual Stack Evaluate(RuntimeBase vm, Stack stack)
        {
            IObjectRef? bak;
            switch (Type, CodeType)
            {
                case (StatementComponentType.Expression, BytecodeType.LiteralNumeric):
                    stack[Default] = Numeric.Compile(vm, Arg);
                    break;
                case (StatementComponentType.Expression, BytecodeType.LiteralString):
                    stack[Default] = String.Instance(vm, Arg);
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
                    SubComponent!.Evaluate(vm, stack.Output()).Copy();
                    break;
                case (StatementComponentType.Expression, BytecodeType.TypeExpression):
                    stack[Default] = vm.FindType(Arg).SelfRef;
                    break;
                case (StatementComponentType.Expression, BytecodeType.ConstructorCall):
                    if (SubStatement?.CodeType != BytecodeType.ParameterExpression)
                        throw new FatalException(
                            "Invalid constructor call; missing parameter expression");
                    var type = vm.FindType(Arg)!;
                    var ctor = (type.ClassMembers.First(x => x.Name == Method.ConstructorName) as IMethod)!;
                    var obj = new CodeObject(vm, type);
                    bak = stack[Default] = vm.PutObject(stack, VariableContext.Absolute, obj);
                    stack.StepInto(vm, SourcefilePosition, stack[Default], ctor, stack =>
                    {
                        SubStatement.Evaluate(vm, stack.Output()).Copy(output: Bet);
                        for (var i = 0; i < ctor.Parameters.Count; i++)
                            vm.PutLocal(stack, ctor.Parameters[i].Name, stack.Bet[vm, stack, i]);
                        ctor.Evaluate(vm, stack.Output());
                    });
                    stack[Default] = bak;
                    break;
                case (StatementComponentType.Expression, BytecodeType.Call):
                    // invoke member
                    if ((stack[Default].Value is IClass cls || (cls = stack[Default].Value.Type) != null)
                        && cls.ClassMembers.FirstOrDefault(x => x.MemberType == ClassMemberType.Method && x.Name == Arg) is IMethod mtd)
                    {
                            var param = mtd.Parameters;
                            var output = stack.Output(Del);
                            output[Del] = new ObjectRef(Class.VoidType.DefaultInstance, mtd.Parameters.Count);
                            SubStatement!.Evaluate(vm, output).Copy(Del);
                            if (mtd.IsNative() && !mtd.Parent.IsNative())
                                if (vm.NativeRunner == null)
                                    throw new FatalException("Cannot invoke native method; NativeRunner not loaded");
                                else vm.NativeRunner.Invoke(vm, stack, stack[Default][vm, stack, 0], mtd).Copy(Omg, Default);
                            else// todo having both next to each other is stupid
                            {
                                stack.StepInto(vm, SourcefilePosition, stack[Default], mtd, stack =>
                                {
                                    stack[Default].Value!.Invoke(vm, stack.Output(), Arg,
                                        (stack.Del as ObjectRef)!.Refs).Copy(Alp);
                                }, Alp);
                            }
                    }
                    else if (cls.ClassMembers.FirstOrDefault(x => x.MemberType == ClassMemberType.Property && x.Name == Arg) is Property prop)
                    {
                        if (prop.IsNative())
                            if (vm.NativeRunner == null)
                                throw new FatalException("Cannot invoke native method; NativeRunner not loaded");
                            else vm.NativeRunner.Invoke(vm, stack, stack[Default][vm, stack, 0], prop).Copy(Omg, Default);
                        else
                            //stack[Default] = prop;
                            prop.ReadValue(vm, stack.Output(), stack[Default]?.Value ?? cls as IClassInstance ?? cls.DefaultInstance).Copy();
                    }
                    else
                    {
                        throw new System.Exception("Invalid state; not a method or property");
                    }
                    // todo:: shouldnt be necessary
                    stack[Default] = stack.Alp;

                    break;
                case (StatementComponentType.Expression, BytecodeType.StdioExpression):
                    stack[Default] = vm.StdioRef;
                    break;
                case (StatementComponentType.Expression, BytecodeType.Undefined):
                    stack[Default] = stack.This;
                    break;
                case (StatementComponentType.Declaration, _):
                    // variable declaration
                    var split = Arg.Split(';');
                    stack[Default] = vm[stack, VariableContext, split[1]] = new ObjectRef(vm.FindType(split[0])!);
                    if (CodeType == BytecodeType.Assignment)
                    {
                        SubComponent!.Evaluate(vm, stack.Output()).Copy(Alp, Bet);
                        stack[Default]![vm, stack, 0] = stack.Bet![vm, stack, 0];
                    }
                    break;
                case (StatementComponentType.Pipe, _):
                    throw new NotImplementedException();
                case (StatementComponentType.Code, BytecodeType.Assignment):
                    // assignment
                    if (SubComponent == null || (SubComponent.Type & StatementComponentType.Expression) == 0)
                        throw new FatalException("Invalid assignment; no target found");
                    if (AltComponent == null || (AltComponent.Type & StatementComponentType.Expression) == 0)
                        throw new FatalException("Invalid assignment; no expression found");
                    SubComponent.Evaluate(vm, stack.Output()).Copy();
                    AltComponent.Evaluate(vm, stack.Output()).Copy(output: Bet);
                    stack[Default]![vm, stack, 0] = stack.Bet![vm, stack, 0];
                    break;
                case (StatementComponentType.Code, BytecodeType.Return):
                    // return
                    if (SubComponent == null || (SubComponent.Type & StatementComponentType.Expression) == 0)
                        throw new FatalException("Invalid return statement; no Expression found");
                    SubComponent.Evaluate(vm, stack.Output()).Copy(output: Alp | Omg);
                    stack.State = State.Return;
                    break;
                case (StatementComponentType.Code, BytecodeType.Throw):
                    if (SubComponent == null || (SubComponent.Type & StatementComponentType.Expression) == 0)
                        throw new FatalException(
                            "Invalid throw statement; no Exception found");
                    SubComponent.Evaluate(vm, stack.Output()).Copy(output: Alp | Omg);
                    stack.State = State.Throw;
                    break;
                case (StatementComponentType.Code, BytecodeType.StmtIf):
                    stack.StepInside(vm, SourcefilePosition, "if", stack =>
                    {
                        if (SubComponent!.Evaluate(vm, stack.Output()).Copy(output: Phi)!.ToBool())
                            InnerCode!.Evaluate(vm, stack.Output()).CopyState();
                        else if (AltComponent?.CodeType == BytecodeType.StmtElse)
                            AltComponent!.InnerCode!.Evaluate(vm, stack.Output()).CopyState();
                    });
                    break;
                case (StatementComponentType.Code, BytecodeType.StmtFor):
                    stack.StepInside(vm, SourcefilePosition, "for", stack =>
                    {
                        
                        for (SubStatement!.Evaluate(vm, stack.Output()).Copy(output: Del);
                             SubComponent!.Evaluate(vm, stack.Channel(Del, Phi)).Copy(Phi)!.ToBool();
                             /* no accumulator :( */)
                        {
                            InnerCode!.Evaluate(vm, stack.Output()).CopyState();
                            var delStack = stack.Channel(Del, Del);
                            // accumulate
                            AltComponent!.Evaluate(vm, delStack);
                            var val = stack[Del]![vm,stack,0] = delStack[Del]![vm,stack,0];
                            if (val == null || val.ObjectId == 0)
                                throw new NullReferenceException();
                        }
                    });
                    break;
                case (StatementComponentType.Code, BytecodeType.StmtForEach):
                    stack.StepInside(vm, SourcefilePosition, "foreach", stack =>
                    {
                        SubComponent!.Evaluate(vm, stack.Output()).Copy(Alp);
                        var iterable = stack[Alp]![vm,stack,0];
                        iterable.Invoke(vm, stack.Output(Eps), "iterator").Copy(Eps);
                        var iterator = stack[Eps]![vm,stack,0];
                        vm[stack, VariableContext.Local, Arg] = stack[Del]
                            = new ObjectRef(iterator.Type.TypeParameterInstances[0].ResolveType(vm, iterator.Type));
                        while (iterator.Invoke(vm, stack.Channel(Eps, Phi), "hasNext").Copy(Phi)!.ToBool())
                        {
                            iterator.Invoke(vm, stack.Channel(Eps, Bet), "next").Copy(Bet);
                            var val = stack[Del]![vm,stack,0] = stack[Bet]![vm,stack,0];
                            if (val == null || val.ObjectId == 0)
                                throw new NullReferenceException();
                            InnerCode!.Evaluate(vm, stack.Output()).CopyState();
                        }
                    });
                    break;
                case (StatementComponentType.Code, BytecodeType.StmtWhile):
                    stack.StepInside(vm, SourcefilePosition, "while", stack =>
                    {
                        while (SubComponent.Evaluate(vm, stack.Output(Phi)).Copy(Phi).ToBool())
                            InnerCode.Evaluate(vm, stack.Output()).CopyState();
                    });
                    break;
                case (StatementComponentType.Code, BytecodeType.StmtDo):
                    stack.StepInside(vm, SourcefilePosition, "do-while", stack =>
                    {
                        do InnerCode.Evaluate(vm, stack.Output()).CopyState();
                        while (SubComponent.Evaluate(vm, stack.Output(Phi)).Copy( Phi).ToBool());
                    });
                    break;
                case (StatementComponentType.Operator, _):
                    var op = (Operator)ByteArg;
                    bool compound = (op & Operator.Compound) == Operator.Compound;
                    bool unaryPrefix = (op & Operator.UnaryPrefix) == Operator.UnaryPrefix;
                    bool unaryPostfix = (op & Operator.UnaryPostfix) == Operator.UnaryPostfix;
                    bool binary = (op & Operator.Binary) == Operator.Binary;
                    if (compound) op ^= Operator.Compound;
                    if (unaryPrefix) op ^= Operator.UnaryPrefix;
                    if (unaryPostfix) op ^= Operator.UnaryPostfix;
                    if (binary) op ^= Operator.Binary;

                    var a = SubComponent!.Evaluate(vm, stack.Output()).Copy(output: Alp);
                    var b = AltComponent?.Evaluate(vm, stack.Output()).Copy(output: Bet);

                    if (unaryPrefix || unaryPostfix)
                        if (a![vm, stack, 0] is Numeric numA)
                            stack[Default] = numA.Operator(vm, op);
                        else stack[Default] = a![vm, stack, 0].Invoke(vm, stack.Output(), "op" + op).Copy();
                    else if (binary)
                        if (a![vm, stack, 0] is Numeric numA && b![vm, stack, 0] is Numeric numB)
                            stack[Default] = numA.Operator(vm, op, numB);
                        else stack[Default] = a![vm, stack, 0].Invoke(vm, stack.Output(), "op" + op, b![vm, stack, 0]).Copy();
                    if (compound)
                        a![vm, stack, 0] = stack[Default]![vm, stack, 0];
                    break;
                case (StatementComponentType.Provider, BytecodeType.LiteralRange):
                    SubComponent!.Evaluate(vm, stack.Output()).Copy(Alp, Bet);
                    AltComponent!.Evaluate(vm, stack.Output()).Copy(Alp, Del);
                    stack[Default] = Range.Instance(vm, (stack.Bet.Value as Numeric)!.IntValue, (stack.Del.Value as Numeric)!.IntValue);
                    break;
                case (StatementComponentType.Provider, BytecodeType.ExpressionVariable):
                    if (stack[Default]?.Value is { } obj1
                        && obj1.Type.ClassMembers.FirstOrDefault(x => x.Name == Arg) is { } icm1)
                    {
                        // call member
                        icm1.Evaluate(vm, stack);
                    }
                    else if (stack.This.Type.ClassMembers.FirstOrDefault(x => x.Name == Arg) is { } icm2)
                    {
                        // call to 'this'
                        icm2.Evaluate(vm, stack);
                    }
                    else
                    {
                        // read variable
                        stack[Default] = vm[stack, VariableContext.Local, Arg]
                                         ?? throw new FatalException("Undefined variable: " + Arg);
                    }

                    break;
                case (StatementComponentType.Provider, BytecodeType.Undefined):
                    stack[Default] = stack.This!;
                    break;
                case (StatementComponentType.Setter, _):
                    // assignment
                    if (stack[Default] == null)
                        throw new FatalException("Invalid assignment; missing target");
                    if (SubStatement == null || (SubStatement.Type & StatementComponentType.Expression) == 0)
                        throw new FatalException("Invalid assignment; no Expression found");
                    SubStatement!.Evaluate(vm, stack.Output()).Copy(Alp, Bet);
                    stack[Default].Value = stack.Bet.Value;
                    break;
                case (StatementComponentType.Emitter, _):
                    if (SubComponent == null || (SubComponent.Type & StatementComponentType.Expression) == 0)
                        throw new FatalException("Invalid emitter; no Expression found");
                    if (!stack[Default].IsPipe)
                        throw new FatalException("Cannot emit value into non-pipe accessor");
                    SubComponent.Evaluate(vm, stack.Output()).Copy(Alp, Bet);
                    stack[Default].WriteValue(vm, stack.Channel(Bet), stack.Bet?.Value ?? IObject.Null);
                    break;
                case (StatementComponentType.Consumer, _):
                    if (SubComponent == null || (SubComponent.Type & StatementComponentType.Declaration) == 0)
                        throw new FatalException("Invalid consumer; no declaration found");
                    if (!stack[Default].IsPipe)
                        throw new FatalException("Cannot consume value from non-pipe accessor");
                    SubComponent.Evaluate(vm, stack.Output()).Copy(Alp, Bet);
                    stack[Default].ReadValue(vm, stack.Channel(Bet), stack.Bet?.Value ?? IObject.Null);
                    break;
                default:
                    throw new NotImplementedException($"Not Implemented: {Type} {CodeType}");
            }

            if (PostComponent != null)
                PostComponent.Evaluate(vm, stack);

            return stack;
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